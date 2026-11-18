using System.ComponentModel.DataAnnotations;
using Fixawyy.Models;

namespace Fixawyy.Model_Views;

public class TechnicianProfileViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    [Display(Name = "About / Professional Bio")]
    public string? Bio { get; set; }

    [Required]
    [Range(0, 10000, ErrorMessage = "Hourly rate must be between 0 and 10000 EGP")]
    [Display(Name = "Hourly Rate (EGP)")]
    public decimal HourlyRate { get; set; } = 150m;

    [Required]
    [Range(0, 50, ErrorMessage = "Years of experience must be between 0 and 50")]
    [Display(Name = "Years of Experience")]
    public int YearsOfExperience { get; set; } = 1;

    [Display(Name = "Available for Jobs")]
    public bool IsAvailable { get; set; } = true;

    public List<int> SelectedSpecialtyIds { get; set; } = new();
    public List<Specialty> AllSpecialties { get; set; } = new();
}

public class ScheduleItemViewModel
{
    public int Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public string DayName => DayOfWeek.ToString();
    public string StartTime { get; set; } = "09:00";
    public string EndTime { get; set; } = "17:00";
    public bool IsWorkingDay { get; set; } = true;
}

public class TechnicianScheduleViewModel
{
    public List<ScheduleItemViewModel> Schedules { get; set; } = new();
}

public class TechnicianDirectoryViewModel
{
    public List<TechnicianCardViewModel> Technicians { get; set; } = new();
    public List<Specialty> Specialties { get; set; } = new();
    public int? SelectedSpecialtyId { get; set; }
    public string? Search { get; set; }
}

public class TechnicianCardViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Bio { get; set; }
    public decimal HourlyRate { get; set; }
    public int YearsOfExperience { get; set; }
    public bool IsAvailable { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public List<string> Specialties { get; set; } = new();
}
