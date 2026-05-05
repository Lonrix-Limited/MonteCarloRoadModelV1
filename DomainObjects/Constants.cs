
using JCass_Core.Statistics;

namespace MonteCarloRoadModelV1.DomainObjects;

/// <summary>
/// General model constants set up from model Lookup sets.
/// </summary>
public class Constants
{

    #region Backing Variables

    private DateTime _baseDate; 
    private int _shortTermPeriod;

    // Related to Candidate Selection
    private double _minSlaToTreatAc;
    private double _minSlaToTreatCs;    
    private double _min_periods_to_next_treat;
    private double _min_sdi_to_treat;
    private double _min_pdi_to_treat;


    private double _potholeBoostFactor;

    private double _maintenanceCostCalibrationFactor;
    private double _maintenanceCostPDIThreshold;

    // Related to TSS (Treatment Suitability Scores - MCDA)
    private double _rehabExcessRutThresh;
    private double _rehabExcessRutFact;
    private double _rehabPdiRank;
    private double _holdingPdiRankPt1;
    private double _holdingPdiRankPt2;
    private double _holdingPdiRankPt3;
    private double _holdingMaxRut;

    private double _preserveSdiRank;

    private double _preserveMaxPdiChipSeal;
    private double _preserveMaxPdiAC;
    private double _holdingMaxPdiAC;

    private double _preserveMaxRut;    
    private double _preserveMinSla;           

    // Related to MCDA Treatment Triggering
    private double _maxSlaForACHeavyMaint;
    private int _minPeriodsBetweenACHeavyMaint;


    // Episode Lengths for deterioration rates
    private int _episodeLengthRutAndIRI;
    private int _episodeLengthTexture;

    //public int DebugLogCounter = 0;

    #endregion

    /// <summary>
    /// Base date for the model run. Maps to lookup set "gernal" and setting key "base_date".
    /// </summary>
    public DateTime BaseDate { get { return _baseDate; } }

    #region Candidate Selection related constants

    /// <summary>
    /// Number of modelling periods considered short term for purposes of trigger adjustment. Used in Candidate Selection.
    /// </summary>
    public int CSShortTermPeriod
    {
        get { return _shortTermPeriod; }     
    }
    
    /// <summary>
    /// Minimum Surface Life Achieved to consider for AC - gatekeeper that can be used to throttle treatments
    /// </summary>
    public double CSMinSlaToTreatAc
    {
        get { return _minSlaToTreatAc; }
    }

    /// <summary>
    /// Minimum periods to next treatment (i.e. do not consider treatment if periods to a committed future treatment is less than this)
    /// </summary>
    public double CSMinPeriodsToNextTreat
    {
        get { return _min_periods_to_next_treat; }
    }


    /// <summary>
    /// Minimum Surface Life Achieved to consider for Chipseals - gatekeeper that can be used to throttle treatments
    /// </summary>
    public double CSMinSlaToTreatCs
    {
        get { return _minSlaToTreatCs; }
    }

    /// <summary>
    /// Minimum Surface Distress Index (SDI) to consider for treatment (EITHER condition applied with minimum PDI)
    /// </summary>
    public double CSMinSDIToTreat
    {
        get { return _min_sdi_to_treat; }       
    }

    /// <summary>
    /// Minimum Pavemenbt Distress Index (PDI) to consider for treatment.  (EITHER condition applied with minimum SDI)    
    /// </summary>
    public double CSMinPDIToTreat
    {
        get { return _min_pdi_to_treat; }
    }

    #endregion

    #region Treatment Selection MCDA

    /// <summary>
    /// Rut threshold above which a penalty(for Holding Actions) or boost(for Rehabs) is applied(see below)
    /// </summary>
    public double TSSRehabExcessRutThresh
    {
        get { return _rehabExcessRutThresh; }
    }

