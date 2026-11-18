namespace Fixawyy.Models;

public class TechnicianSpecialty
{
    public int TechnicianProfileId { get; set; }
    public virtual TechnicianProfile TechnicianProfile { get; set; } = null!;

    public int SpecialtyId { get; set; }
    public virtual Specialty Specialty { get; set; } = null!;
}
