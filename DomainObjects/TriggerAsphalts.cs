using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using JCass_Core.JFunctions;
using JCass_ModelCore.Models;
using JCass_ModelCore.Treatments;

namespace MonteCarloRoadModelV1.DomainObjects;

public static class TriggerAsphalts
{

    public static List<TreatmentInstance> GetTriggeredAsphaltOrOgpaTreatments(RoadSegmentMC segment, int period, 
        ModelBase frameworkModel, MonteCarloRoadModelV1 domainModel, Dictionary<string, Dictionary<string, object>> lookups,
        Dictionary<string, object> infoFromModel)
    {
        List<TreatmentInstance> triggeredTreatments = new List<TreatmentInstance>();

        AddPreservationThinACIfValid(segment, domainModel, period, triggeredTreatments);
        AddHoldingThinACIfValid(segment, frameworkModel, domainModel, period, triggeredTreatments);
        AddAcHeavyMaintenanceIfValid(segment, period, domainModel, triggeredTreatments, infoFromModel);
        AddRehabilitationIfValid(segment, domainModel, period, triggeredTreatments, lookups);

        return triggeredTreatments;
    }

    private static void AddRehabilitationIfValid(RoadSegmentMC segment, MonteCarloRoadModelV1 domainModel, int iPeriod, List<TreatmentInstance> treatments, Dictionary<string, Dictionary<string, object>> lookups)
    {
        if (segment.CanRehabFlag == 0) return; // If the segment is not eligible for rehabilitation, do not add a treatment

        string treatmentName = segment.SurfaceClass + "_rehab";

        double pdi = segment.PavementDistressIndex;

        double tssScore = TreatmentSuitabilityScorer.GetTSSForRehabilitation(segment, domainModel, iPeriod);
        if (tssScore <= 0) return; // If the TSS score is below the minimum allowed, do not add a treatment

        string reason = $"SLA={Math.Round(segment.SurfaceAchievedLifePercent, 1)}";
        string comment = $"PDI={Math.Round(pdi, 1)}, TSS={Math.Round(tssScore, 2)}";

        double quantity = segment.AreaSquareMetre;
        string unitRateSetKey = segment.SurfaceClass + "_rehab_rate";
        var unitRateSet = lookups[unitRateSetKey];
        if (!unitRateSet.ContainsKey(segment.ONRC)) throw new Exception($"Unit rate for ONRC category '{segment.ONRC}' not found in lookup set '{unitRateSetKey}'.");
        double unitRate = (double)unitRateSet[segment.ONRC];

        TreatmentInstance treatment = new TreatmentInstance(segment.ElementIndex, treatmentName, iPeriod, quantity: quantity, unitRate: unitRate, false, reason, comment);
        treatment.TreatmentSuitabilityScore = tssScore;
        treatments.Add(treatment);
    }

