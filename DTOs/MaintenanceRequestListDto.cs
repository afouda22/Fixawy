
namespace Fixawyy.DTOs;

public class MaintenanceRequestListDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string DepartmentName { get; set; } = string.Empty;

    public string LocationName { get; set; } = string.Empty;

    public string? Building { get; set; }

    public string? Floor { get; set; }

    public string Priority { get; set; } = string.Empty;

    public int PriorityScore { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
