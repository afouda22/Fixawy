namespace Fixawyy.Models;

public class TechnicianProfile
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser User { get; set; } = null!;

    public string? Bio { get; set; }
    public decimal HourlyRate { get; set; } = 150m;
    public int YearsOfExperience { get; set; } = 1;
    public bool IsAvailable { get; set; } = true;
    public double Rating { get; set; } = 0.0;
    public int ReviewCount { get; set; } = 0;

    public virtual ICollection<TechnicianSpecialty> TechnicianSpecialties { get; set; } = new List<TechnicianSpecialty>();
    public virtual ICollection<TechnicianSchedule> Schedules { get; set; } = new List<TechnicianSchedule>();
    public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}
