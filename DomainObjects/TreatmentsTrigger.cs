
using JCass_Core.JFunctions;
using JCass_ModelCore.Models;
using JCass_ModelCore.Treatments;

namespace MonteCarloRoadModelV1.DomainObjects;

/// <summary>
/// Class for checking treatments triggering 
/// </summary>
public class TreatmentsTrigger
{
    private ModelBase _frameworkModel;
    private MonteCarloRoadModelV1 _domainModel;

    public TreatmentsTrigger(ModelBase frameworkModel, MonteCarloRoadModelV1 domainModel)
    {
        _frameworkModel = frameworkModel ?? throw new ArgumentNullException(nameof(frameworkModel), "Domain model cannot be null");
        _domainModel = domainModel ?? throw new ArgumentNullException(nameof(domainModel), "Domain model cannot be null");
    }

    public List<TreatmentInstance> GetTriggeredTreatments(RoadSegmentMC segment, int period, Dictionary<string, object> infoFromModel)
    {        
        List<TreatmentInstance> triggeredTreatments = new List<TreatmentInstance>();

        // Check if the segment passes the Candidate Selection checks. If not, return an empty list.
        if (segment.IsCandidateForTreatment == 0) return triggeredTreatments;

        // Although we check if Periods to Next Treatment (i.e. committed) in the Candidate Selection, we need to do it 
        // again here, because the Candidate Selection result was last evaluated at the last epoch, while the periods to
        // next treatment have now changed since the period has changed
        int periodsToNextTreatment = Convert.ToInt32(infoFromModel["periods_to_next_treatment"]);
        if (periodsToNextTreatment <= 6) { return triggeredTreatments; }

        // Check if a birthday treatment should be added. If so, since we are forcing it, do not look for other candidate treatments
        this.AddBirthdayTreatmentBlocksOrConcreteIfValid(segment, period, triggeredTreatments);
        if (triggeredTreatments.Count > 0) return triggeredTreatments;

        //---------------------------------------------------------------------------------------------------------------------------------
        //      If we get here, we know that no second coats or birthday treatments are added.
        //      Now find candidate treatments to add to the optimisation stage
        //---------------------------------------------------------------------------------------------------------------------------------

        if (TriggerChipseals.NextSurfacingIsChipsealTreatment(segment))
        {
            return TriggerChipseals.GetTriggeredChipsealTreatments(segment, period, _domainModel, _frameworkModel.Lookups);
        }
        else
        {
            // If we get here, the surfacing type should be 'ac' or 'ogpa'. Double check and throw an exception if not
            if (segment.NextSurface != "ac" && segment.NextSurface != "ogpa")
            {
                throw new Exception($"Unexpected surfacing type for segment {segment.ElementIndex}. Expected 'ac' or 'ogpa', but got '{segment.NextSurface}'");
            }

            // Safe to get an AC or OGPA rehabilitation treatment
            return TriggerAsphalts.GetTriggeredAsphaltOrOgpaTreatments(segment, period, _frameworkModel, _domainModel, _frameworkModel.Lookups, infoFromModel);

        }

    }
        
    private void AddBirthdayTreatmentBlocksOrConcreteIfValid(RoadSegmentMC segment, int iPeriod, List<TreatmentInstance> treatments)
    {
        //n : pcal_can_treat_flag = 1 AND n : pcal_next_surf_blocks_flag = 1 AND n : period >= file_earliest_treat_period AND n : para_surf_remain_life <= 1

        if (segment.CanTreatFlag == 0) return; // If the segment cannot be treated, do not add a treatment
        string treatmentName = "";

        switch (segment.NextSurface)
        {
            case "blocks":
                treatmentName = "BlockRep";
                break;
            case "concrete":
                treatmentName = "ConcRep";
                break;
            case "other":
                treatmentName = "Xtreat";
                break;
            default:
                //If we get here, it is ChipSeal or Asphalt, which are not valid for this treatment
                return;
        }

        if (segment.SurfaceRemainingLife > 1) return; // If the surface remaining life is greater than 1, do not add a treatment
        if (iPeriod < segment.EarliestTreatmentPeriod) return; // If the period is less than the earliest treatment period, do not add a treatment

        //If we get here, a birthday treatment is valid
        double quantity = segment.AreaSquareMetre;
        bool forceTreatment = true;
        TreatmentInstance treatment = new TreatmentInstance(segment.ElementIndex, treatmentName, iPeriod, quantity, forceTreatment,  "Birthday treatment", "");
        treatment.TreatmentSuitabilityScore = 102; // Set a high suitability score for second coat treatments
        treatments.Add(treatment);

    }
   

}
