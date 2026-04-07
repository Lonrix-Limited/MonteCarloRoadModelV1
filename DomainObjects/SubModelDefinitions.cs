using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.EMMA;
using JCass_Core.Statistics;
using JCass_Functions.Engineering;
using JCass_ModelCore.MonteCarlo;

namespace MonteCarloRoadModelV1.DomainObjects;


/// <summary>
/// Wrappper class to hold the various sub-models and simulators used in the Monte Carlo Road Deterioration Model. This includes:
/// <list type="bullet">
/// <item><description>Simulators for the increments of the three main deterioration indicators (Rut Depth, IRI, Texture)</description></item>
/// <item><description>Piecewise Linear Models to calculate the Standard Deviation of the residuals for the increments</description></item>
/// <item><description>Probability models for pothole filling and maintenance activities</description></item>
/// <item><description>Simulators for the extent of maintenance activities when they are triggered</description></item>
/// </list>
/// </summary>
public class SubModelDefinitions

{
    private NormalGenerator _normalGenerator;


    #region Increment Simulators and Residual SD Functions

    /// <summary>
    /// Simulator for Rut Depth Increment. 
    /// </summary>
    public DistributionSimulator RutIncrementSimulator { get; set; }

    /// <summary>
    /// Alternative model for Rut increment prediction, using a Linear Regression model instead of a distribution simulator. 
    /// </summary>
    //public LinearRegressionModel RutIncrementModel { get; set; }

    /// <summary>
    /// Simulator for IRI Increment.
    /// </summary>
    public DistributionSimulator IRIIncrementSimulator { get; set; }

    /// <summary>
    /// Alternative model for IRI increment prediction, using a Linear Regression model instead of a distribution simulator.
    /// </summary>
    //public LinearRegressionModel IRIIncrementModel { get; set; }

    /// <summary>
    /// Simulator for Texture Increment.
    /// </summary>
    public DistributionSimulator TextureIncrementSimulator { get; set; }


    /// <summary>
    /// Piecewise Linear function to calculate the Standard Deviation of the Rut Increment residual as a function of the current rut depth.
    /// Function generally gives higher SD values for higher rut depths, to reflect the higher variability in rut increment as rut depth increases. 
    /// </summary>
    public PieceWiseLinearModelGeneric RutInrementResidualSDFunction { get; set; }


    /// <summary>
    /// Piecewise Linear function to calculate the Standard Deviation of the IRI Increment residual as a function of the current IRI value.
    /// </summary>
    public PieceWiseLinearModelGeneric IRIInrementResidualSDFunction { get; set; }


    /// <summary>
    /// Piecewise Linear function to calculate the Standard Deviation of the Texture Increment residual as a function of the current texture value.
    /// </summary>
    public PieceWiseLinearModelGeneric TextureInrementResidualSDFunction { get; set; }

    #endregion

    #region Maintenance Probability Models and Extent Simulators

    /// <summary>
    /// Model to predict the probability of Pothole filling occurring in the next period for AC surfacings. Coefficients are 
    /// read at startup from CSV file exported with R script. This CSV should have columns 'term' and 'estimate' where 
    /// 'term' is the name of the coefficient (e.g. '(Intercept)', 'IRI', 'Rut', etc) and 'estimate' is the value of the coefficient. 
    /// </summary>
    public JCass_Core.Statistics.LogisticModel PotfillProbabilityModelAC { get; set; }


    /// <summary>
    /// Model to predict the probability of Pothole filling occurring in the next period for CS surfacings. Coefficients are 
    /// read at startup from CSV file exported with R script. This CSV should have columns 'term' and 'estimate' where 
    /// 'term' is the name of the coefficient (e.g. '(Intercept)', 'IRI', 'Rut', etc) and 'estimate' is the value of the coefficient. 
    /// </summary>
    public JCass_Core.Statistics.LogisticModel PotfillProbabilityModelCS { get; set; }


    /// <summary>
    /// Model to predict the probability of Pavement (PA) Maintenance occurring in the next period for AC surfacings. Coefficients are 
    /// read at startup from CSV file exported with R script. This CSV should have columns 'term' and 'estimate' where 
    /// 'term' is the name of the coefficient (e.g. '(Intercept)', 'IRI', 'Rut', etc) and 'estimate' is the value of the coefficient. 
    /// </summary>
    public JCass_Core.Statistics.LogisticModel MaintPaProbabilityModelAC { get; set; }


    /// <summary>
    /// Model to predict the probability of Pavement (PA) Maintenance occurring in the next period for CS surfacings. Coefficients are 
    /// read at startup from CSV file exported with R script. This CSV should have columns 'term' and 'estimate' where 
    /// 'term' is the name of the coefficient (e.g. '(Intercept)', 'IRI', 'Rut', etc) and 'estimate' is the value of the coefficient. 
    /// </summary>
    public JCass_Core.Statistics.LogisticModel MaintPaProbabilityModelCS { get; set; }


    /// <summary>
    /// Simulator for assigning a random Extent of PA maintenance (excluding Pothole filling) when it is triggered, specifically
    /// for Asphalt and OGPA
    /// </summary>
    public DistributionSimulator MaintenanceExtentPAForACandOgpa { get; set; }


