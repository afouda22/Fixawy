using System.ComponentModel.DataAnnotations;
using Fixawyy.DTOs;
using Fixawyy.Models;
using Microsoft.AspNetCore.Http;

namespace Fixawyy.Model_Views;

public class CreateMaintenanceRequestViewModel
{
    [Required, StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(2000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Required, Range(1, int.MaxValue)]
    [Display(Name = "Department")]
    public int DepartmentId { get; set; }

    [Required, Range(1, int.MaxValue)]
    [Display(Name = "Location")]
    public int LocationId { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    [Display(Name = "Required skill")]
    public string RequiredSkill { get; set; } = string.Empty;

    [EnumDataType(typeof(ImpactScope))]
    [Display(Name = "Impact scope")]
    public ImpactScope ImpactScope { get; set; }

    [EnumDataType(typeof(HazardLevel))]
    [Display(Name = "Hazard level")]
    public HazardLevel HazardLevel { get; set; }

    [EnumDataType(typeof(LocationCriticality))]
    [Display(Name = "Location criticality")]
    public LocationCriticality LocationCriticality { get; set; }

    [Display(Name = "Attachments")]
    public List<IFormFile> Attachments { get; set; } = new();

    public IReadOnlyList<Department> Departments { get; set; } = Array.Empty<Department>();
    public IReadOnlyList<Location> Locations { get; set; } = Array.Empty<Location>();
}

public class MaintenanceRequestFilterViewModel
{
    public TicketStatus? Status { get; set; }
    public TicketPriority? Priority { get; set; }
    public int? DepartmentId { get; set; }
    public int? LocationId { get; set; }
    [DataType(DataType.Date)]
    public DateTime? FromDate { get; set; }
    [DataType(DataType.Date)]
    public DateTime? ToDate { get; set; }
    public string Sort { get; set; } = "newest";
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
    public int Page { get; set; } = 1;
    public IReadOnlyList<Department> Departments { get; set; } = Array.Empty<Department>();
    public IReadOnlyList<Location> Locations { get; set; } = Array.Empty<Location>();
}

public class MaintenanceRequestListPageViewModel
{
    public IReadOnlyList<MaintenanceRequestListDto> Requests { get; set; } = Array.Empty<MaintenanceRequestListDto>();
    public MaintenanceRequestFilterViewModel Filter { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages => Filter.PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)Filter.PageSize);
}

public class MaintenanceRequestDetailsPageViewModel
{
    public MaintenanceRequestDetailsDto Request { get; set; } = new();
    public IReadOnlyList<TicketAttachment> Attachments { get; set; } = Array.Empty<TicketAttachment>();
    public bool CanManageAttachments { get; set; }
}

public class DepartmentFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    [Display(Name = "Department name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}

public class DepartmentDetailsViewModel
{
    public Department Department { get; set; } = new();
    public IReadOnlyList<MaintenanceRequestListDto> Requests { get; set; } = Array.Empty<MaintenanceRequestListDto>();
}

public class LocationFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(150, MinimumLength = 2)]
    [Display(Name = "Location name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(150)]
    public string? Building { get; set; }

    [StringLength(50)]
    public string? Floor { get; set; }
}

public class LocationDetailsViewModel
{
    public Location Location { get; set; } = new();
    public IReadOnlyList<MaintenanceRequestListDto> Requests { get; set; } = Array.Empty<MaintenanceRequestListDto>();
}

public class B2BDashboardViewModel
{
    public int TotalRequests { get; set; }
    public int PendingRequests { get; set; }
    public int AssignedRequests { get; set; }
    public int InProgressRequests { get; set; }
    public int ResolvedRequests { get; set; }
    public int HighPriorityRequests { get; set; }
    public int CriticalPriorityRequests { get; set; }
    public IReadOnlyList<MaintenanceRequestListDto> RecentRequests { get; set; } = Array.Empty<MaintenanceRequestListDto>();
}
