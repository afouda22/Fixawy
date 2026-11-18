using SmartMaintenance.Dispatcher.DTOs;

namespace SmartMaintenance.Dispatcher.Services;

public interface IAnalyticsService
{
    Task<KpiSummaryDto> GetKpiSummaryAsync();
    Task<List<DepartmentBreakdownDto>> GetTicketsByDepartmentAsync();
    Task<List<StatusBreakdownDto>> GetTicketsByStatusAsync();
    Task<List<TechnicianRankingDto>> GetTechnicianRankingsAsync();
    Task<byte[]> GenerateCsvReportAsync();
    Task<byte[]> GeneratePdfReportAsync();
}