    /// <summary>
    /// Multiply excessive rut with this value to get the boost for Rehab TSS based on excessive rut(if any)
    /// </summary>
    public double TSSRehabExcessRutFact
    {
        get { return _rehabExcessRutFact; }
    }

    /// <summary>
    /// PDI rank below which TSS score for Rehab becomes zero (i.e. no rehab if PDI is below this value)
    /// </summary>
    public double TSSRehabPdiRank
    {
        get { return _rehabPdiRank; }
    }

    /// <summary>
    /// PDI rank below which TSS score for Holding Action becomes zero (i.e. no holding action if PDI is below this value)
    /// </summary>
    public double TSSHoldingPdiRankPt1
    {
        get { return _holdingPdiRankPt1; }
    }


    /// <summary>
    /// PDI rank at which score for holding action is maximal(100)
    /// </summary>
    public double TSSHoldingPdiRankPt2
    {
        get { return _holdingPdiRankPt2; }
    }
    
    /// <summary>
    /// TSS for holding action based on PDI when PDI rank is 100
    /// </summary>
    public double TSSHoldingPdiRankPt3
    {
        get { return _holdingPdiRankPt3; }
    }

    /// <summary>
    /// Do not consider holding action if rut is above this value (unless it is not a rehab route in which case it is ignored)
    /// </summary>
    public double TSSHoldingMaxRut
    {
        get { return _holdingMaxRut; }
    }

    
    /// <summary>
    /// SDI Rank below which score for Preservation becomes zero (we want to apply preservation where there is some surface distress)
    /// </summary>
    public double TSSPreserveSdiRank
    {
        get { return _preserveSdiRank; }
    }
        
    /// <summary>
    /// Do not consider Preservation ChipSeal treatment if PDI is above this value 
    /// </summary>
    public double TSSPreserveMaxPdiChipseal
    {
        get { return _preserveMaxPdiChipSeal; }
    }

    /// <summary>
    /// Do not consider Preservation AC treatment if PDI is above this value 
    /// </summary>
    public double TSSPreserveMaxPdiAC
    {
        get { return _preserveMaxPdiAC; }
    }

    /// <summary>
    /// Do not consider Holding AC treatment if PDI is above this value 
    /// </summary>
    public double TSSHoldingMaxPdiAC
    {
        get { return _holdingMaxPdiAC; }
    }

    /// <summary>
    /// Do not consider preservation if rut is above this value
    /// </summary>
    public double TSSPreserveMaxRut
    {
        get { return _preserveMaxRut; }
    }

    /// <summary>
    /// Do not consider preservation if Surface Life Achieved % is below this value
    /// </summary>
    public double TSSPreserveMinSla
    {
        get { return _preserveMinSla; }
    }

    /// <summary>
    /// Maximum Surface Life Achieved % to consider AC Heavy Maintenance (i.e. do not consider AC Heavy Maintenance if SLA is above this value)
    /// </summary>
    public double MaxSlaForACHeavyMaint
    {
        get { return _maxSlaForACHeavyMaint; }
    }

    /// <summary>
    /// Minimum number of periods between AC Heavy Maintenance treatment and any other previous treatment (excluding Routine Maintenance)
    /// </summary>
    public int MinPeriodsBetweenACHeavyMaint
    {
        get { return _minPeriodsBetweenACHeavyMaint; }
    }

    /// <summary>
    /// Maximum episode length for assigned deterioration rate for Rut and IRI. If last draw of random rate is longer than this ago, then draw new
    /// </summary>
    public int MaximumEpisodeLengthRutAndIRI
    {
        get { return _episodeLengthRutAndIRI; }
    }

    /// <summary>
    /// Maximum episode length for assigned deterioration rate for Texture Depth. If last draw of random rate is longer than this ago, then draw new
    /// </summary>
    public int MaximumEpisodeLengthTexture
    {
        get { return _episodeLengthTexture; }
    }

    #endregion

    #region Calibration Factors

    // --- Residuals ---

