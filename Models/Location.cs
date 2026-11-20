
namespace Fixawyy.Models;

public class Location
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Building { get; set; }

    public string? Floor { get; set; }

    public virtual ICollection<MaintenanceTicket> MaintenanceTickets { get; set; }
        = new List<MaintenanceTicket>();
}
