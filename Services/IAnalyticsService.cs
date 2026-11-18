using Fixawyy.DTOs;

namespace Fixawyy.Services;

public interface IAnalyticsService
{
    // استخراج ملخص المؤشرات التشغيلية KPIs
    Task<KpiSummaryDto> GetKpiSummaryAsync();

    // توزيع البلاغات حسب الأقسام
    Task<List<DepartmentBreakdownDto>> GetTicketsByDepartmentAsync();

    // توزيع البلاغات حسب الحالات
    Task<List<StatusBreakdownDto>> GetTicketsByStatusAsync();

    // تصنيف وترتيب الفنيين حسب الإنجاز والتقييم وسرعة الإغلاق
    Task<List<TechnicianRankingDto>> GetTechnicianRankingsAsync();

    // تصدير البيانات إلى ملف CSV
    Task<byte[]> GenerateCsvReportAsync();

    // تصدير تقرير تنفيذي بصيغة PDF
    Task<byte[]> GeneratePdfReportAsync();
}
