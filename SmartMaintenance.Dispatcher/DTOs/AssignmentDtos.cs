namespace SmartMaintenance.Dispatcher.DTOs;

public class TechnicianMatchResultDto
{
    public int TechnicianId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public double Rating { get; set; }
    public int ActiveTicketsCount { get; set; }
    public double MatchScore { get; set; } // درجة التوافق من 0 إلى 100
    public bool IsRecommended { get; set; } // الفني صاحب أعلى تقييم
    public List<string> MatchReasons { get; set; } = new(); // أسباب الاختيار
}

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

public class AutoAssignRequestDto
{
    public int TicketId { get; set; }
    public string? Notes { get; set; }
}

public class ManualAssignRequestDto
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
