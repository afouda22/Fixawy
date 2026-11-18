using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Fixawyy.DTOs;
using Fixawyy.Models;
using Fixawyy.Services;

namespace Fixawyy.Controllers;

[Authorize(Roles = AppRoles.OperationsAuthorizedRoles)]
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
    public async Task<ActionResult<KpiSummaryDto>> GetKpis()
    {
        var kpis = await _analyticsService.GetKpiSummaryAsync();
        return Ok(kpis);
    }

    [HttpGet("by-department")]
    public async Task<ActionResult<List<DepartmentBreakdownDto>>> GetByDepartment()
    {
        var data = await _analyticsService.GetTicketsByDepartmentAsync();
        return Ok(data);
    }

    [HttpGet("by-status")]
    public async Task<ActionResult<List<StatusBreakdownDto>>> GetByStatus()
    {
        var data = await _analyticsService.GetTicketsByStatusAsync();
        return Ok(data);
    }

    [HttpGet("technician-rankings")]
    public async Task<ActionResult<List<TechnicianRankingDto>>> GetTechnicianRankings()
    {
        var rankings = await _analyticsService.GetTechnicianRankingsAsync();
        return Ok(rankings);
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv()
    {
        var csvBytes = await _analyticsService.GenerateCsvReportAsync();
        return File(csvBytes, "text/csv", $"Fixawyy_Operations_Report_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
    }

    [HttpGet("export/pdf")]
    public async Task<IActionResult> ExportPdf()
    {
        var pdfBytes = await _analyticsService.GeneratePdfReportAsync();
        return File(pdfBytes, "application/pdf", $"Fixawyy_Executive_Report_{DateTime.UtcNow:yyyyMMdd_HHmm}.pdf");
    }
}
