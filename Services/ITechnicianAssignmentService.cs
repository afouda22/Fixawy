using Fixawyy.DTOs;

namespace Fixawyy.Services;

public interface ITechnicianAssignmentService
{
    // استرجاع قائمة التذاكر المعلقة بانتظار التوزيع (مع فلترة اختيارية بحسب المنشأة)
    Task<List<TicketSummaryDto>> GetPendingTicketsAsync(string? facility = null);

    // خوارزمية فحص واقتراح أفضل الفنيين لتذكرة معينة مع أسباب الاختيار ونقاط التوافق
    Task<List<TechnicianMatchResultDto>> GetMatchingTechniciansAsync(int ticketId);

    // التعيين التلقائي لأفضل فني ترشحه الخوارزمية
    Task<AssignmentResponseDto> AutoAssignTechnicianAsync(int ticketId, string? notes = null);

    // التعيين اليدوي بواسطة مدير المنشأة
    Task<AssignmentResponseDto> ManualAssignTechnicianAsync(int ticketId, int technicianId, string? notes = null);

    // استرجاع قائمة بجميع التعيينات النشطة الحالية (مع فلترة اختيارية بحسب المنشأة)
    Task<List<AssignmentResponseDto>> GetActiveAssignmentsAsync(string? facility = null);

    // إنهاء المهمة وتحديد البلاغ كمحلول (Resolved) وبث حدث StatusChanged
    Task<bool> ResolveAssignmentAsync(int assignmentId);

    // استرجاع قائمة المنشآت والمرافق المسجلة بالنظام
    Task<List<string>> GetFacilitiesAsync();
}
