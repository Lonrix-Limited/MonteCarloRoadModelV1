using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JCass_ModelCore.Models;

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

    public RoadSegmentMC ResetSegment(int iElemIndex)
    {

        if (iElemIndex == 8426)
        {
            int kk = 9;
        }

        // Create a new RoadSegmentMC object based purely on the raw data provided in the string array.
        RoadSegmentMC segment = RoadSegmentFactoryMC.GetFromRawData(_frameworkModel, iElemIndex);

        


        return segment;
    }

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
        
        if (segment.SurfaceClass == "cs" || segment.SurfaceClass == "slurry")
        {
            return GetRutResetForCSandSlurry(inputParameters, subModels, treatmentTypeCode, random);
        }
        else if (segment.SurfaceClass == "ac" || segment.SurfaceClass == "ogpa")
        {
            return GetRutResetForACandOGPA(inputParameters, subModels, treatmentTypeCode, random);
        }
        if (segment.SurfaceClass == "concrete")
        {
            return 1.0;   //Not enough data.
        }
        if (segment.SurfaceClass == "unknown")
        {
            return GetRutResetForCSandSlurry(inputParameters, subModels, treatmentTypeCode, random);  //Default to CS model for unknown surface class.
        }
        else
        {
            throw new InvalidOperationException($"Unknown surface class: {segment.SurfaceClass}");
        }
    }

    private static double GetRutResetForCSandSlurry(Dictionary<string, object> inputParameters, SubModelDefinitions subModels, string treatmentTypeCode, Random random)
    {
        if (treatmentTypeCode == "resurf")
        {
            return subModels.RutResetSimulatorCSResurf.GetSimulatedValue(inputParameters, random);
        }
        else if (treatmentTypeCode == "rehab")
        {
            return subModels.RutResetSimulatorCSRehab.GetSimulatedValue(inputParameters, random);
        }
        else
        {
            throw new InvalidOperationException($"Unknown treatment type code: {treatmentTypeCode}");
        }
    }

    private static double GetRutResetForACandOGPA(Dictionary<string, object> inputParameters, SubModelDefinitions subModels, string treatmentTypeCode, Random random)
    {
        if (treatmentTypeCode == "resurf")
        {
            return subModels.RutResetSimulatorACResurf.GetSimulatedValue(inputParameters, random);
        }
        else if (treatmentTypeCode == "rehab")
        {
            return subModels.RutResetSimulatorACRehab.GetSimulatedValue(inputParameters, random);
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

        if (segment.SurfaceClass == "cs" || segment.SurfaceClass == "slurry")
        {
            return GetIRIResetForCSandSlurry(inputParameters, subModels, treatmentTypeCode, random);
        }
        else if (segment.SurfaceClass == "ac" || segment.SurfaceClass == "ogpa")
        {
            return GetIRIResetForACandOGPA(inputParameters, subModels, treatmentTypeCode, random);
        }
        if (segment.SurfaceClass == "concrete")
        {
            return 1.5;   //Not enough data.
        }
        if (segment.SurfaceClass == "unknown")
        {
            return GetIRIResetForCSandSlurry(inputParameters, subModels, treatmentTypeCode, random);  //Default to CS model for unknown surface class.
        }
        else
        {
            throw new InvalidOperationException($"Unknown surface class: {segment.SurfaceClass}");
        }
    }

    private static double GetIRIResetForCSandSlurry(Dictionary<string, object> inputParameters, SubModelDefinitions subModels, string treatmentTypeCode, Random random)
    {
        if (treatmentTypeCode == "resurf")
        {
            return subModels.IRIResetSimulatorCSResurf.GetSimulatedValue(inputParameters, random);
        }
        else if (treatmentTypeCode == "rehab")
        {
            return subModels.IRIResetSimulatorCSRehab.GetSimulatedValue(inputParameters, random);
        }
        else
        {
            throw new InvalidOperationException($"Unknown treatment type code: {treatmentTypeCode}");
        }
    }

    private static double GetIRIResetForACandOGPA(Dictionary<string, object> inputParameters, SubModelDefinitions subModels, string treatmentTypeCode, Random random)
    {
        if (treatmentTypeCode == "resurf")
        {
            return subModels.IRIResetSimulatorACResurf.GetSimulatedValue(inputParameters, random);
        }
        else if (treatmentTypeCode == "rehab")
        {
            return subModels.IRIResetSimulatorACRehab.GetSimulatedValue(inputParameters, random);
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

        if (segment.SurfaceClass == "cs" || segment.SurfaceClass == "slurry")
        {
            return GetTextureDepthResetForCSandSlurry(inputParameters, subModels, treatmentTypeCode, random);
        }
        else if (segment.SurfaceClass == "ac" || segment.SurfaceClass == "ogpa")
        {
            return GetTextureDepthResetForACandOGPA(inputParameters, subModels, treatmentTypeCode, random);
        }
        if (segment.SurfaceClass == "concrete")
        {
            return 1.5;   //Not enough data.
        }
        if (segment.SurfaceClass == "unknown")
        {
            return GetTextureDepthResetForCSandSlurry(inputParameters, subModels, treatmentTypeCode, random);  //Default to CS model for unknown surface class.
        }
        else
        {
            throw new InvalidOperationException($"Unknown surface class: {segment.SurfaceClass}");
        }
    }

    private static double GetTextureDepthResetForCSandSlurry(Dictionary<string, object> inputParameters, SubModelDefinitions subModels, string treatmentTypeCode, Random random)
    {
        if (treatmentTypeCode == "resurf")
        {
            return subModels.TextureResetSimulatorCSResurf.GetSimulatedValue(inputParameters, random);
        }
        else if (treatmentTypeCode == "rehab")
        {
            return subModels.TextureResetSimulatorCSRehab.GetSimulatedValue(inputParameters, random);
        }
        else
        {
            throw new InvalidOperationException($"Unknown treatment type code: {treatmentTypeCode}");
        }
    }

    private static double GetTextureDepthResetForACandOGPA(Dictionary<string, object> inputParameters, SubModelDefinitions subModels, string treatmentTypeCode, Random random)
    {
        if (treatmentTypeCode == "resurf")
        {
            return subModels.TextureResetSimulatorACResurf.GetSimulatedValue(inputParameters, random);
        }
        else if (treatmentTypeCode == "rehab")
        {
            return subModels.TextureResetSimulatorACRehab.GetSimulatedValue(inputParameters, random);
        }
        else
        {
            throw new InvalidOperationException($"Unknown treatment type code: {treatmentTypeCode}");
        }
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
        };
    }

    #endregion
}
