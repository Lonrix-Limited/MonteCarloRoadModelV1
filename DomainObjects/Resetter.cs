using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JCass_ModelCore.Models;
using JCass_ModelCore.Treatments;

namespace MonteCarloRoadModelV1.DomainObjects;

public class Resetter
{

    private ModelBase _frameworkModel;
    private MonteCarloRoadModelV1 _domainModel;

    public Resetter(ModelBase frameworkModel, MonteCarloRoadModelV1 domainModel)
    {
        _frameworkModel = frameworkModel ?? throw new ArgumentNullException(nameof(frameworkModel), "Domain model cannot be null");
        _domainModel = domainModel ?? throw new ArgumentNullException(nameof(domainModel), "Domain model cannot be null");
    }

    public RoadSegmentMC ResetSegment(RoadSegmentMC segment, int period, TreatmentInstance treatment)
    {
        // if treatment is null, return segment without changes
        if (treatment == null) return segment;

        if (segment.ElementIndex == 22991)
        {
            int debug = 0; // Debugging breakpoint
        }

        bool isRehabTreatment = treatment.TreatmentName.ToLower().Contains("rehab");
        string treatmentTypeCode = isRehabTreatment ? "rehab" : "resurf";

        // Reset where needed, or Increment those that do not reset on treatment, such as traffic.
        // Keep the code same order as the model parameter list

        segment.AverageDailyTraffic = segment.AverageDailyTraffic * (1 + segment.TrafficGrowthPercent / 100);
        // No need to reset HCV count as it is automatically calculated based on the AverageDailyTraffic and HCVPercent

        segment.PavementAge = segment.PavementAge + 1;
        if (treatment.TreatmentName.ToLower().Contains("rehab")) segment.PavementAge = 0;  // Reset pavement age to 0 for rehab treatments. 

        segment.PavementRemainingLife = Convert.ToDouble(_frameworkModel.Lookups["pavement_expected_life"][segment.ONRC]);

        // No need to update Pavement Life Achieved and HCV Risk because it is automatically calculated based on the HCV and Pavement Life Achieved

        // Update surfacing age, class, material, thickness, function, expected life based on the treatment being applied. 
        UpdateSurfacingPropertiesForTreatment(segment, treatment);


        //--------------------------------------------------------------------------------------------------------------------------------------------
        // HSD Increments. When treatment is applied, always draw a new increment for the episode
        //--------------------------------------------------------------------------------------------------------------------------------------------

        // Rut Depth                
        double newValue = GetRutResetValue(segment, _domainModel.SubModels, treatmentTypeCode, _frameworkModel.Random); //Reset value.
        segment.RutIncrement = Incrementer.GetRutIncrementForEpisode(segment, _domainModel.SubModels, _frameworkModel.Random, _domainModel.Constants); //Get new increment for new eposode.        
        double residual = Incrementer.GetTextureResidual(_domainModel.SubModels, _frameworkModel.Random, _domainModel.Constants, newValue);
        segment.RutMeanLatent = newValue;
        segment.RutMeanObserved = segment.RutMeanLatent + residual;  // Update the observed rut mean with the residual to reflect the variability in the increment
        segment.RutAndIRIIncrementEpisodeLength = 1;  // Reset episode length to 1 since we are drawing a new increment for the episode


        // IRI 
        newValue = GetIRIResetValue(segment, _domainModel.SubModels, treatmentTypeCode, _frameworkModel.Random); //Reset value.
        segment.IRIIncrement = Incrementer.GetIRIIncrementForEpisode(segment, _domainModel.SubModels, _frameworkModel.Random, _domainModel.Constants); //Get new increment for new eposode.        
        residual = Incrementer.GetIRIResidual(_domainModel.SubModels, _frameworkModel.Random, _domainModel.Constants, newValue);
        segment.IRIMeanLatent = newValue;
        segment.IRIMeanObserved = segment.IRIMeanLatent + residual;
        // No need to reset episode separately for IRI as it is the same as Rut episode

        
        // Texture Depth               
        newValue = GetTextureDepthResetValue(segment, _domainModel.SubModels, treatmentTypeCode, _frameworkModel.Random); //Reset value.
        segment.TextureIncrement = Incrementer.GetTextureIncrementForEpisode(segment, _domainModel.SubModels, _frameworkModel.Random, _domainModel.Constants); //Get new increment for new eposode.        
        residual = Incrementer.GetTextureResidual(_domainModel.SubModels, _frameworkModel.Random, _domainModel.Constants, newValue);
        segment.TextureMeanLatent = newValue;
        segment.TextureMeanObserved = segment.TextureMeanLatent + residual;
        segment.TextureIncrementEpisodeLength = 1;  // Reset episode length to 1 since we are drawing a new increment for the episode

        // Maintenance
        _domainModel.MaintenanceModel.UpdateRoutineMaintenanceExtents(segment);

        return segment;
    }

    #region Surfacing Properties Reset


