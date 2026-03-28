using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using JCass_Core.Statistics;
using JCass_Data.Objects;
using JCass_Data.Utils;
using JCass_Functions.Engineering;
using JCass_ModelCore.MonteCarlo;
using MonteCarloRoadModelV1.DomainObjects;

namespace MonteCarloRoadModelV1.Utilities;

public static class SetupUtilities
{

    /// <summary>
    /// Helper function to setup the increment residual SD functions for the MonteCarloRoadModelV1 domain model. This reads in the setup codes from a CSV file and 
    /// creates the PieceWiseLinearModelGeneric instances for each of the three parameters (rut, IRI, texture). The setup codes in the
    /// CSV file should be in the format expected by the PieceWiseLinearModelGeneric constructor. The created models are then assigned to the domain model instance.
    /// </summary>
    /// <param name="domainModel">Monte Carlo DomainModel</param>
    /// <param name="workFolder">workfolder to find the path to setup CSV files</param>
    /// <exception cref="Exception"></exception>
    public static void SetupIncrementResidualModels(DomainObjects.MonteCarloRoadModelV1 domainModel, string workFolder)
    {
        string incrementResidualSDSetupFile = System.IO.Path.Combine(workFolder, @"domain_model/inc_resids_plm_setup_codes.csv");
        if (!System.IO.File.Exists(incrementResidualSDSetupFile))
        {
            throw new Exception($"Increment Residual SD setup file not found at: {incrementResidualSDSetupFile}");
        }
        jcDataSet allSetupData = CSVHelper.ReadDataFromCsvFile(incrementResidualSDSetupFile);
        allSetupData.SetupRowKeys("parameter");

        domainModel.SubModels.RutInrementResidualSDFunction = GetPieceWiseLinearModel("rut_inc_resid", allSetupData);
        domainModel.SubModels.IRIInrementResidualSDFunction = GetPieceWiseLinearModel("iri_inc_resid", allSetupData);
        domainModel.SubModels.TextureInrementResidualSDFunction = GetPieceWiseLinearModel("text_inc_resid", allSetupData);
    }

    /// <summary>
    /// Setup helper function to setup the distribution simulators for the MonteCarloRoadModelV1 domain model. This reads in the setup data 
    /// from a CSV file and creates DistributionSimulator instances for each of the three parameters (rut, IRI, texture). The created 
    /// simulators are then assigned to the domain model instance.
    /// </summary>
    /// <param name="domainModel">Monte Carlo domain model</param>
    /// <param name="workFolder">workfolder to find the path to setup CSV files</param>
    /// <exception cref="Exception"></exception>
    public static void SetupDistributionSimulators(DomainObjects.MonteCarloRoadModelV1 domainModel, string workFolder)
    {
        //------------------------------------  Set up distribution simulators for increments------------------------------------

        string distributionSetupFile = System.IO.Path.Combine(workFolder, @"domain_model/increms_cohort_rule_plm_setup_for_cassandra.csv");
        if (!System.IO.File.Exists(distributionSetupFile)) throw new Exception($"Distribution setup file for Increments not found at: {System.IO.Path.GetFileName(distributionSetupFile)}");
        
        jcDataSet allSetupData = CSVHelper.ReadDataFromCsvFile(distributionSetupFile);

        domainModel.SubModels.RutIncrementSimulator = SetupUtilities.GetDistributionSimulator("rut_inc", allSetupData);
        domainModel.SubModels.IRIIncrementSimulator = SetupUtilities.GetDistributionSimulator("iri_inc", allSetupData);
        domainModel.SubModels.TextureIncrementSimulator = SetupUtilities.GetDistributionSimulator("text_not_ac_inc", allSetupData);

        //------------------------------------  Set up distribution simulators for Maintenance Extent (PA and Potfill) ------------------------------------

        // First all PA excluding maintenance
        distributionSetupFile = System.IO.Path.Combine(workFolder, @"domain_model/maint_pa_extent_cohort_rule_plm_setup_for_cassandra.csv");
        if (!System.IO.File.Exists(distributionSetupFile)) throw new Exception($"Distribution setup file for Maintenance PA not found at: {System.IO.Path.GetFileName(distributionSetupFile)}");

        allSetupData = CSVHelper.ReadDataFromCsvFile(distributionSetupFile);
        domainModel.SubModels.MaintenanceExtentPAForACandOgpa = SetupUtilities.GetDistributionSimulator("post_all_mtc_extent_ac_ogpa", allSetupData);
        domainModel.SubModels.MaintenanceExtentPAForCSandSlurry = SetupUtilities.GetDistributionSimulator("post_all_mtc_extent_cs_slurry", allSetupData);

        // Then potfill extent
        distributionSetupFile = System.IO.Path.Combine(workFolder, @"domain_model/maint_poth_extent_cohort_rule_plm_setup_for_cassandra.csv");
        if (!System.IO.File.Exists(distributionSetupFile)) throw new Exception($"Distribution setup file for Maintenance Potfill not found at: {System.IO.Path.GetFileName(distributionSetupFile)}");

        allSetupData = CSVHelper.ReadDataFromCsvFile(distributionSetupFile);
        domainModel.SubModels.MaintenanceExtentPotfillACandOgpa = SetupUtilities.GetDistributionSimulator("post_potfill_mtc_extent_ac_ogpa", allSetupData);
        domainModel.SubModels.MaintenanceExtentPotfillCSandSlurry = SetupUtilities.GetDistributionSimulator("post_potfill_mtc_extent_cs_slurry", allSetupData);

    }

