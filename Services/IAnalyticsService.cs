using Fixawyy.DTOs;

namespace Fixawyy.Services;

public interface IAnalyticsService
{
    // استخراج ملخص المؤشرات التشغيلية KPIs (مع فلترة اختيارية بحسب المنشأة)
    Task<KpiSummaryDto> GetKpiSummaryAsync(string? facility = null);

    // توزيع البلاغات حسب الأقسام / المنشآت
    Task<List<DepartmentBreakdownDto>> GetTicketsByDepartmentAsync(string? facility = null);

    // توزيع البلاغات حسب الحالات
    Task<List<StatusBreakdownDto>> GetTicketsByStatusAsync(string? facility = null);

    // تصنيف وترتيب الفنيين حسب الإنجاز والتقييم وسرعة الإغلاق
    Task<List<TechnicianRankingDto>> GetTechnicianRankingsAsync(string? facility = null);

    // تصدير البيانات إلى ملف CSV بحسب المنشأة
    Task<byte[]> GenerateCsvReportAsync(string? facility = null);

    // تصدير تقرير تنفيذي بصيغة PDF بحسب المنشأة
    Task<byte[]> GeneratePdfReportAsync(string? facility = null);
}