    /// <summary>
    /// Updates surfacing properties of the given road segment based on the specified treatment. 
    /// </summary>
    /// <param name="segment">Segment on which to update surfacing properties</param>
    /// <param name="treatment">Treatment being applied</param>
    private void UpdateSurfacingPropertiesForTreatment(RoadSegmentMC segment, TreatmentInstance treatment)
    {
        bool isRehabTreatment = treatment.TreatmentName.ToLower().Contains("rehab");
        // No change in these properties:
                       
        // segment.SurfaceExpectedLife 

        segment.SurfaceAge = 0;  
        segment.SurfaceMaterial = _frameworkModel.Lookups["treat_surf_materials"][treatment.TreatmentName].ToString();
        segment.SurfaceClass = _frameworkModel.Lookups["treat_surf_class"][treatment.TreatmentName].ToString();
        if (isRehabTreatment)
        {
            segment.SurfaceThickness = Convert.ToDouble(_frameworkModel.Lookups["surf_thickness_new"][segment.SurfaceMaterial]);
            segment.SurfaceNumberOfLayers = 1;

            // For rehab on chipseal, reset surfacing function to 1; for ac set to "R"
            segment.SurfaceFunction = segment.SurfaceClass == "cs" ? "1" : "R";            
        }
        else
        {
            double thicknessAdded = Convert.ToDouble(_frameworkModel.Lookups["surf_thickness_add"][segment.SurfaceMaterial]);
            segment.SurfaceThickness = segment.SurfaceThickness + thicknessAdded;
            segment.SurfaceNumberOfLayers += 1;
            segment.SurfaceFunction = GetNextSurfaceFunction(segment.SurfaceFunction);
        }

        segment.SurfaceExpectedLife = Convert.ToDouble(_frameworkModel.Lookups["surf_life_exp"][segment.SurfaceExpectedLifeCode]);
        // Note: surface life achieved and surface remaining life are automatically calculated based on the surface age and expected life

    }

    private string GetNextSurfaceFunction(string currentFunction)
    {
        switch (currentFunction)
        {
            case "1":
                return "2";
            case "2": 
                return "R";
            default:
                return "R";
        }
    }


    #endregion

    #region Rut Reset

    /// <summary>
    /// Gets a simulated Rut Reset value for the given road segment based on the appropriate model for the surface class and treatment type.
    /// </summary>
    /// <param name="segment">Segment to simulate the Rut Reset value for</param>
    /// <param name="subModels">Sub-model definitions to use for the simulation</param>
    /// <param name="treatmentTypeCode">Code representing the type of treatment (e.g., "resurf" or "rehab")</param>
    /// <param name="random">Random number generator to use for the simulation</param>
    /// <returns>The simulated Rut Reset value</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static double GetRutResetValue(RoadSegmentMC segment, SubModelDefinitions subModels, string treatmentTypeCode, Random random)
    {
        
        var inputParameters = GetInputParametersForSegment(segment);

        if (treatmentTypeCode == "resurf")
        {
            return subModels.RutResetSimulatorResurf.GetSimulatedValue(inputParameters, random);
        }
        else if (treatmentTypeCode == "rehab")
        {
            return subModels.RutResetSimulatorRehab.GetSimulatedValue(inputParameters, random);
        }
        else
        {
            throw new InvalidOperationException($"Unknown treatment type code: {treatmentTypeCode}");
        }
    }

    #endregion

    #region IRI Reset

    /// <summary>
    /// Gets a simulated IRI Reset value for the given road segment based on the appropriate model for the surface class and treatment type.
    /// </summary>
    /// <param name="segment">Segment to simulate the IRI Reset value for</param>
    /// <param name="subModels">Sub-model definitions to use for the simulation</param>
    /// <param name="treatmentTypeCode">Code representing the type of treatment (e.g., "resurf" or "rehab")</param>
    /// <param name="random">Random number generator to use for the simulation</param>
    /// <returns>The simulated IRI Reset value</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static double GetIRIResetValue(RoadSegmentMC segment, SubModelDefinitions subModels, string treatmentTypeCode, Random random)
    {
        var inputParameters = GetInputParametersForSegment(segment);

        if (treatmentTypeCode == "resurf")
        {
            return subModels.IRIResetSimulatorResurf.GetSimulatedValue(inputParameters, random);
        }
        else if (treatmentTypeCode == "rehab")
        {
            return subModels.IRIResetSimulatorRehab.GetSimulatedValue(inputParameters, random);
        }
        else
        {
            throw new InvalidOperationException($"Unknown treatment type code: {treatmentTypeCode}");
        }
    }
        

    #endregion

    #region Texture Reset

    /// <summary>
    /// Gets a simulated Texture Depth Reset value for the given road segment based on the appropriate model for the surface class and treatment type.
    /// </summary>
    /// <param name="segment">Segment to simulate the Texture Depth Reset value for</param>
    /// <param name="subModels">Sub-model definitions to use for the simulation</param>
    /// <param name="treatmentTypeCode">Code representing the type of treatment (e.g., "resurf" or "rehab")</param>
    /// <param name="random">Random number generator to use for the simulation</param>
    /// <returns>The simulated Texture Depth  Reset value</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static double GetTextureDepthResetValue(RoadSegmentMC segment, SubModelDefinitions subModels, string treatmentTypeCode, Random random)
    {
        var inputParameters = GetInputParametersForSegment(segment);
        return subModels.TextureResetSimulator.GetSimulatedValue(inputParameters, random); //Texture reset does not vary by surface class, so we can use the same model for all segments regardless of surface class.        
    }
       
    #endregion

    #region Helper Methods

    private static Dictionary<string, object> GetInputParametersForSegment(RoadSegmentMC segment)
    {        
        return new Dictionary<string, object>
        {
            { "rut_mean_pre", segment.RutMeanLatent },
            { "iri_mean_pre", segment.IRIMeanLatent },
            { "surf_age", segment.SurfaceAge },            
            { "pre1_rea_mtc_extent", segment.MaintenancePavement },
            { "adt", segment.AverageDailyTraffic },
            { "heavy_perc", segment.HeavyVehiclePercentage },
            { "surf_thick", segment.SurfaceThickness },
            { "surf_class", segment.SurfaceClassForRules },
            { "surf_count", segment.SurfaceNumberOfLayers }
        };
    }

    #endregion
}
