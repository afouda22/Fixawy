namespace Fixawyy.DTOs;

public class TicketSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string RequiredSkill { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class TechnicianMatchResultDto
{
    public int TechnicianId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public double Rating { get; set; }
    public int ActiveTicketsCount { get; set; }
    public double MatchScore { get; set; } // من 0 إلى 100
    public List<string> MatchReasons { get; set; } = new();
    public bool IsRecommended { get; set; }
}

public class AssignmentRequestDto
{
    public int TicketId { get; set; }
    public string? Notes { get; set; }
}

public class ManualAssignmentRequestDto
{
    public int TicketId { get; set; }
    public int TechnicianId { get; set; }
    public string? Notes { get; set; }
}

public class AssignmentResponseDto
{
    public int AssignmentId { get; set; }
    public int TicketId { get; set; }
    public string TicketTitle { get; set; } = string.Empty;
    public int TechnicianId { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public double MatchScore { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

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