    private double _calFactRutResiduals;
    private double _calFactIriResiduals;
    private double _calFactTextureResiduals;

    // --- Maintenance ---

    private double _calFactPaProba;
    private double _calFactPotfillProba;
    private double _calFactPaExtent;
    private double _calFactPotfillExtent;
    private double _calFactRutReduc;
    private double _calFactIriReduc;
    private double _calMinRutReducPaMaint;
    private double _calMinIRIReducPaMaint;

    // --- Increments ---

    private PieceWiseLinearModel _calFactRutIncrement;
    private PieceWiseLinearModel _calFactIriIncrement;
    private PieceWiseLinearModel _calFactTextureIncrement;
    private double _calMaxRutIncrement;
    private double _calMaxIriIncrement;

    // --- Resets ---

    private double _calFactRutReset;
    private double _calFactIriReset;
    private double _calFactTextureReset;

    private Dictionary<string, double> _resetAdjustmentFactorsIRI;
    private Dictionary<string, double> _resetAdjustmentFactorsRut;


    // ---- Residual calibration properties ----

    /// <summary>
    /// Calibration factor for Rut residuals. Residual is multiplied by this factor. Set to zero to remove residual effects completely.
    /// Lookup set: cal_residuals, key: rut.
    /// </summary>
    public double CalFactRutResiduals
    {
        get { return _calFactRutResiduals; }
    }

    /// <summary>
    /// Calibration factor for IRI residuals. Residual is multiplied by this factor. Set to zero to remove residual effects completely.
    /// Lookup set: cal_residuals, key: iri.
    /// </summary>
    public double CalFactIriResiduals
    {
        get { return _calFactIriResiduals; }
    }

    /// <summary>
    /// Calibration factor for Texture residuals. Residual is multiplied by this factor. Set to zero to remove residual effects completely.
    /// Lookup set: cal_residuals, key: texture.
    /// </summary>
    public double CalFactTextureResiduals
    {
        get { return _calFactTextureResiduals; }
    }

    // ---- Maintenance calibration properties ----

    /// <summary>
    /// Calibration factor for probability of PA maintenance (excluding Pothole filling). Decreasing this decreases probability of PA maintenance.
    /// Lookup set: cal_maintenance, key: pa_proba.
    /// </summary>
    public double CalFactPaMaintenanceProbability
    {
        get { return _calFactPaProba; }
    }

    /// <summary>
    /// Calibration factor for probability of Pothole Filling maintenance. Decreasing this decreases probability of Pothole Filling.
    /// Lookup set: cal_maintenance, key: potfill_proba.
    /// </summary>
    public double CalFactPotfillProbability
    {
        get { return _calFactPotfillProba; }
    }

    /// <summary>
    /// Calibration factor for extent of PA Maintenance. Sampled extent is multiplied by this factor to reduce or increase.
    /// Lookup set: cal_maintenance, key: pa_extent.
    /// </summary>
    public double CalFactPaMaintenanceExtent
    {
        get { return _calFactPaExtent; }
    }

    /// <summary>
    /// Calibration factor for extent of Pothole filling. Sampled extent is multiplied by this factor to reduce or increase.
    /// Lookup set: cal_maintenance, key: potfill_extent.
    /// </summary>
    public double CalFactPotfillMaintenanceExtent
    {
        get { return _calFactPotfillExtent; }
    }

    /// <summary>
    /// Extent of PA maintenance at which Rut-reduction to to maintenance kicks in. Range should be 0 to 1. Value of greater than one 
    /// effectively removes rut reduction due to PA Maint.
    /// Lookup set: cal_maintenance, key: rut_reduc.
    /// </summary>
    public double RutReductionDueToPAMaintenanceThreshold
    {
        get { return _calFactRutReduc; }
    }

