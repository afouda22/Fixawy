using Microsoft.AspNetCore.Components;

namespace SmartMaintenance.Dispatcher.Models;

public class Technician
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

   
    public string Specialization { get; set; } = string.Empty;

   
    public double Rating { get; set; } = 5.0;

    
    public bool IsAvailable { get; set; } = true;

   
    public int ActiveTicketsCount { get; set; } = 0;

  
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}