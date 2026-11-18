using Fixawyy.DTOs;

namespace Fixawyy.Services;

public interface ITechnicianAssignmentService
{
    // استرجاع قائمة التذاكر المعلقة بانتظار التوزيع
    Task<List<TicketSummaryDto>> GetPendingTicketsAsync();

    // خوارزمية فحص واقتراح أفضل الفنيين لتذكرة معينة مع أسباب الاختيار ونقاط التوافق
    Task<List<TechnicianMatchResultDto>> GetMatchingTechniciansAsync(int ticketId);

    // التعيين التلقائي لأفضل فني ترشحه الخوارزمية
    Task<AssignmentResponseDto> AutoAssignTechnicianAsync(int ticketId, string? notes = null);

    // التعيين اليدوي بواسطة المدير
    Task<AssignmentResponseDto> ManualAssignTechnicianAsync(int ticketId, int technicianId, string? notes = null);

    // استرجاع قائمة بجميع التعيينات النشطة الحالية
    Task<List<AssignmentResponseDto>> GetActiveAssignmentsAsync();

    // إنهاء المهمة وتحديد البلاغ كمحلول (Resolved) وبث حدث StatusChanged
    Task<bool> ResolveAssignmentAsync(int assignmentId);
}
