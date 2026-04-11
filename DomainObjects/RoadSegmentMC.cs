
using JCass_ModelCore.Models;

namespace MonteCarloRoadModelV1.DomainObjects;


/// <summary>
/// Object representing a road segment with various properties and attributes.
/// </summary>
public class RoadSegmentMC
{

    private double _surfaceAge;
    private double _surfaceAgeBeforeReset;
    private string _surfaceFunction = "unknown"; // Default value for previous surface function
    private string _previousSurfaceFunction = "unknown"; // Default value for previous surface function

    #region Identification

    /// <summary>
    /// Zero-based index of the element in the model. This is set by the Framework Model and is used to identify the element in the model.
    /// </summary>
    public int ElementIndex { get; set; }

    /// <summary>
    /// Short code for identifying the segment in debug/feeback messages
    /// </summary>
    public string FeebackCode
    {
        get
        {
            return $"elem_index: {this.ElementIndex:D4} - {this.SegmentCode}";
        }
    }

    /// <summary>
    /// Segment identifier. Maps to input column "file_seg_name".
    /// </summary>
    public string SegmentCode { get; set; }

    /// <summary>
    /// Section ID. Maps to "file_section_id".
    /// </summary>
    public double SectionID { get; set; }

    /// <summary>
    /// Name of the section. Maps to "file_section_name".
    /// </summary>
    public string SectionName { get; set; }

    /// <summary>
    /// Start metre of the segment. Maps to "file_loc_from".
    /// </summary>
    public double LocFrom { get; set; }

    /// <summary>
    /// End metre of the segment. Maps to "file_loc_to".
    /// </summary>
    public double LocTo { get; set; }

    /// <summary>
    /// Lane code. Maps to "file_lane_name".
    /// </summary>
    public string LaneCode { get; set; }

    #endregion

    #region Quantity 

    /// <summary>
    /// Length of the segment in metres.
    /// </summary>
    public double LengthInMetre { get; set; }

    /// <summary>
    /// Square metre area.
    /// </summary>
    public double AreaSquareMetre { get; set; }

    /// <summary>
    /// Width in metres. By default, this is calculated on initialisation from Area and Length
    /// </summary>
    public double WidthInMetre { get; set; }

    #endregion
    
    #region Surface and Pavement Properties

    private string _surfaceClass;

    /// <summary>
    /// Surface class ('cs', 'ac', 'blocks', 'concrete', 'other').
    /// </summary>
    public string SurfaceClass
    {
        get => _surfaceClass;
        set => _surfaceClass = value?.ToLower();
    }

    public string SurfaceClassForRules
    {
        get
        {
            // Map surface classes to the ones used in the rules. This is needed because some of the 
            // surface classes in the input data are not used in the rules, and need to be mapped to the
            // ones assigned in the Model Development  (Casper) pipeline

            // 'concrete' should be 'other' to match the rules (even though there is a 'conc' in some rules,
            // user 'other' because some model building sets did not contain a 'conc' class. Thus, to be consistent, we map 'concrete' to 'other' for all rules)
            if (this.SurfaceClass == "concrete") return "other";
            
            // 'unknown' should be 'other' to match the rules
             if (this.SurfaceClass == "unknown") return "other";
                        
            return this.SurfaceClass;
        }
    }


    /// <summary>
    /// Flag indicating if the surface is a chip seal. This is calculated based on the SurfaceClass property.
    /// </summary>
    public int SurfaceIsChipSealFlag
    {
        get
        {
            // Return 1 if the surface class is 'cs' (chip seal), otherwise return 0.
            return this.SurfaceClass == "cs" ? 1 : 0;
        }
    }

    /// <summary>
    /// Flag indicating if the surface is either chip seal or asphalt concrete. This is calculated based on the SurfaceClass property.
    /// </summary>
    public int SurfaceIsChipSealOrACFlag
    {
        get
        {
            // Return 1 if the surface class is 'cs' (chip seal) or 'ac' (asphalt concrete), otherwise return 0.
            return this.SurfaceClass == "cs" || this.SurfaceClass == "ac" ? 1 : 0;
        }
    }

