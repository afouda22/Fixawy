namespace SmartMaintenance.Dispatcher.DTOs;

public class KpiSummaryDto
{
    public int TotalTickets { get; set; }
    public int PendingTickets { get; set; }
    public int InProgressTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public double AverageResolutionHours { get; set; }
    public int TotalTechnicians { get; set; }
}

public class DepartmentBreakdownDto
{
    public string Department { get; set; } = string.Empty;
    public int TicketCount { get; set; }
}

public class StatusBreakdownDto
{
    public string Status { get; set; } = string.Empty;
    public int TicketCount { get; set; }
}

public class TechnicianRankingDto
{
    public int TechnicianId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public double Rating { get; set; }
    public int CompletedTickets { get; set; }
    public int ActiveTickets { get; set; }
    public double AvgResolutionHours { get; set; }
}
