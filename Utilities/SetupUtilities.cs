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

        domainModel.RutInrementResidualSDFunction = GetPieceWiseLinearModel("rut_inc_resid", allSetupData);
        domainModel.IRIInrementResidualSDFunction = GetPieceWiseLinearModel("iri_inc_resid", allSetupData);
        domainModel.TextureInrementResidualSDFunction = GetPieceWiseLinearModel("text_inc_resid", allSetupData);
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
        string distributionSetupFile = System.IO.Path.Combine(workFolder, @"domain_model/cohort_rule_plm_setup_for_cassandra.csv");
        if (!System.IO.File.Exists(distributionSetupFile))
        {
            throw new Exception($"Distribution setup file not found at: {distributionSetupFile}");
        }

        jcDataSet allSetupData = CSVHelper.ReadDataFromCsvFile(distributionSetupFile);

        domainModel.RutIncrementSimulator = SetupUtilities.GetDistributionSimulator("rut_inc", allSetupData);
        domainModel.IRIIncrementSimulator = SetupUtilities.GetDistributionSimulator("iri_inc", allSetupData);
        domainModel.TextureIncrementSimulator = SetupUtilities.GetDistributionSimulator("text_not_ac_inc", allSetupData);
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
        string coefsFile = Path.Combine(workFolder, @"domain_model/logistic_potfill_ac_ogpa.csv");
        if (!File.Exists(coefsFile)) throw new Exception($"Coefficient file for Logistic model not found at: {Path.GetFileName(coefsFile)}");        
        jcDataSet d1 = CSVHelper.ReadDataFromCsvFile(coefsFile);
        Dictionary<string, double> coefs1 = GetLogisticModelCoefficients(d1);
        domainModel.PotfillProbabilityModelAC = new LogisticModel(coefs1);

        coefsFile = Path.Combine(workFolder, @"domain_model/logistic_potfill_cs_slurry.csv");
        if (!File.Exists(coefsFile)) throw new Exception($"Coefficient file for Logistic model not found at: {Path.GetFileName(coefsFile)}");
        jcDataSet d2 = CSVHelper.ReadDataFromCsvFile(coefsFile);
        Dictionary<string, double> coefs2 = GetLogisticModelCoefficients(d2);
        domainModel.PotfillProbabilityModelCS = new LogisticModel(coefs2);
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
