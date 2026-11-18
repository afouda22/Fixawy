using Microsoft.AspNetCore.Mvc;
using SmartMaintenance.Dispatcher.Services;

namespace SmartMaintenance.Dispatcher.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("kpi-summary")]
    public async Task<IActionResult> GetKpiSummary()
    {
        var summary = await _analyticsService.GetKpiSummaryAsync();
        return Ok(summary);
    }

    [HttpGet("by-department")]
    public async Task<IActionResult> GetTicketsByDepartment()
    {
        var data = await _analyticsService.GetTicketsByDepartmentAsync();
        return Ok(data);
    }

    [HttpGet("by-status")]
    public async Task<IActionResult> GetTicketsByStatus()
    {
        var data = await _analyticsService.GetTicketsByStatusAsync();
        return Ok(data);
    }

    [HttpGet("technician-rankings")]
    public async Task<IActionResult> GetTechnicianRankings()
    {
        var rankings = await _analyticsService.GetTechnicianRankingsAsync();
        return Ok(rankings);
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv()
    {
        var bytes = await _analyticsService.GenerateCsvReportAsync();
        return File(bytes, "text/csv", $"Maintenance_Tickets_Report_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
    }

    [HttpGet("export/pdf")]
    public async Task<IActionResult> ExportPdf()
    {
        var bytes = await _analyticsService.GeneratePdfReportAsync();
        return File(bytes, "application/pdf", $"SmartMaintenance_Executive_Report_{DateTime.UtcNow:yyyyMMdd_HHmm}.pdf");
    }
}
