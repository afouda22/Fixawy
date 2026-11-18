using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Fixawyy.DTOs;
using Fixawyy.Models;
using Fixawyy.Services;

namespace Fixawyy.Controllers;

[Authorize(Roles = AppRoles.DispatcherAuthorizedRoles)]
[ApiController]
[Route("api/analytics")]
public class AnalyticsApiController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsApiController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("kpis")]
    [HttpGet("kpi-summary")]
    public async Task<ActionResult<KpiSummaryDto>> GetKpis([FromQuery] string? facility = null)
    {
        var kpis = await _analyticsService.GetKpiSummaryAsync(facility);
        return Ok(kpis);
    }

    [HttpGet("by-department")]
    public async Task<ActionResult<List<DepartmentBreakdownDto>>> GetByDepartment([FromQuery] string? facility = null)
    {
        var data = await _analyticsService.GetTicketsByDepartmentAsync(facility);
        return Ok(data);
    }

    [HttpGet("by-status")]
    public async Task<ActionResult<List<StatusBreakdownDto>>> GetByStatus([FromQuery] string? facility = null)
    {
        var data = await _analyticsService.GetTicketsByStatusAsync(facility);
        return Ok(data);
    }

    [HttpGet("technician-rankings")]
    public async Task<ActionResult<List<TechnicianRankingDto>>> GetTechnicianRankings([FromQuery] string? facility = null)
    {
        var rankings = await _analyticsService.GetTechnicianRankingsAsync(facility);
        return Ok(rankings);
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv([FromQuery] string? facility = null)
    {
        var csvBytes = await _analyticsService.GenerateCsvReportAsync(facility);
        var filename = string.IsNullOrWhiteSpace(facility) || facility == "All"
            ? $"Fixawyy_Operations_Report_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv"
            : $"Fixawyy_Report_{facility.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv";

        return File(csvBytes, "text/csv", filename);
    }

    [HttpGet("export/pdf")]
    public async Task<IActionResult> ExportPdf([FromQuery] string? facility = null)
    {
        var pdfBytes = await _analyticsService.GeneratePdfReportAsync(facility);
        var filename = string.IsNullOrWhiteSpace(facility) || facility == "All"
            ? $"Fixawyy_Executive_Report_{DateTime.UtcNow:yyyyMMdd_HHmm}.pdf"
            : $"Fixawyy_Report_{facility.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd_HHmm}.pdf";

        return File(pdfBytes, "application/pdf", filename);
    }
}
