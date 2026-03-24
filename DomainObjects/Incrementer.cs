using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Drawing.Charts;
using JCass_ModelCore.Models;

namespace MonteCarloRoadModelV1.DomainObjects;

public class Incrementer
{

    private ModelBase _frameworkModel;
    private MonteCarloRoadModelV1 _domainModel;

    public Incrementer(ModelBase frameworkModel, MonteCarloRoadModelV1 domainModel)
    {
        _frameworkModel = frameworkModel ?? throw new ArgumentNullException(nameof(frameworkModel), "Domain model cannot be null");
        _domainModel = domainModel ?? throw new ArgumentNullException(nameof(domainModel), "Domain model cannot be null");
    }

    public RoadSegmentMC Increment(RoadSegmentMC segment, int period)
    {
        if (segment.ElementIndex == 1272)
        {
            int debug = 0; // Debugging breakpoint
        }

        // Increment all properties related to model parameters
        // Keep the code same order as the model parameter list

        segment.AverageDailyTraffic = segment.AverageDailyTraffic * (1 + segment.TrafficGrowthPercent / 100);
        // No need to reset HCV count as it is automatically calculated based on the AverageDailyTraffic and HCVPercent

        segment.PavementAge = segment.PavementAge + 1;
        segment.PavementRemainingLife = segment.PavementRemainingLife - 1;

        // No need to update Pavement Life Achieved and HCV Risk because it is automatically calculated based on the HCV and Pavement Life Achieved

        // No change in these properties:
        // segment.SurfaceMaterial 
        // segment.SurfaceClass
        // segment.SurfaceThickness            
        // segment.SurfaceNumberOfLayers
        // segment.SurfaceFunction 
        // segment.SurfaceExpectedLife 

        segment.SurfaceAge = segment.SurfaceAge + 1;

        // Note: surface life achieved and surface remaining life are automatically calculated based on the surface age and expected life

        //--------------------------------------------------------------------------------------------------------------------------------------------
        // HSD Increments. For a normal increment situation, we retain the previously assigned Base Increment for the Episode, and then
        // add a residual for the current year
        //--------------------------------------------------------------------------------------------------------------------------------------------

        // Check if we need to draw a new increment for rut and IRI based on the episode length. This will update the RutIncrement and IRIIncrement properties of the segment as needed
        CheckRutAndIRIIncrementForEpisode(segment, _domainModel.Constants.MaximumEpisodeLengthRutAndIRI);

        // Rut Depth        
        double newValue = segment.RutMeanLatent + segment.RutIncrement;
        double standardDeviation = _domainModel.SubModels.RutInrementResidualSDFunction.GetValue(newValue);
        double residual = _domainModel.SubModels.NormalGenerator.NextNormal(0, standardDeviation);
        segment.RutMeanLatent = newValue;
        segment.RutMeanObserved = segment.RutMeanLatent + residual;  // Update the observed rut mean with the residual to reflect the variability in the increment


        // IRI 
        newValue = segment.IRIMeanLatent + segment.IRIIncrement;
        standardDeviation = _domainModel.SubModels.IRIInrementResidualSDFunction.GetValue(newValue);
        residual = _domainModel.SubModels.NormalGenerator.NextNormal(0, standardDeviation);
        segment.IRIMeanLatent = newValue;
        segment.IRIMeanObserved = segment.IRIMeanLatent + residual;

        // Texture Depth
        // Check if we need to draw a new increment for texture based on the episode length. This will update the TextureIncrement property of the segment as needed
        CheckTextureIncrementForEpisode(segment, _domainModel.Constants.MaximumEpisodeLengthTexture);

        newValue = segment.TextureMeanLatent + segment.TextureIncrement;
        standardDeviation = _domainModel.SubModels.TextureInrementResidualSDFunction.GetValue(newValue);
        residual =  _domainModel.SubModels.NormalGenerator.NextNormal(0, standardDeviation);
        segment.TextureMeanLatent = newValue;
        segment.TextureMeanObserved = segment.TextureMeanLatent + residual;

        // Maintenance
        _domainModel.MaintenanceModel.UpdateRoutineMaintenanceExtents(segment);
        

        // Ranking parameters will be calculated by the framework model

        return segment;

    }


    private void CheckRutAndIRIIncrementForEpisode(RoadSegmentMC segment, int maximumEpisodeLength)
    {
        if (segment.RutAndIRIIncrementEpisodeLength < maximumEpisodeLength)
        {
            // No need to draw a new increment for rut and IRI

            //Increment the episode length for rut and IRI increments
            segment.RutAndIRIIncrementEpisodeLength++;
        }
        else
        {
            // Need to draw a new increment for rut and IRI, and reset the episode length
            segment.RutIncrement = GetRutIncrementForEpisode(segment, _domainModel.SubModels, _frameworkModel.Random);
            segment.IRIIncrement = GetIRIIncrementForEpisode(segment, _domainModel.SubModels, _frameworkModel.Random);

            //Reset the episode length to 1
            segment.RutAndIRIIncrementEpisodeLength = 1;
        }        
    }

    private void CheckTextureIncrementForEpisode(RoadSegmentMC segment, int maximumEpisodeLength)
    {
        if (segment.TextureIncrementEpisodeLength < maximumEpisodeLength)
        {
            // No need to draw a new increment for rut and IRI

            //Increment the episode length for rut and IRI increments
            segment.TextureIncrementEpisodeLength++;
        }
        else
        {
            // Need to draw a new increment for rut and IRI, and reset the episode length
            segment.TextureIncrement = GetTextureIncrementForEpisode(segment, _domainModel.SubModels, _frameworkModel.Random);

            //Reset the episode length to 1
            segment.TextureIncrementEpisodeLength = 1;
        }
    }
    

    public static double GetRutIncrementForEpisode(RoadSegmentMC segment, SubModelDefinitions subModels, Random random)
    {        
        return subModels.RutIncrementSimulator.GetSimulatedValue(GetSimulatorInputValues(segment), random);
    }

    public static double GetIRIIncrementForEpisode(RoadSegmentMC segment, SubModelDefinitions subModels, Random random)
    {        
        return subModels.IRIIncrementSimulator.GetSimulatedValue(GetSimulatorInputValues(segment), random);
    }

    public static double GetTextureIncrementForEpisode(RoadSegmentMC segment, SubModelDefinitions subModels, Random random)
    {        
        return subModels.TextureIncrementSimulator.GetSimulatedValue(GetSimulatorInputValues(segment), random);
    }

    private static Dictionary<string, object> GetSimulatorInputValues(RoadSegmentMC segment)
    {
        Dictionary<string, object> inputParameters = new Dictionary<string, object>
        {
            { "rut_mean", segment.RutMeanLatent },
            { "iri_mean", segment.IRIMeanLatent },
            { "surf_age", segment.SurfaceAge },
            { "pre_potfill_mtc_extent", segment.MaintenancePotfill },
            { "pre_all_mtc_extent", segment.MaintenancePavement },
            { "adt", segment.AverageDailyTraffic },
            { "heavy_perc", segment.HeavyVehiclePercentage },
            { "surf_thick", segment.SurfaceThickness },
            {"rainfall", segment.RainfallMM }
        };
        return inputParameters;
    }



}
