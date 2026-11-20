namespace Fixawyy.Models;

public enum TicketPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum TicketStatus
{
    Pending,
    Assigned,
    InProgress,
    Resolved,
    Closed
}

public enum ImpactScope
{
    SingleRoom = 1,
    MultipleRooms = 2,
    EntireFloor = 3,
    EntireBuilding = 4
}

public enum HazardLevel
{
    Cosmetic = 1,
    Minor = 2,
    Moderate = 3,
    Dangerous = 4
}

public enum LocationCriticality
{
    Cafeteria = 1,
    Office = 2,
    MainFacility = 3,
    ServerRoom = 4
}