    /// <summary>
    /// Surfacing date. Expected to be ISO date in format 'dd/mm/yyyy' in input data, and converted to DateTime during initialisation.
    /// </summary>
    public DateTime SurfacingDate { get; set; }

    /// <summary>
    /// Surfacing date in fractional years, calculated from the SurfacingDateString during Initialisation.
    /// </summary>
    public double SurfaceAge 
    { get {
            return _surfaceAge;
        }
      set {
            _surfaceAgeBeforeReset = this.SurfaceAge;
            _surfaceAge = value;
        } 
    }
    
    /// <summary>
    /// Intermediate variable holding the Surface Age before a reset was applied.
    /// </summary>
    public double SurfaceAgeBeforeReset { get { return _surfaceAgeBeforeReset; } }

    /// <summary>
    /// Surface function.
    /// </summary>
    public string SurfaceFunction
    {
        get { return _surfaceFunction;  }
        set
        {
            _previousSurfaceFunction = _surfaceFunction;
            _surfaceFunction = value;
        }
    }

    /// <summary>
    /// Preceding Surface function - use this to check what the situation was before the last reset.
    /// </summary>
    public string SurfaceFunctionPrevious
    {
        get { return _previousSurfaceFunction; }        
    }

    /// <summary>
    /// Surfacing material.
    /// </summary>
    public string SurfaceMaterial { get; set; }

    /// <summary>
    /// Surfacing expected life (years) from RAMM.
    /// </summary>
    public double SurfaceExpectedLife { get; set; }

    /// <summary>
    /// Read-only code combining Surface Function, Surface Material and Road Class, used to look up the Surface Expected Life from Lookup Tables. 
    /// </summary>
    public string SurfaceExpectedLifeCode
    {
        get
        {
            return $"{this.SurfaceFunction.ToLower()}_{this.SurfaceMaterial.ToLower()}_{this.RoadClass}";
        }
    }

    /// <summary>
    /// Returns the Surface Expective life minus the Surface Age, which gives the remaining life of the surface in years.
    /// </summary>
    public double SurfaceRemainingLife
    {
        get
        {            
            return this.SurfaceExpectedLife - this.SurfaceAge;
        }
    }

        
    /// <summary>
    /// Returns the percentage of the Surface Expected Life that has been achieved based on the Surface Age.
    /// </summary>
    public double SurfaceAchievedLifePercent
    {
        get
        {
            if (this.SurfaceExpectedLife <= 0.0)
            {
                throw new Exception($"Surface expected life is zero or negative for segment {this.FeebackCode}. Surface Age: {this.SurfaceAge}, Expected Life: {this.SurfaceExpectedLife}.");
            }
            // As per JFunctions, limit the value to 200 to prevent very high values from distorting MCDA
            // TODO: Re-think this
            return Math.Min(200, 100 * (this.SurfaceAge / this.SurfaceExpectedLife));
        }
    }

    /// <summary>
    /// Surfacing number of layers
    /// </summary>
    public double SurfaceNumberOfLayers { get; set; }

    /// <summary>
    /// Surfacing thickness in millimetres.
    /// </summary>
    public double SurfaceThickness { get; set; }


    /// <summary>
    /// Pavement construction date. Expected to be ISO date in format 'dd/mm/yyyy' in input data, and converted to DateTime during initialisation.
    /// </summary>
    public DateTime PavementDate { get; set; }

    /// <summary>
    /// Pavement Age in fractional years, calculated from the PavementDateString during Initialisation.
    /// </summary>
    public double PavementAge { get; set; }

    /// <summary>
    /// Age-based pavement remaining life.
    /// </summary>
    public double PavementRemainingLife { get; set; }

