namespace Fixawyy.Model_Views;

public class UserItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Facility { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
}

public class UserManagementViewModel
{
    public List<UserItemViewModel> Users { get; set; } = new();
    public string? Search { get; set; }
    public string? RoleFilter { get; set; }
    public List<string> AvailableRoles { get; set; } = new();
    public List<string> AvailableFacilities { get; set; } = new();
}