    /// <summary>
    /// Simulator for assigning a random Extent of PA maintenance (excluding Pothole filling) when it is triggered, specifically
    /// for Chipseals and Slurry Seals
    /// </summary>
    public DistributionSimulator MaintenanceExtentPAForCSandSlurry { get; set; }


    /// <summary>
    /// Simulator for assigning a random Extent of Pothole Filling maintenance when it is triggered, specifically
    /// for Asphalt and OGPA. 
    /// </summary>
    public DistributionSimulator MaintenanceExtentPotfillACandOgpa { get; set; }

    /// <summary>
    /// Simulator for assigning a random Extent of Pothole Filling maintenance when it is triggered, specifically
    /// for Chipseals and Slurries. 
    /// </summary>
    public DistributionSimulator MaintenanceExtentPotfillCSandSlurry { get; set; }

    #endregion

    #region Reset Simulators

    /// <summary>
    /// Simulator for Rut Depth Reset (value after treatment) for Chipseal and Slurry Seal Resurfacings
    /// </summary>
    public DistributionSimulator RutResetSimulatorCSResurf { get; set; }

    /// <summary>
    /// Simulator for Rut Depth Reset (value after treatment) for Chipseal and Slurry Seal Rehabilitations
    /// </summary>
    public DistributionSimulator RutResetSimulatorCSRehab { get; set; }


    /// <summary>
    /// Simulator for Rut Depth Reset (value after treatment) for Asphalt and OGPA Resurfacings
    /// </summary>
    public DistributionSimulator RutResetSimulatorACResurf { get; set; }

    /// <summary>
    /// Simulator for Rut Depth Reset (value after treatment) for Asphalt and OGPA Rehabilitations
    /// </summary>
    public DistributionSimulator RutResetSimulatorACRehab { get; set; }

    /// <summary>
    /// Simulator for IRI Reset (value after treatment) for Chipseal and Slurry Seal Resurfacings
    /// </summary>
    public DistributionSimulator IRIResetSimulatorCSResurf { get; set; }

    /// <summary>
    /// Simulator for IRI Reset (value after treatment) for Chipseal and Slurry Seal Rehabilitations
    /// </summary>
    public DistributionSimulator IRIResetSimulatorCSRehab { get; set; }

    /// <summary>
    /// Simulator for IRI Reset (value after treatment) for Asphalt and OGPA Resurfacings
    /// </summary>
    public DistributionSimulator IRIResetSimulatorACResurf { get; set; }

    /// <summary>
    /// Simulator for IRI Reset (value after treatment) for Asphalt and OGPA Rehabilitations
    /// </summary>
    public DistributionSimulator IRIResetSimulatorACRehab { get; set; }


    /// <summary>
    /// Simulator for Texture Depth Reset (value after treatment) for Chipseal and Slurry Seal Resurfacings
    /// </summary>
    public DistributionSimulator TextureResetSimulatorCSResurf { get; set; }

    /// <summary>
    /// Simulator for Texture Depth Reset (value after treatment) for Chipseal and Slurry Seal Rehabilitations
    /// </summary>
    public DistributionSimulator TextureResetSimulatorCSRehab { get; set; }

    /// <summary>
    /// Simulator for Texture Depth Reset (value after treatment) for Asphalt and OGPA Resurfacings
    /// </summary>
    public DistributionSimulator TextureResetSimulatorACResurf { get; set; }
    /// <summary>
    /// Simulator for Texture Depth Reset (value after treatment) for Asphalt and OGPA Rehabilitations
    /// </summary>
    public DistributionSimulator TextureResetSimulatorACRehab { get; set; }

    #endregion

    #region Rut and IRI Reduction (not reset!) after PA Maintenance, based on Extent


    public LinearRegressionModel RutReductionAfterPaMaintenanceModel { get; set; }

    public LinearRegressionModel IRIReductionAfterPaMaintenanceModel { get; set; }


    /// <summary>
    /// Simulator for the reduction in Rut Depth (not reset value, but reduction in the current value) after PA maintenance 
    /// (excluding pothole filling), based on the extent of maintenance. 
    /// </summary>
    //public DistributionSimulator RutReductionAfterPaMaintenanceSimulator { get; set; }

    /// <summary>
    /// Simulator for the reduction in IRI (not reset value, but reduction in the current value) after PA maintenance 
    /// (excluding pothole filling), based on the extent of maintenance. 
    /// </summary>
    public DistributionSimulator IRIReductionAfterPaMaintenanceSimulator { get; set; }

    #endregion

    #region Other Sub-Models

    /// <summary>
    /// Generator for random numbers from the standard normal distribution, used for simulating the residuals for the increments. 
    /// The seed is set at the start of the model run based on the seed in the configuration.
    /// </summary>
    public NormalGenerator NormalGenerator { get { return _normalGenerator; } }

    #endregion

    public SubModelDefinitions(int randomSeed)
    {
        _normalGenerator = new NormalGenerator(randomSeed);
    }

}
