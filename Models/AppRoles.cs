namespace Fixawyy.Models;

public static class AppRoles
{
    public const string Admin = "Admin";                     // Super Admin (Full platform management)
    public const string FacilityAdmin = "FacilityAdmin";     // Admin of Employees per Facility (أدمن موظفي المنشأة)
    public const string Employee = "Employee";               // Regular Facility Employee (Submits requests only)
    public const string Technician = "Technician";           // Technician (Completes work orders)
    public const string Customer = "Customer";               // Residential Customer

    // Only Facility Admins and Super Admins can access the Dispatcher Engine & Analytics
    public const string DispatcherAuthorizedRoles = $"{Admin},{FacilityAdmin}";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Admin,
        FacilityAdmin,
        Employee,
        Technician,
        Customer
    };
}
