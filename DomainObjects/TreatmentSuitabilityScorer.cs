

namespace MonteCarloRoadModelV1.DomainObjects;

public static class TreatmentSuitabilityScorer
{

    public static double GetTSSForPreservationTreatment(RoadSegmentMC segment, MonteCarloRoadModelV1 domainModel, int iPeriod)
    {
        double pdi = segment.PavementDistressIndex;
        double tssScore1 = domainModel.SubModels.TSSForPreservationTreatment.GetValue(segment.SurfaceDistressIndexRank);   //Use RANK, not the SDI itself!!
        double tssScore = tssScore1 - 0.5*pdi;
        return tssScore;
    }

    public static double GetTSSForRehabilitation(RoadSegmentMC segment, MonteCarloRoadModelV1 domainModel, int iPeriod)
    {            
        double tssScore = domainModel.SubModels.TSSForRehabilitation.GetValue(segment.PavementDistressIndexRank);   //Use RANK, not the PDI itself!!        
        return tssScore;
    }

    public static double GetTSSForHoldingAction(RoadSegmentMC segment, MonteCarloRoadModelV1 domainModel, int iPeriod)
    {        
        // If Rut is above the allowed value for Preseal Repairs, then TSS is zero
        if (segment.RutMeanObserved > domainModel.Constants.TSSHoldingMaxRut) return 0.0;

        // If we get here, a holding action is valid. Calculate the relative suitability score based on the Pavement Distress Index (PDI)        
        double tssScore = domainModel.SubModels.TSSForHoldingAction.GetValue(segment.PavementDistressIndexRank);
        return tssScore;
    }

}
