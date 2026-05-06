using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JCass_Core.JFunctions;
using JCass_ModelCore.DomainModels;
using JCass_ModelCore.Treatments;

namespace MonteCarloRoadModelV1.DomainObjects;

public static class TriggerChipseals
{

    public static List<TreatmentInstance> GetTriggeredChipsealTreatments(RoadSegmentMC segment, int period, MonteCarloRoadModelV1 domainModel, Dictionary<string, Dictionary<string, object>> lookups)
    {
        List<TreatmentInstance> triggeredTreatments = new List<TreatmentInstance>();

        // If we are triggering a ChipSeal treatment, then we need to check if a second coat after Rehabilitation or a follow-up chipseal after Preseal Repairs should be added.
        // Check if second coat after Rehabilitation should be added. If so, since we are forcing it, do not look
        // for other candidate treatments
        AddSecondCoatIfValid(segment, period, triggeredTreatments);
        if (triggeredTreatments.Count > 0) return triggeredTreatments;

        //-------------------------------------- Other Treatments if we are not adding a second coat --------------------------------------

        AddPreservationChipsealIfValid(segment, domainModel, period, triggeredTreatments);

        AddPresealRepairIfValid(segment, domainModel, period, triggeredTreatments);

        AddRehabilitationIfValid(segment, domainModel, period, triggeredTreatments, lookups);

        return triggeredTreatments;
    }

    public static bool NextSurfacingIsChipsealTreatment(RoadSegmentMC segment)
    {
        // First check if pre-processing specified that the next surfacing MUST be ChipSeal. If so, then return true by default
        // This allows us to force the treatment to switch from whatever it is currently to ChipSeal, even if the current surface is not ChipSeal.
        // This is needed to handle cases where the current surface is AC, but the next surface needs to be ChipSeal
        if (segment.NextSurface == "cs") return true;

        // If we are not forcing the next surfacing to be ChipSeal, then we need to check if the current surface is ChipSeal. If it
        // is not, then we should not trigger a ChipSeal treatment
        if (segment.SurfaceClass != "cs") return false;

        // If we are here, the current surface is ChipSeal and NextSurface is not specified as something other than ChipSeal, so we can trigger a
        // ChipSeal treatment 
        return true;

    }

    private static void AddRehabilitationIfValid(RoadSegmentMC segment, MonteCarloRoadModelV1 domainModel, int iPeriod, List<TreatmentInstance> treatments, Dictionary<string, Dictionary<string, object>> lookups)
    {
        if (segment.CanRehabFlag == 0) return; // If the segment is not eligible for rehabilitation, do not add a treatment

        string treatmentName = "cs_rehab";

        double pdi = segment.PavementDistressIndex;

        double tssScore = TreatmentSuitabilityScorer.GetTSSForRehabilitation(segment, domainModel, iPeriod);
        if (tssScore <= 0) return; // If the TSS score is below the minimum allowed, do not add a treatment

        string reason = $"SLA={Math.Round(segment.SurfaceAchievedLifePercent, 1)}";
        string comment = $"PDI={Math.Round(pdi, 1)}, TSS={Math.Round(tssScore, 2)}";

        double quantity = segment.AreaSquareMetre;
        var unitRateSet = lookups["cs_rehab_rate"];
        if (!unitRateSet.ContainsKey(segment.ONRC)) throw new Exception($"Unit rate for ONRC category '{segment.ONRC}' not found in lookup set 'cs_rehab_rate'.");
        double unitRate = (double)unitRateSet[segment.ONRC];
        
        TreatmentInstance treatment = new TreatmentInstance(segment.ElementIndex, treatmentName, iPeriod, quantity:quantity, unitRate: unitRate, false, reason, comment);
        treatment.TreatmentSuitabilityScore = tssScore;
        treatments.Add(treatment);
    }

    private static void AddPresealRepairIfValid(RoadSegmentMC segment, MonteCarloRoadModelV1 domainModel, int iPeriod, List<TreatmentInstance> treatments)
    {
        if (segment.PavementDistressIndex <= 0.0) return; // If preseal area fraction is zero or negative, do not add a treatment
        
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

        double pdi = segment.PavementDistressIndex;

        string reason = $"SLA={Math.Round(segment.SurfaceAchievedLifePercent, 1)}";
        string comment = $"PDI={Math.Round(pdi, 1)}, TSS={Math.Round(tssScore, 2)}";

        // PDI is a percentage value that indicates the fraction of the area that needs pre-sealing
        double treatmentAreaFraction = segment.PavementDistressIndex / 100.0; 
        string treatmentName = "cs_preseal";
        double quantity = segment.AreaSquareMetre * treatmentAreaFraction;
        TreatmentInstance treatment = new TreatmentInstance(segment.ElementIndex, treatmentName, iPeriod, quantity, false, reason, comment);
        treatment.TreatmentSuitabilityScore = tssScore;
        treatments.Add(treatment);

    }

    private static void AddPreservationChipsealIfValid(RoadSegmentMC segment, MonteCarloRoadModelV1 domainModel, int iPeriod, List<TreatmentInstance> treatments)
    {
        string treatmentName = "cs_preserve";
        
        // If the rut depth is above the maximum threshold, do not add a treatment
        if (segment.RutMeanObserved > domainModel.Constants.TSSPreserveMaxRut) return;

        // If the surface life achieved is not greater than the minimum required, do not add a treatment
        if (segment.SurfaceAchievedLifePercent < domainModel.Constants.TSSPreserveMinSla) return;

        // For preservation, if PDI is above the maximum threshold, do not add a treatment
        if (segment.PavementDistressIndex > domainModel.Constants.TSSPreserveMaxPdiChipseal) return;

        double tssScore = TreatmentSuitabilityScorer.GetTSSForPreservationTreatment(segment, domainModel, iPeriod);
        
        double sdi = segment.SurfaceDistressIndex;
        string reason = $"SLA={Math.Round(segment.SurfaceAchievedLifePercent, 1)}";
        string comment = $"SDI={Math.Round(sdi, 1)}, TSS={Math.Round(tssScore, 2)}";

        double quantity = segment.AreaSquareMetre;        
        TreatmentInstance treatment = new TreatmentInstance(segment.ElementIndex, treatmentName, iPeriod, quantity, false, reason, comment);
        
        treatment.TreatmentSuitabilityScore = tssScore;
        treatments.Add(treatment);
    }

    private static void AddSecondCoatIfValid(RoadSegmentMC segment, int iPeriod, List<TreatmentInstance> treatments)
    {
        // Only add a second coat if it is needed and the segment is a valid candidate
        if (segment.SecondCoatNeeded)
        {
            string treatmentName = "cs_on_rehab";
            string reason = "Second-Coat on Rehab";

            if (segment.SurfaceFunction != "1a")
            {
                treatmentName = "cs_on_repairs";
                reason = "Second-Coat on Preseal Repairs";
            }

            double quantity = segment.AreaSquareMetre;

            TreatmentInstance treatment = new TreatmentInstance(segment.ElementIndex, treatmentName, iPeriod, quantity, true, reason, reason);
            treatment.TreatmentSuitabilityScore = 102; // Set a high suitability score for second coat treatments
            treatments.Add(treatment);
        }
    }      

}