    private static void AddHoldingThinACIfValid(RoadSegmentMC segment, ModelBase frameworkModel, MonteCarloRoadModelV1 domainModel, int iPeriod, List<TreatmentInstance> treatments)
    {
        string treatmentName = "ThinAC_H";
        if (segment.NextSurface == "cs") return;

        // If the rut depth is above the maximum threshold, do not add a treatment
        if (segment.RutMeanObserved > domainModel.Constants.TSSPreserveMaxRut) return;

        // If the surface life achieved is not greater than the minimum required, do not add a treatment
        if (segment.SurfaceAchievedLifePercent < domainModel.Constants.TSSPreserveMinSla) return;

        // For preservation, if PDI is above the maximum threshold, do not add a treatment
        if (segment.PavementDistressIndex > domainModel.Constants.TSSHoldingMaxPdiAC) return;

        // For Holding AC, do not eliminate if asphalt overlay is not allowed (in 'segment.AsphaltOkFlag') because of too high deflection etc.
        // This is because this treatment is assumed to include strengthening repairs to adress weak areas

        double tssScore = TreatmentSuitabilityScorer.GetTSSForPreservationTreatment(segment, domainModel, iPeriod);
        // If the TSS score is below the minimum allowed, do not add a treatment
        if (tssScore <= 0) return;

        double sdi = segment.SurfaceDistressIndex;
        string reason = $"SLA={Math.Round(segment.SurfaceAchievedLifePercent, 1)}";
        string comment = $"SDI={Math.Round(sdi, 1)}, TSS={Math.Round(tssScore, 2)}";

        double quantity = segment.AreaSquareMetre;

        double overlayQuantity = quantity;
        double repairQuantity = quantity * Math.Min(100, segment.PavementDistressIndex) / 100;
        double acOverlayUnitRate = frameworkModel.TreatmentTypes["ac_resurf"].UnitRate;
        double acRepairUnitRate = frameworkModel.TreatmentTypes["ac_hmaint"].UnitRate;

        double overlayCost = overlayQuantity * acOverlayUnitRate;
        double repairCost = repairQuantity * acRepairUnitRate;

        double totalCost = overlayCost + repairCost;

        double dummyArea = totalCost; // Dummy area which is effectively the cost

        // Check to ensure that the dummy rate for the combined treatment is 1.0
        double dummyUnitRate = frameworkModel.TreatmentTypes["ac_holding"].UnitRate;
        if (dummyUnitRate != 1.0)
        {
            throw new InvalidOperationException($"Dummy unit rate for ThinAC treatment which combined overlay and repairs should be 1.0, but it is {dummyUnitRate}");
        }

        TreatmentInstance treatment = new TreatmentInstance(segment.ElementIndex, treatmentName, iPeriod, dummyArea, false, reason, comment);

        // Assign the relative fractions of the cost to the appropriate budget categories
        decimal repairFraction = Convert.ToDecimal(repairCost / totalCost);
        decimal overlayFraction = Convert.ToDecimal(overlayCost / totalCost);
        Dictionary<string, decimal> treatmentFractions = new Dictionary<string, decimal>
        {
            { "Resurfacing", overlayFraction },
            { "Pre-Repairs", repairFraction }
        };
        treatment.AssignBudgetCategoryFractions(treatmentFractions);


        treatment.TreatmentSuitabilityScore = tssScore;
        treatments.Add(treatment);
    }

    private static void AddPreservationThinACIfValid(RoadSegmentMC segment, MonteCarloRoadModelV1 domainModel, int iPeriod, List<TreatmentInstance> treatments)
    {
        string treatmentName = segment.SurfaceClass + "_resurf";
        
        // If the rut depth is above the maximum threshold, do not add a treatment
        if (segment.RutMeanObserved > domainModel.Constants.TSSPreserveMaxRut) return;

        // If the surface life achieved is not greater than the minimum required, do not add a treatment
        if (segment.SurfaceAchievedLifePercent < domainModel.Constants.TSSPreserveMinSla) return;

        // For preservation, if PDI is above the maximum threshold, do not add a treatment
        if (segment.PavementDistressIndex > domainModel.Constants.TSSPreserveMaxPdiAC) return;

        // If asphalt overlay is not allowed because of too high deflection etc, do not add a treatment
        if (segment.CanDoThinACOverlay == 0) return;

        double tssScore = TreatmentSuitabilityScorer.GetTSSForPreservationTreatment(segment, domainModel, iPeriod);

        // If the TSS score is below the minimum allowed, do not add a treatment
        if (tssScore <= 0) return;

        double sdi = segment.SurfaceDistressIndex;
        string reason = $"SLA={Math.Round(segment.SurfaceAchievedLifePercent, 1)}";
        string comment = $"SDI={Math.Round(sdi, 1)}, TSS={Math.Round(tssScore, 2)}";

        double overlayQuantity = segment.AreaSquareMetre;

        TreatmentInstance treatment = new TreatmentInstance(segment.ElementIndex, treatmentName, iPeriod, overlayQuantity, false, reason, comment);

        treatment.TreatmentSuitabilityScore = tssScore;
        treatments.Add(treatment);
    }

