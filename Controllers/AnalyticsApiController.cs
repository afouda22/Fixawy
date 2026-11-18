using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    private readonly UserManager<ApplicationUser> _userManager;

    public AnalyticsApiController(
        IAnalyticsService analyticsService,
        UserManager<ApplicationUser> userManager)
    {
        _analyticsService = analyticsService;
        _userManager = userManager;
    }

    private async Task<string?> GetEffectiveFacilityAsync(string? requestedFacility)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return requestedFacility;

        bool isSuperAdmin = await _userManager.IsInRoleAsync(user, AppRoles.Admin);
        if (isSuperAdmin)
        {
            return requestedFacility;
        }

        // Facility Admin is strictly locked to their assigned facility
        return !string.IsNullOrWhiteSpace(user.Facility) ? user.Facility : requestedFacility;
    }

    [HttpGet("kpis")]
    [HttpGet("kpi-summary")]
    public async Task<ActionResult<KpiSummaryDto>> GetKpis([FromQuery] string? facility = null)
    {
        var effectiveFacility = await GetEffectiveFacilityAsync(facility);
        var kpis = await _analyticsService.GetKpiSummaryAsync(effectiveFacility);
        return Ok(kpis);
    }

    [HttpGet("by-department")]
    public async Task<ActionResult<List<DepartmentBreakdownDto>>> GetByDepartment([FromQuery] string? facility = null)
    {
        var effectiveFacility = await GetEffectiveFacilityAsync(facility);
        var data = await _analyticsService.GetTicketsByDepartmentAsync(effectiveFacility);
        return Ok(data);
    }

    [HttpGet("by-status")]
    public async Task<ActionResult<List<StatusBreakdownDto>>> GetByStatus([FromQuery] string? facility = null)
    {
        var effectiveFacility = await GetEffectiveFacilityAsync(facility);
        var data = await _analyticsService.GetTicketsByStatusAsync(effectiveFacility);
        return Ok(data);
    }

    [HttpGet("technician-rankings")]
    public async Task<ActionResult<List<TechnicianRankingDto>>> GetTechnicianRankings([FromQuery] string? facility = null)
    {
        var effectiveFacility = await GetEffectiveFacilityAsync(facility);
        var rankings = await _analyticsService.GetTechnicianRankingsAsync(effectiveFacility);
        return Ok(rankings);
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv([FromQuery] string? facility = null)
    {
        var effectiveFacility = await GetEffectiveFacilityAsync(facility);
        var csvBytes = await _analyticsService.GenerateCsvReportAsync(effectiveFacility);
        var filename = string.IsNullOrWhiteSpace(effectiveFacility) || effectiveFacility == "All"
            ? $"Fixawyy_Operations_Report_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv"
            : $"Fixawyy_Report_{effectiveFacility.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv";

        return File(csvBytes, "text/csv", filename);
    }

    [HttpGet("export/pdf")]
    public async Task<IActionResult> ExportPdf([FromQuery] string? facility = null)
    {
        var effectiveFacility = await GetEffectiveFacilityAsync(facility);
        var pdfBytes = await _analyticsService.GeneratePdfReportAsync(effectiveFacility);
        var filename = string.IsNullOrWhiteSpace(effectiveFacility) || effectiveFacility == "All"
            ? $"Fixawyy_Executive_Report_{DateTime.UtcNow:yyyyMMdd_HHmm}.pdf"
            : $"Fixawyy_Report_{effectiveFacility.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd_HHmm}.pdf";

        return File(pdfBytes, "application/pdf", filename);
    }
}