    /// <summary>
    /// Extent of PA maintenance at which IRI-reduction to to maintenance kicks in. Range should be 0 to 1. Value of greater than one 
    /// Lookup set: cal_maintenance, key: iri_reduc.
    /// </summary>
    public double IriReductionDueToPAMaintenanceThreshold
    {
        get { return _calFactIriReduc; }
    }

    /// <summary>
    /// Minimum Rut reduction due to PA Maintenance allowed (in mm). Set this to a low negative to prevent PA maint from making Rut much worse. For
    /// example, since negative reduction means rut gets worse, setting this to -2 means that if sampled reduction is -3mm, it will be 
    /// calibrated to -2mm (i.e. only 2mm worse instead of 3mm worse). Set this to zero or a positive value to prevent PA maintenance 
    /// from making Rut worse at all.
    /// </summary>
    public double CalFactMinRutReductionDueToPAMaintenance
    {
        get { return _calMinRutReducPaMaint; }
    }


    /// <summary>
    /// Minimum IRI reduction due to PA Maintenance allowed (in mm/m). Set this to a low negative to prevent PA maint from making IRI much worse. For
    /// example, since negative reduction means IRI gets worse, setting this to -0.5 mm/m means that if sampled reduction is -1.50 mm/m, it will be
    /// calibrated to -0.5 mm/m (i.e. only 0.5 mm/m worse instead of 1.5 mm/m worse). Set this to zero or a positive value to prevent PA maintenance
    /// from making IRI worse at all.
    /// </summary>
    public double CalFactMinIriReductionDueToPAMaintenance
    {
        get { return _calMinIRIReducPaMaint; }
    }

    // ---- Increment calibration properties ----

    /// <summary>
    /// Calibration factor for episodic increment for Rut. Sampled value is multiplied by this factor to reduce or increase.
    /// Lookup set: cal_increments, key: rut.
    /// </summary>
    public PieceWiseLinearModel CalFactRutIncrement
    {
        get { return _calFactRutIncrement; }
    }

    /// <summary>
    /// Maximum Rut increment allowed for an episode, in mm/year. Set this to a low value to prevent excessively high increments for some episodes. 
    /// Set this to a very high value to effectively have no maximum.
    /// </summary>
    public double CalMaxRutIncrement
    {
        get { return _calMaxRutIncrement; }
    }

    /// <summary>
    /// Calibration factor for episodic increment for IRI. Sampled value is multiplied by this factor to reduce or increase.
    /// Lookup set: cal_increments, key: iri.
    /// </summary>
    public PieceWiseLinearModel CalFactIriIncrement
    {
        get { return _calFactIriIncrement; }
    }

    /// <summary>
    /// Maximum IRI increment allowed for an episode, in mm/m/year. Set this to a low value to prevent excessively high increments for some episodes.
    /// Set this to a very high value to effectively have no maximum.
    /// </summary>
    public double CalMaxIriIncrement
    {
        get { return _calMaxIriIncrement; }
    }

    /// <summary>
    /// Calibration factor for episodic increment for Texture. Sampled value is multiplied by this factor to reduce or increase.
    /// Lookup set: cal_increments, key: texture.
    /// </summary>
    public PieceWiseLinearModel CalFactTextureIncrement
    {
        get { return _calFactTextureIncrement; }
    }

    // ---- Reset calibration properties ----

    /// <summary>
    /// Calibration factor for Rut reset. Sampled reset value is multiplied by this factor to increase or decrease reset value.
    /// Lookup set: cal_resets, key: rut.
    /// </summary>
    public double CalFactRutReset
    {
        get { return _calFactRutReset; }
    }

    /// <summary>
    /// Calibration factor for IRI reset. Sampled reset value is multiplied by this factor to increase or decrease reset value.
    /// Lookup set: cal_resets, key: iri.
    /// </summary>
    public double CalFactIriReset
    {
        get { return _calFactIriReset; }
    }

    /// <summary>
    /// Calibration factor for Texture reset. Sampled reset value is multiplied by this factor to increase or decrease reset value.
    /// Lookup set: cal_resets, key: texture.
    /// </summary>
    public double CalFactTextureReset
    {
        get { return _calFactTextureReset; }
    }

