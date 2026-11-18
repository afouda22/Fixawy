namespace Fixawyy.Models;

public class Specialty
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }

    public virtual ICollection<TechnicianSpecialty> TechnicianSpecialties { get; set; } = new List<TechnicianSpecialty>();
}