    public static PieceWiseLinearModelGeneric GetPieceWiseLinearModel(string parameterName, jcDataSet allSetups)
    {
        Dictionary<string, object> row = allSetups.Row(parameterName);
        string pwlSetupString = row["plm_setup_code"].ToString();
        PieceWiseLinearModelGeneric model = new PieceWiseLinearModelGeneric(pwlSetupString, false); //Do not extrapolate.
        return model;
    }

    public static DistributionSimulator GetDistributionSimulator(string parameterName, jcDataSet allSetups)
    {
        jcDataSet setupDataForParameter = GetFilteredDataSet(parameterName, allSetups);
        DistributionSimulator simulator = new DistributionSimulator(parameterName, setupDataForParameter);
        return simulator;
    }


    public static void SetupProbabilityModels(DomainObjects.MonteCarloRoadModelV1 domainModel, string workFolder)
    {
        //Set up Logistic prediction model for potfill probability for AC OGPA
        string coefsFile = Path.Combine(workFolder, @"domain_model/logistic_potfill_ac_ogpa.csv");
        if (!File.Exists(coefsFile)) throw new Exception($"Coefficient file for Logistic model not found at: {Path.GetFileName(coefsFile)}");        
        jcDataSet d1 = CSVHelper.ReadDataFromCsvFile(coefsFile);
        Dictionary<string, double> coefs1 = GetLogisticModelCoefficients(d1);
        domainModel.SubModels.PotfillProbabilityModelAC = new LogisticModel(coefs1);

        //Set up Logistic prediction model for potfill probability for CS slurry
        coefsFile = Path.Combine(workFolder, @"domain_model/logistic_potfill_cs_slurry.csv");
        if (!File.Exists(coefsFile)) throw new Exception($"Coefficient file for Logistic model not found at: {Path.GetFileName(coefsFile)}");
        jcDataSet d2 = CSVHelper.ReadDataFromCsvFile(coefsFile);
        Dictionary<string, double> coefs2 = GetLogisticModelCoefficients(d2);
        domainModel.SubModels.PotfillProbabilityModelCS = new LogisticModel(coefs2);

        //Set up Logistic prediction model for maintenance PA probability for CS slurry
        coefsFile = Path.Combine(workFolder, @"domain_model/logistic_pa_maint_cs_slurry.csv");
        if (!File.Exists(coefsFile)) throw new Exception($"Coefficient file for Logistic model not found at: {Path.GetFileName(coefsFile)}");
        jcDataSet d3 = CSVHelper.ReadDataFromCsvFile(coefsFile);
        Dictionary<string, double> coefs3 = GetLogisticModelCoefficients(d3);
        domainModel.SubModels.MaintPaProbabilityModelCS = new LogisticModel(coefs3);

        //Set up Logistic prediction model for maintenance PA probability for AC OGPA
        coefsFile = Path.Combine(workFolder, @"domain_model/logistic_pa_maint_ac_ogpa.csv");
        if (!File.Exists(coefsFile)) throw new Exception($"Coefficient file for Logistic model not found at: {Path.GetFileName(coefsFile)}");
        jcDataSet d4 = CSVHelper.ReadDataFromCsvFile(coefsFile);
        Dictionary<string, double> coefs4 = GetLogisticModelCoefficients(d4);
        domainModel.SubModels.MaintPaProbabilityModelAC = new LogisticModel(coefs4);
    }