    /// <summary>
    /// Returns the percentage of the Expected Pavement Life based on the Pavement Age and Remaining Life.
    /// </summary>
    public double PavementAchievedLife
    {
        get
        {
            double expectedLife = this.PavementAge + this.PavementRemainingLife;
            if (expectedLife <= 0.0)
            {
                throw new Exception($"Pavement expected life is zero or negative for segment {this.FeebackCode}. Pavement Age: {this.PavementAge}, Remaining Life: {this.PavementRemainingLife}.");
            }
            return this.PavementAge / expectedLife * 100.0;
        }
    }
    

    #endregion

    #region ONRC/Carriageway and Rainfall Attributes

    private string _urbanRural;
    private string _onrc;    
    
    /// <summary>
    /// Urban/Rural flag.
    /// </summary>
    public string UrbanRural
    {
        get => _urbanRural;
        set => _urbanRural = value?.ToLower();
    }

    /// <summary>
    /// ONRC Category.
    /// </summary>
    public string ONRC
    {
        get => _onrc;
        set => _onrc = value?.ToLower();
    }
            
    /// <summary>
    /// Annual Rainfall, in mm
    /// </summary>
    public double RainfallMM { get; set; }


    /// <summary>
    /// Road class based on ONRC, with values 'l', 'm' and 'h' for low, medium and high traffic respectively. This classification
    /// collapses the ONRC categories into three classes based on traffic volumes. It is used to refine certain aspects such 
    /// as Expected Surface Life, allowing differentiation but only in 3 classes.
    /// </summary>
    public string RoadClass { get; set; }

    #endregion

    #region Traffic and Growth

    /// <summary>
    /// Average daily traffic.
    /// </summary>
    public double AverageDailyTraffic { get; set; }

    /// <summary>
    /// Heavy vehicle percentage.
    /// </summary>
    public double HeavyVehiclePercentage { get; set; }
    
    /// <summary>
    /// Traffic growth percentage.
    /// </summary>
    public double TrafficGrowthPercent { get; set; }

    /// <summary>
    /// Heavy vehicles per day, calculated as a percentage of Average Daily Traffic using HeavyVehiclePercentage.
    /// </summary>
    public double HeavyVehiclesPerDay
    {
        get
        {
            return this.AverageDailyTraffic * (this.HeavyVehiclePercentage / 100.0);
        }
    }

    #endregion
    
    #region High Speed Data (HSD) (Rut, Roughness, Texture etc.)

    /// <summary>
    /// HSD survey date (expecting ISO date in format 'yyyymmdd' in input data
    /// </summary>
    public DateTime HSDSurveyDate { get; set; }

    
    /// <summary>
    /// Mean IRI (mm/m) for the segment — latent underlying condition state used for deterioration
    /// </summary>
    public double IRIMeanLatent { get; set; }

    /// <summary>
    /// Mean IRI (mm/m) for the segment — observed condition state including random fluctuation
    /// </summary>
    public double IRIMeanObserved { get; set; }

    /// <summary>
    /// Episode Increment for IRI in mm/m/year. 
    /// </summary>
    public double IRIIncrement { get; set; }

    /// <summary>
    /// Mean rut depth (mm) — latent underlying condition state used for deterioration   
    /// </summary>
    public double RutMeanLatent { get; set; }

    /// <summary>
    /// Mean rut depth (mm) — observed condition state including random fluctuation   
    /// </summary>
    public double RutMeanObserved { get; set; }

    /// <summary>
    /// Rut increment in mm/year for the episode    
    /// </summary>
    public double RutIncrement { get; set; }

    /// <summary>
    /// Episode length for Rut and IRI increments, in years. This value is reset, and Rut and IRI increments are re-drawn 
    /// when current episode length exceeds the maximum allowed in Constants.EpisodeLengthRutAndIRI
    /// </summary>
    public int RutAndIRIIncrementEpisodeLength { get; set; }