    private static void AddAcHeavyMaintenanceIfValid(RoadSegmentMC segment, int iPeriod, MonteCarloRoadModelV1 domainModel, 
        List<TreatmentInstance> treatments, Dictionary<string, object> infoFromModel)
    {
        double presealAreaFraction = segment.PavementDistressIndex/100; 
        if (presealAreaFraction <= 0) return; // If there is no area in distress, do not add a treatment

        int periodsToLastNonRoutineTreatment = PeriodsToLastTreatmentNotRoutineMaintenance(infoFromModel, iPeriod);

        // Do not add AC Heavy Maintenance if the periods since last non-routine treatment is less than the minimum allowed
        if (periodsToLastNonRoutineTreatment < domainModel.Constants.MinPeriodsBetweenACHeavyMaint) return;

        // If an asphalt overlay is allowed, then only consider this treatment if the Surface Life Achieved is less than the maximum allowed for AC Heavy Maintenance
        // If an asphalt overlay is not allowed (e.g. due to deflection), then we can consider this treatment regardless of the SLA, otherwise the element will
        // have to wait until it can be rehabilitated
        if (segment.CanDoThinACOverlay == 1)
        {
            if (segment.SurfaceAchievedLifePercent > domainModel.Constants.MaxSlaForACHeavyMaint) return;
        }

        double tssScore = 0;
        if (segment.CanRehabFlag == 1)
        {
            // If this is a rehab route, then Preseal must compete with Rehab. Thus calculate the TSS for Preseal since
            // Rehab will be competing based on its TSS score.
            tssScore = TreatmentSuitabilityScorer.GetTSSForHoldingAction(segment, domainModel, iPeriod);
        }
        else
        {
            // If this is NOT a rehab route, then Preseal is considered as a Rehabilitation. Thus the TSS in this case
            // should be based on the TSS for Rehabilitation.
            tssScore = TreatmentSuitabilityScorer.GetTSSForRehabilitation(segment, domainModel, iPeriod);
        }

        if (tssScore <= 0) return; // If the TSS score is below the minimum allowed, do not add a treatment
        
        string reason = $"SLA={Math.Round(segment.SurfaceAchievedLifePercent, 1)}";
        string comment = $"PDI={Math.Round(segment.PavementDistressIndex, 1)}, TSS={Math.Round(tssScore, 2)}";

        // surface class should be 'ac' or 'ogpa'
        string treatmentName = segment.SurfaceClass + "_hmaint";

        double quantity = segment.AreaSquareMetre * presealAreaFraction;
        TreatmentInstance treatment = new TreatmentInstance(segment.ElementIndex, treatmentName, iPeriod, quantity, false, reason, comment);
        treatment.TreatmentSuitabilityScore = tssScore;
        treatments.Add(treatment);
    }

    private static int PeriodsToLastTreatmentNotRoutineMaintenance(Dictionary<string, object> infoFromModel, int iPeriod)
    {
        if (infoFromModel["previous_treatments"] is null) return 999; // Indicates that no treatments have been placed yet

        List<TreatmentInstance> previousTreatments = (List<TreatmentInstance>)infoFromModel["previous_treatments"];

        TreatmentInstance lastNonRoutineMaintenanceTreatment = null;

        // Loop over all previous treatments to find the most recent non-routine maintenance treatment
        int minTreatmentPeriod = int.MaxValue;
        foreach (TreatmentInstance treatment in previousTreatments)
        {
            if (treatment.TreatmentName != "RMaint")
            {
                int periodsToTreatment = iPeriod - treatment.TreatmentPeriod;
                if (periodsToTreatment < minTreatmentPeriod)
                {
                    minTreatmentPeriod = periodsToTreatment;
                    lastNonRoutineMaintenanceTreatment = treatment;
                }
            }
        }
        if (lastNonRoutineMaintenanceTreatment is not null)
        {
            return iPeriod - lastNonRoutineMaintenanceTreatment.TreatmentPeriod;
        }
        else
        {
            return 999; // Indicates that no non-routine treatment has been placed yet
        }
    }

}
