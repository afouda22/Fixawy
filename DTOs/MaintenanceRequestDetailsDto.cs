
namespace Fixawyy.DTOs;

public class MaintenanceRequestDetailsDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string DepartmentName { get; set; } = string.Empty;

    public string LocationName { get; set; } = string.Empty;

    public string? Building { get; set; }

    public string? Floor { get; set; }

    public string RequiredSkill { get; set; } = string.Empty;

    public string ImpactScope { get; set; } = string.Empty;

    public string HazardLevel { get; set; } = string.Empty;

    public string LocationCriticality { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public int PriorityScore { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }
}