    /// <summary>
    /// Texture depth mean for segment, in mm — latent underlying condition state used for deterioration
    /// </summary>
    public double TextureMeanLatent { get; set; }

    /// <summary>
    /// Texture depth mean for segment, in mm — observed condition state including random fluctuation
    /// </summary>
    public double TextureMeanObserved { get; set; }

    /// <summary>
    /// Texture depth increment for the episode, in mm/year
    /// </summary>
    public double TextureIncrement { get; set; }

    /// <summary>
    /// Episode length for texture increment, in years. This value is reset, and Increment is re-drawn when current episode length exceeds the
    /// maximum allowed in Constants.EpisodeLengthTexture
    /// </summary>
    public int TextureIncrementEpisodeLength { get; set; }


    #endregion

    #region Maintenance


    /// <summary>
    /// Extent of Pavement-related Routine Maintenance (excluding Pothole Filling), as fraction of total length (value 0 to 1), triggered in the period. 
    /// </summary>
    public double MaintenancePavement { get; set; }

    /// <summary>
    /// Extent of Pothole Filling Routine Maintenance, as fraction of total length (value 0 to 1), triggered in the period. 
    /// </summary>
    public double MaintenancePotfill { get; set; }


    #endregion

    #region Helper Methods

    /// <summary>
    /// Updates the sinks mapping back to parameter values in the model. 
    /// </summary>
    /// <param name="numModParamValues">Return value: Sink holding values for numeric parameters (to be updated by Domain Model). Keys are parameter names, values are assigned values</param>
    /// <param name="textModParamValues">Return value: Sink holding values for text parameters (to be updated by Domain Model). Keys are parameter names, values are assigned values</param>     
    public void SetParameterValues(Action<string, double> numModParamValues, Action<string, string> textModParamValues)
    {
        numModParamValues("par_adt", this.AverageDailyTraffic);
        numModParamValues("par_hcv", this.HeavyVehiclesPerDay);

        numModParamValues("par_pave_age", this.PavementAge);
        numModParamValues("par_pave_remlife", this.PavementRemainingLife);
        numModParamValues("par_pave_life_ach", this.PavementAchievedLife);
        
        textModParamValues("par_surf_mat", this.SurfaceMaterial);
        textModParamValues("par_surf_class", this.SurfaceClass);        
        numModParamValues("par_surf_thick", this.SurfaceThickness);
        numModParamValues("par_surf_layers", this.SurfaceNumberOfLayers);
        textModParamValues("par_surf_func", this.SurfaceFunction);
        numModParamValues("par_surf_exp_life", this.SurfaceExpectedLife);
        numModParamValues("par_surf_age", this.SurfaceAge);
        numModParamValues("par_surf_life_ach", this.SurfaceAchievedLifePercent);
        numModParamValues("par_surf_remain_life", this.SurfaceRemainingLife);
        
        numModParamValues("par_rut_increm", this.RutIncrement);
        numModParamValues("par_rut", this.RutMeanLatent);
        numModParamValues("par_rut_obs", this.RutMeanObserved);
        numModParamValues("par_rut_iri_epi_len", this.RutAndIRIIncrementEpisodeLength);

        numModParamValues("par_iri_increm", this.IRIIncrement);
        numModParamValues("par_iri", this.IRIMeanLatent);
        numModParamValues("par_iri_obs", this.IRIMeanObserved);

        numModParamValues("par_text_increm", this.TextureIncrement);
        numModParamValues("par_text", this.TextureMeanLatent);
        numModParamValues("par_text_obs", this.TextureMeanObserved);
        numModParamValues("par_text_epi_len", this.TextureIncrementEpisodeLength);

        numModParamValues("par_maint_pa", this.MaintenancePavement);
        numModParamValues("par_maint_poth", this.MaintenancePotfill);

        // The following are Network Parameters - to be set automatically by the framework model:        
        //para_sla_rank
        //para_rut_rank
        //para_iri_rank

    }

    
    #endregion

}


