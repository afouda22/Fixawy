namespace Fixawyy.Models;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Technician = "Technician";
    public const string Customer = "Customer";
    public const string Employee = "Employee";
    public const string FacilityEmployee = "FacilityEmployee";

    public const string OperationsAuthorizedRoles = $"{Admin},{Manager},{Employee},{FacilityEmployee}";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Admin,
        Manager,
        Technician,
        Customer,
        Employee,
        FacilityEmployee
    };
}