    public static void SetupResetModels(DomainObjects.MonteCarloRoadModelV1 domainModel, string workFolder)
    {
        //------------------------------------  Set up distribution simulators for RESETS ------------------------------------

        string distributionSetupFile = Path.Combine(workFolder, @"domain_model/reset_cohort_rule_plm_setup_for_cassandra.csv");
        if (!File.Exists(distributionSetupFile)) throw new Exception($"Setup file for Reset Distributions not found at: {Path.GetFileName(distributionSetupFile)}");
        jcDataSet d1 = CSVHelper.ReadDataFromCsvFile(distributionSetupFile);

        jcDataSet allSetupData = CSVHelper.ReadDataFromCsvFile(distributionSetupFile);
        
        // Rut Reset simulators
        domainModel.SubModels.RutResetSimulatorACRehab = SetupUtilities.GetDistributionSimulator("rut_reset_ac_rehab", allSetupData);
        domainModel.SubModels.RutResetSimulatorACResurf = SetupUtilities.GetDistributionSimulator("rut_reset_ac_resurf", allSetupData);
        domainModel.SubModels.RutResetSimulatorCSRehab = SetupUtilities.GetDistributionSimulator("rut_reset_cs_rehab", allSetupData);
        domainModel.SubModels.RutResetSimulatorCSResurf = SetupUtilities.GetDistributionSimulator("rut_reset_cs_resurf", allSetupData);

        // IRI Reset simulators
        domainModel.SubModels.IRIResetSimulatorACRehab = SetupUtilities.GetDistributionSimulator("iri_reset_ac_rehab", allSetupData);
        domainModel.SubModels.IRIResetSimulatorACResurf = SetupUtilities.GetDistributionSimulator("iri_reset_ac_resurf", allSetupData);
        domainModel.SubModels.IRIResetSimulatorCSRehab = SetupUtilities.GetDistributionSimulator("iri_reset_cs_rehab", allSetupData);
        domainModel.SubModels.IRIResetSimulatorCSResurf = SetupUtilities.GetDistributionSimulator("iri_reset_cs_resurf", allSetupData);

        // Texture Reset simulators
        domainModel.SubModels.TextureResetSimulatorACRehab = SetupUtilities.GetDistributionSimulator("text_reset_ac_rehab", allSetupData);
        domainModel.SubModels.TextureResetSimulatorACResurf = SetupUtilities.GetDistributionSimulator("text_reset_ac_resurf", allSetupData);
        domainModel.SubModels.TextureResetSimulatorCSRehab = SetupUtilities.GetDistributionSimulator("text_reset_cs_rehab", allSetupData);
        domainModel.SubModels.TextureResetSimulatorCSResurf = SetupUtilities.GetDistributionSimulator("text_reset_cs_resurf", allSetupData);
    }

    public static void SetupReductionDueToPaMaintenanceModels(DomainObjects.MonteCarloRoadModelV1 domainModel, string workFolder)
    {
        //-----------------  Set up distribution simulators for REDUCTION in Rut and IRI after PA Maintenance ------------------------------------

        string distributionSetupFile = Path.Combine(workFolder, @"domain_model/reduction_after_maint_cohort_rule_plm_setup_for_cassandra.csv");
        if (!File.Exists(distributionSetupFile)) throw new Exception($"Setup file for Reduction Due to PA Maintenance Distributions not found at: {Path.GetFileName(distributionSetupFile)}");
        jcDataSet d1 = CSVHelper.ReadDataFromCsvFile(distributionSetupFile);

        jcDataSet allSetupData = CSVHelper.ReadDataFromCsvFile(distributionSetupFile);

        // Rut Reduction after PA Maintenance simulator
        domainModel.SubModels.RutReductionAfterPaMaintenanceSimulator = SetupUtilities.GetDistributionSimulator("rut_reduc_after_maint", allSetupData);

        // IRI Reduction after PA Maintenance simulator
        domainModel.SubModels.IRIReductionAfterPaMaintenanceSimulator = SetupUtilities.GetDistributionSimulator("iri_reduc_after_maint", allSetupData);

    }



    private static jcDataSet GetFilteredDataSet(string parameterName, jcDataSet allSetups)
    {
        jcDataSet setupData = new jcDataSet();
        setupData.Columns = allSetups.Columns;
        for (int i = 0; i < allSetups.Count; i++)
        {
            Dictionary<string, object> row = allSetups.Row(i);
            string setupParameterName = row["parameter_key"].ToString();
            if (setupParameterName == parameterName)
            {
                setupData.AddRow(row);
            }
        }
        return setupData;
    }

    private static Dictionary<string, double> GetLogisticModelCoefficients(jcDataSet coefsData)
    {        
        Dictionary<string, double> coefs = new Dictionary<string, double>();
        for (int i = 0; i < coefsData.Count; i++)
        {
            Dictionary<string, object> row = coefsData.Row(i);
            string variableName = row["term"].ToString();
            double coefValue = Convert.ToDouble(row["estimate"]);
            coefs[variableName] = coefValue;
        }
        return coefs;
    }
}
