namespace Fixawyy.Models;

public class TechnicianSchedule
{
    public int Id { get; set; }

    public int TechnicianProfileId { get; set; }
    public virtual TechnicianProfile TechnicianProfile { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsWorkingDay { get; set; } = true;
}
