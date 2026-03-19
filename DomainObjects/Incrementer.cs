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

        // Rut Depth
        double newValue = segment.RutMeanLatent + segment.RutIncrement;
        double standardDeviation = _domainModel.SubModels.RutInrementResidualSDFunction.GetValue(segment.RutMeanLatent);
        double residual = _domainModel.SubModels.NormalGenerator.NextNormal(0, standardDeviation);
        segment.RutMeanLatent = newValue;
        segment.RutMeanObserved = segment.RutMeanLatent + residual;  // Update the observed rut mean with the residual to reflect the variability in the increment


        // IRI 
        newValue = segment.IRIMeanLatent + segment.IRIIncrement;
        standardDeviation = _domainModel.SubModels.IRIInrementResidualSDFunction.GetValue(segment.IRIMeanLatent);
        residual = _domainModel.SubModels.NormalGenerator.NextNormal(0, standardDeviation);
        segment.IRIMeanLatent = newValue;
        segment.IRIMeanObserved = segment.IRIMeanLatent + residual;

        // Texture Depth
        newValue = segment.TextureMeanLatent + segment.TextureIncrement;
        standardDeviation = _domainModel.SubModels.TextureInrementResidualSDFunction.GetValue(segment.TextureMeanLatent);
        residual =  _domainModel.SubModels.NormalGenerator.NextNormal(0, standardDeviation);
        segment.TextureMeanLatent = newValue;
        segment.TextureMeanObserved = segment.TextureMeanLatent + residual;

        // Maintenance
        UpdateRoutineMaintenanceExtents(segment);


        // Ranking parameters will be calculated by the framework model

        return segment;

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


    /// <summary>
    /// Updates the maintenance extent for PA (excluding potfill) and potfill maintenance based on the 
    /// predicted probabilities and extents from the domain model.
    /// </summary>
    /// <param name="segment"></param>
    private void UpdateRoutineMaintenanceExtents(RoadSegmentMC segment)
    {
        // Deal first with PA maintenance (excluding potfill) 
        double probabilityOfMaintenance = GetMaintenanceProbabilityPA(segment);
        double randomValue = _frameworkModel.Random.NextDouble();
        if (randomValue < probabilityOfMaintenance)
        {
            segment.MaintenancePavement = GetMaintenanceExtentPA(segment);
        }
        else
        {
            segment.MaintenancePavement = 0;            
        }

        // Now deal with potfill maintenance
        probabilityOfMaintenance = GetMaintenanceProbabilityPotFill(segment);
        randomValue = _frameworkModel.Random.NextDouble();
        if (randomValue < probabilityOfMaintenance)
        {
            segment.MaintenancePotfill = GetMaintenanceExtentPotfill(segment);
        }
        else
        {
            segment.MaintenancePotfill = 0;
        }


    }

    /// <summary>
    /// Gets the probability of PA maintenance (excluding potfill) for the given road segment based on the appropriate model for the surface class.
    /// </summary>
    /// <param name="segment"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private double GetMaintenanceProbabilityPA(RoadSegmentMC segment)
    {        
        Dictionary<string, double> inputParameters = new Dictionary<string, double>
        {
            { "rut_mean_pre", segment.RutMeanLatent },
            { "iri_mean_pre", segment.IRIMeanLatent },
            { "surf_age", segment.SurfaceAge },
            { "pre_potfill_mtc_extent", segment.MaintenancePotfill },
            { "pre_all_mtc_extent", segment.MaintenancePavement },
            { "log(adt)", Math.Log(segment.AverageDailyTraffic) },
            { "heavy_perc", segment.HeavyVehiclePercentage },
        };

        if (segment.SurfaceClass == "cs" || segment.SurfaceClass == "slurry")
        {
            return _domainModel.SubModels.MaintPaProbabilityModelCS.PredictProbability(inputParameters);
        }
        else if (segment.SurfaceClass == "ac" || segment.SurfaceClass == "ogpa")
        {
            return _domainModel.SubModels.MaintPaProbabilityModelAC.PredictProbability(inputParameters);
        }
        if (segment.SurfaceClass == "concrete")
        {
            return 0.0;   //Not enough data. TODO: explore potfill model for concrete
        }
        if (segment.SurfaceClass == "unknown")
        {
            //For unknown, return value for CS and Slurry as a best-guess
            return _domainModel.SubModels.MaintPaProbabilityModelCS.PredictProbability(inputParameters);
        }
        else
        {
            throw new InvalidOperationException($"Unknown surface class: {segment.SurfaceClass}");
        }
    }


    /// <summary>
    /// Gets the probability of potfill maintenance for the given road segment based on the appropriate model for the surface class.
    /// </summary>
    /// <param name="segment"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private double GetMaintenanceProbabilityPotFill(RoadSegmentMC segment)
    {
        Dictionary<string, double> inputParameters = new Dictionary<string, double>
        {
            { "rut_mean_pre", segment.RutMeanLatent },
            { "iri_mean_pre", segment.IRIMeanLatent },
            { "surf_age", segment.SurfaceAge },
            { "pre_potfill_mtc_extent", segment.MaintenancePotfill },
            { "pre_all_mtc_extent", segment.MaintenancePotfill },
            { "log(adt)", Math.Log(segment.AverageDailyTraffic) },
            { "heavy_perc", segment.HeavyVehiclePercentage },
        };

        if (segment.SurfaceClass == "cs" || segment.SurfaceClass == "slurry")
        {
            return _domainModel.SubModels.PotfillProbabilityModelCS.PredictProbability(inputParameters);
        }
        else if (segment.SurfaceClass == "ac" || segment.SurfaceClass == "ogpa")
        {
            return _domainModel.SubModels.PotfillProbabilityModelAC.PredictProbability(inputParameters);
        }
        if (segment.SurfaceClass == "concrete")
        {
            return 0.0;   //Not enough data. TODO: explore potfill model for concrete
        }
        if (segment.SurfaceClass == "unknown")
        {
            //For unknown, return value for CS and Slurry as a best-guess
            return _domainModel.SubModels.MaintPaProbabilityModelCS.PredictProbability(inputParameters);
        }
        else
        {
            throw new InvalidOperationException($"Unknown surface class: {segment.SurfaceClass}");
        }
    }

    /// <summary>
    /// Gets a simulated maintenance extent for PA maintenance (excluding potfill) for the given road segment based on the appropriate model for the surface class.
    /// </summary>
    /// <param name="segment"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private double GetMaintenanceExtentPA(RoadSegmentMC segment)
    {
        Dictionary<string, object> inputParameters = new Dictionary<string, object>
        {
            { "rut_mean_pre", segment.RutMeanLatent },
            { "iri_mean_pre", segment.IRIMeanLatent },
            { "surf_age", segment.SurfaceAge },
            { "pre_potfill_mtc_extent", segment.MaintenancePotfill },
            { "pre_all_mtc_extent", segment.MaintenancePavement },
            { "adt", segment.AverageDailyTraffic },
            { "heavy_perc", segment.HeavyVehiclePercentage },
            { "surf_thick", segment.SurfaceThickness },
        };

        if (segment.SurfaceClass == "cs" || segment.SurfaceClass == "slurry")
        {
            return _domainModel.SubModels.MaintenanceExtentPAForCSandSlurry.GetSimulatedValue(inputParameters, _frameworkModel.Random);
        }
        else if (segment.SurfaceClass == "ac" || segment.SurfaceClass == "ogpa")
        {
            return _domainModel.SubModels.MaintenanceExtentPAForACandOgpa.GetSimulatedValue(inputParameters, _frameworkModel.Random);
        }
        if (segment.SurfaceClass == "concrete")
        {
            return 0.0;   //Not enough data. TODO: explore potfill model for concrete
        }
        if (segment.SurfaceClass == "unknown")
        {
            //For unknown, return value for CS and Slurry as a best-guess
            return _domainModel.SubModels.MaintenanceExtentPAForCSandSlurry.GetSimulatedValue(inputParameters, _frameworkModel.Random);
        }
        else
        {
            throw new InvalidOperationException($"Unknown surface class: {segment.SurfaceClass}");
        }
    }


    /// <summary>
    /// Gets a simulated maintenance extent for potfill maintenance for the given road segment based on the appropriate model for the surface class.
    /// </summary>
    /// <param name="segment"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private double GetMaintenanceExtentPotfill(RoadSegmentMC segment)
    {
        Dictionary<string, object> inputParameters = new Dictionary<string, object>
        {
            { "rut_mean_pre", segment.RutMeanLatent },
            { "iri_mean_pre", segment.IRIMeanLatent },
            { "surf_age", segment.SurfaceAge },
            { "pre_potfill_mtc_extent", segment.MaintenancePotfill },
            { "pre_all_mtc_extent", segment.MaintenancePavement },
            { "adt", segment.AverageDailyTraffic },
            { "heavy_perc", segment.HeavyVehiclePercentage },
            { "surf_thick", segment.SurfaceThickness },
        };

        if (segment.SurfaceClass == "cs" || segment.SurfaceClass == "slurry")
        {
            return _domainModel.SubModels.MaintenanceExtentPotfillCSandSlurry.GetSimulatedValue(inputParameters, _frameworkModel.Random);
        }
        else if (segment.SurfaceClass == "ac" || segment.SurfaceClass == "ogpa")
        {
            return _domainModel.SubModels.MaintenanceExtentPotfillACandOgpa.GetSimulatedValue(inputParameters, _frameworkModel.Random);
        }
        if (segment.SurfaceClass == "concrete")
        {
            return 0.0;   //Not enough data. TODO: explore potfill model for concrete
        }
        if (segment.SurfaceClass == "unknown")
        {
            //For unknown, return value for CS and Slurry as a best-guess
            return _domainModel.SubModels.MaintenanceExtentPotfillCSandSlurry.GetSimulatedValue(inputParameters, _frameworkModel.Random);
        }
        else
        {
            throw new InvalidOperationException($"Unknown surface class: {segment.SurfaceClass}");
        }
    }

}
