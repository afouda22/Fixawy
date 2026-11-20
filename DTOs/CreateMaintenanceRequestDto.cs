
using System.ComponentModel.DataAnnotations;
using Fixawyy.Models;

namespace Fixawyy.DTOs;

public class CreateMaintenanceRequestDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public int DepartmentId { get; set; }

    [Required]
    public int LocationId { get; set; }

    [Required]
    [StringLength(100)]
    public string RequiredSkill { get; set; } = string.Empty;

    [Required]
    public ImpactScope ImpactScope { get; set; }

    [Required]
    public HazardLevel HazardLevel { get; set; }

    [Required]
    public LocationCriticality LocationCriticality { get; set; }
}