    /// <summary>
    /// Adjustment factors for IRI resets based on Treatment Type. This value is added to the reset value after multiplying by the CalFactIriReset factor.
    /// Lookup set: cal_reset_adj_iri, key: with sub-keys for each treatment type.
    /// </summary>
    public Dictionary<string, double> ResetAdjustmentFactorsIRI
    {
        get { return _resetAdjustmentFactorsIRI; }
    }

    /// <summary>
    /// Adjustment factors for Rut resets based on Treatment Type. This value is added to the reset value after multiplying by the CalFactRutReset factor.
    /// Lookup set: cal_reset_adj_rut, key: with sub-keys for each treatment type.
    /// </summary>
    public Dictionary<string, double> ResetAdjustmentFactorsRut
    {
        get { return _resetAdjustmentFactorsRut; }
    }

    #endregion


    public Constants(Dictionary<string, Dictionary<string, object>> lookupSets)
    {        
        _baseDate = JCass_Core.Utils.HelperMethods.ParseDateNoTime(lookupSets["general"]["base_date"]);
        _shortTermPeriod = Convert.ToInt32(lookupSets["general"]["short_term_periods"]);

        // Candidate Selection related constants
        _min_periods_to_next_treat = Convert.ToInt32(lookupSets["candidate_selection"]["min_periods_to_next_treat"]);
        _min_sdi_to_treat = Convert.ToDouble(lookupSets["candidate_selection"]["min_sdi_to_treat"]);
        _min_pdi_to_treat = Convert.ToDouble(lookupSets["candidate_selection"]["min_pdi_to_treat"]);
        _minSlaToTreatAc = Convert.ToDouble(lookupSets["candidate_selection"]["min_sla_to_treat_ac"]);
        _minSlaToTreatCs = Convert.ToDouble(lookupSets["candidate_selection"]["min_sla_to_treat_cs"]);
               
        _maintenanceCostCalibrationFactor = Convert.ToDouble(lookupSets["maint_pred"]["cal_maint_pred"]);
        _maintenanceCostPDIThreshold = Convert.ToDouble(lookupSets["maint_pred"]["maint_pdi_threshold"]);

        // Related to TSS
        _rehabExcessRutThresh = Convert.ToDouble(lookupSets["treatment_suitability_scores"]["rehab_excess_rut_thresh"]);
        _rehabExcessRutFact = Convert.ToDouble(lookupSets["treatment_suitability_scores"]["rehab_excess_rut_fact"]);
        _rehabPdiRank = Convert.ToDouble(lookupSets["treatment_suitability_scores"]["rehab_pdi_rank"]);
        _holdingPdiRankPt1 = Convert.ToDouble(lookupSets["treatment_suitability_scores"]["holding_pdi_rank_pt1"]);
        _holdingPdiRankPt2 = Convert.ToDouble(lookupSets["treatment_suitability_scores"]["holding_pdi_rank_pt2"]);
        _holdingPdiRankPt3 = Convert.ToDouble(lookupSets["treatment_suitability_scores"]["holding_pdi_rank_pt3"]);
        _holdingMaxRut = Convert.ToDouble(lookupSets["treatment_suitability_scores"]["holding_max_rut"]);
        _preserveSdiRank = Convert.ToDouble(lookupSets["treatment_suitability_scores"]["preserve_sdi_rank"]);
        
        _preserveMaxPdiChipSeal = Convert.ToDouble(lookupSets["treatment_suitability_scores"]["preserve_max_pdi_chipseal"]);
        _preserveMaxPdiAC = Convert.ToDouble(lookupSets["treatment_suitability_scores"]["preserve_max_pdi_ac"]);
        _holdingMaxPdiAC = Convert.ToDouble(lookupSets["treatment_suitability_scores"]["holding_max_pdi_ac"]);

        _preserveMaxRut = Convert.ToDouble(lookupSets["treatment_suitability_scores"]["preserve_max_rut"]);
        _preserveMinSla = Convert.ToDouble(lookupSets["treatment_suitability_scores"]["preserve_min_sla"]);

        // Related to MCDA Treatment Triggering
        _maxSlaForACHeavyMaint = Convert.ToDouble(lookupSets["mcda_treatment_triggering"]["ac_hmaint_maximum_sla"]);
        _minPeriodsBetweenACHeavyMaint = Convert.ToInt32(lookupSets["mcda_treatment_triggering"]["ac_hmaint_min_periods_between"]);

        _episodeLengthRutAndIRI = Convert.ToInt32(lookupSets["episode_length_max"]["rut_and_iri"]);
        _episodeLengthTexture = Convert.ToInt32(lookupSets["episode_length_max"]["texture"]);

        // Calibration factors - Residuals
        _calFactRutResiduals = Convert.ToDouble(lookupSets["cal_residuals"]["rut"]);
        _calFactIriResiduals = Convert.ToDouble(lookupSets["cal_residuals"]["iri"]);
        _calFactTextureResiduals = Convert.ToDouble(lookupSets["cal_residuals"]["texture"]);

        // Calibration factors - Maintenance
        _calFactPaProba = Convert.ToDouble(lookupSets["cal_maintenance"]["pa_proba"]);
        _calFactPotfillProba = Convert.ToDouble(lookupSets["cal_maintenance"]["potfill_proba"]);
        _calFactPaExtent = Convert.ToDouble(lookupSets["cal_maintenance"]["pa_extent"]);
        _calFactPotfillExtent = Convert.ToDouble(lookupSets["cal_maintenance"]["potfill_extent"]);
        _calFactRutReduc = Convert.ToDouble(lookupSets["cal_maintenance"]["rut_reduc"]);
        _calFactIriReduc = Convert.ToDouble(lookupSets["cal_maintenance"]["iri_reduc"]);
        _calMinRutReducPaMaint = Convert.ToDouble(lookupSets["cal_maintenance"]["rut_reduc_min"]);
        _calMinIRIReducPaMaint = Convert.ToDouble(lookupSets["cal_maintenance"]["iri_reduc_min"]);

        // Calibration factors - Increments
        _calFactRutIncrement = new PieceWiseLinearModel(lookupSets["cal_increments"]["rut"].ToString(),false);
        _calFactIriIncrement = new PieceWiseLinearModel(lookupSets["cal_increments"]["iri"].ToString(),false);
        _calFactTextureIncrement = new PieceWiseLinearModel(lookupSets["cal_increments"]["texture"].ToString(),false);
        _calMaxRutIncrement = Convert.ToDouble(lookupSets["cal_increments"]["rut_max"]);
        _calMaxIriIncrement = Convert.ToDouble(lookupSets["cal_increments"]["iri_max"]);

        // Calibration factors - Resets
        _calFactRutReset = Convert.ToDouble(lookupSets["cal_resets"]["rut"]);
        _calFactIriReset = Convert.ToDouble(lookupSets["cal_resets"]["iri"]);
        _calFactTextureReset = Convert.ToDouble(lookupSets["cal_resets"]["texture"]);

        _resetAdjustmentFactorsIRI = new Dictionary<string, double>();
        foreach (var key in lookupSets["cal_reset_adj_iri"].Keys)
        {
            _resetAdjustmentFactorsIRI[key] = Convert.ToDouble(lookupSets["cal_reset_adj_iri"][key]);
        }

        _resetAdjustmentFactorsRut = new Dictionary<string, double>();
        foreach (var key in lookupSets["cal_reset_adj_rut"].Keys)
        {
            _resetAdjustmentFactorsRut[key] = Convert.ToDouble(lookupSets["cal_reset_adj_rut"][key]);
        }

    }




}
