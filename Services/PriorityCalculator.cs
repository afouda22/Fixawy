
using Fixawyy.Models;

namespace Fixawyy.Services;

public class PriorityCalculator
{
    public int CalculateScore(
        ImpactScope impactScope,
        HazardLevel hazardLevel,
        LocationCriticality locationCriticality)
    {
        int impactScore = impactScope switch
        {
            ImpactScope.SingleRoom => 10,
            ImpactScope.MultipleRooms => 20,
            ImpactScope.EntireFloor => 35,
            ImpactScope.EntireBuilding => 50,
            _ => throw new ArgumentOutOfRangeException(nameof(impactScope))
        };

        int hazardScore = hazardLevel switch
        {
            HazardLevel.Cosmetic => 5,
            HazardLevel.Minor => 15,
            HazardLevel.Moderate => 25,
            HazardLevel.Dangerous => 35,
            _ => throw new ArgumentOutOfRangeException(nameof(hazardLevel))
        };

        int criticalityScore = locationCriticality switch
        {
            LocationCriticality.Cafeteria => 5,
            LocationCriticality.Office => 10,
            LocationCriticality.MainFacility => 15,
            LocationCriticality.ServerRoom => 20,
            _ => throw new ArgumentOutOfRangeException(nameof(locationCriticality))
        };

        return impactScore + hazardScore + criticalityScore;
    }

    public TicketPriority CalculatePriority(int score)
    {
        return score switch
        {
            >= 80 => TicketPriority.Critical,
            >= 60 => TicketPriority.High,
            >= 40 => TicketPriority.Medium,
            _ => TicketPriority.Low
        };
    }
}
