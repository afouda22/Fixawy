using System.Text;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Fixawyy.Context;
using Fixawyy.DTOs;
using Fixawyy.Models;

namespace Fixawyy.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _context;

    public AnalyticsService(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<MaintenanceTicket> FilterTickets(string? facility)
    {
        var query = _context.MaintenanceTickets.AsQueryable();
        if (!string.IsNullOrWhiteSpace(facility) && !facility.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            var fac = facility.Trim().ToLower();
            query = query.Where(t => t.Department.ToLower().Contains(fac));
        }
        return query;
    }

    public async Task<KpiSummaryDto> GetKpiSummaryAsync(string? facility = null)
    {
        var query = FilterTickets(facility);

        var totalTickets = await query.CountAsync();
        var pendingTickets = await query.CountAsync(t => t.Status == TicketStatus.Pending);
        var inProgressTickets = await query.CountAsync(t => t.Status == TicketStatus.InProgress || t.Status == TicketStatus.Assigned);
        var resolvedTickets = await query.CountAsync(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed);
        var totalTechnicians = await _context.TechnicianProfiles.CountAsync();

        // حساب متوسط وقت الحل للتذاكر المنجزة بالساعات
        var resolvedList = await query
            .Where(t => t.ResolvedAt != null)
            .Select(t => new { t.CreatedAt, ResolvedAt = t.ResolvedAt!.Value })
            .ToListAsync();

        double avgHours = 0;
        if (resolvedList.Any())
        {
            avgHours = Math.Round(resolvedList.Average(t => (t.ResolvedAt - t.CreatedAt).TotalHours), 1);
        }

        return new KpiSummaryDto
        {
            TotalTickets = totalTickets,
            PendingTickets = pendingTickets,
            InProgressTickets = inProgressTickets,
            ResolvedTickets = resolvedTickets,
            AverageResolutionHours = avgHours,
            TotalTechnicians = totalTechnicians
        };
    }

    public async Task<List<DepartmentBreakdownDto>> GetTicketsByDepartmentAsync(string? facility = null)
    {
        var query = FilterTickets(facility);

        return await query
            .GroupBy(t => t.Department)
            .Select(g => new DepartmentBreakdownDto
            {
                Department = string.IsNullOrEmpty(g.Key) ? "General" : g.Key,
                TicketCount = g.Count()
            })
            .OrderByDescending(d => d.TicketCount)
            .ToListAsync();
    }

    public async Task<List<StatusBreakdownDto>> GetTicketsByStatusAsync(string? facility = null)
    {
        var query = FilterTickets(facility);

        return await query
            .GroupBy(t => t.Status)
            .Select(g => new StatusBreakdownDto
            {
                Status = g.Key.ToString(),
                TicketCount = g.Count()
            })
            .ToListAsync();
    }

    public async Task<List<TechnicianRankingDto>> GetTechnicianRankingsAsync(string? facility = null)
    {
        var technicians = await _context.TechnicianProfiles
            .Include(tp => tp.User)
            .Include(tp => tp.TechnicianSpecialties)
                .ThenInclude(ts => ts.Specialty)
            .Include(tp => tp.Assignments)
                .ThenInclude(a => a.Ticket)
            .ToListAsync();

        var result = new List<TechnicianRankingDto>();
        var fac = !string.IsNullOrWhiteSpace(facility) && !facility.Equals("All", StringComparison.OrdinalIgnoreCase)
            ? facility.Trim().ToLower()
            : null;

        foreach (var tech in technicians)
        {
            var assignments = tech.Assignments.AsEnumerable();
            if (fac != null)
            {
                assignments = assignments.Where(a => a.Ticket != null && a.Ticket.Department.ToLower().Contains(fac));
            }

            var assignmentList = assignments.ToList();
            var completedAssignments = assignmentList
                .Where(a => a.Status == "Completed" && a.Ticket != null && a.Ticket.ResolvedAt != null)
                .ToList();

            double avgHours = 0;
            if (completedAssignments.Any())
            {
                avgHours = Math.Round(completedAssignments.Average(a => (a.Ticket!.ResolvedAt!.Value - a.AssignedAt).TotalHours), 1);
            }

            var specNames = string.Join(", ", tech.TechnicianSpecialties
                .Where(ts => ts.Specialty != null)
                .Select(ts => ts.Specialty!.Name));

            if (string.IsNullOrEmpty(specNames)) specNames = "General";

            result.Add(new TechnicianRankingDto
            {
                TechnicianId = tech.Id,
                FullName = tech.User?.FullName ?? $"Technician #{tech.Id}",
                Specialization = specNames,
                Rating = tech.Rating,
                CompletedTickets = completedAssignments.Count,
                ActiveTickets = assignmentList.Count(a => a.Status == "Active"),
                AvgResolutionHours = avgHours
            });
        }

        return result
            .OrderByDescending(r => r.Rating)
            .ThenByDescending(r => r.CompletedTickets)
            .ToList();
    }

    public async Task<byte[]> GenerateCsvReportAsync(string? facility = null)
    {
        var query = FilterTickets(facility);

        var tickets = await query
            .Include(t => t.Assignments)
                .ThenInclude(a => a.Technician)
                    .ThenInclude(tp => tp!.User)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("TicketId,Title,Facility_Department,RequiredSkill,Priority,Status,CreatedAt,ResolvedAt,DurationHours,AssignedTechnician");

        foreach (var t in tickets)
        {
            var assigned = t.Assignments.OrderByDescending(a => a.AssignedAt).FirstOrDefault();
            var techName = assigned?.Technician?.User?.FullName ?? "Unassigned";
            double duration = t.ResolvedAt.HasValue ? Math.Round((t.ResolvedAt.Value - t.CreatedAt).TotalHours, 1) : 0;
            var resolvedStr = t.ResolvedAt.HasValue ? t.ResolvedAt.Value.ToString("yyyy-MM-dd HH:mm") : "N/A";

            sb.AppendLine($"{t.Id},\"{t.Title.Replace("\"", "\"\"")}\",\"{t.Department}\",\"{t.RequiredSkill}\",{t.Priority},{t.Status},{t.CreatedAt:yyyy-MM-dd HH:mm},{resolvedStr},{duration},\"{techName}\"");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> GeneratePdfReportAsync(string? facility = null)
    {
        var kpi = await GetKpiSummaryAsync(facility);
        var deptData = await GetTicketsByDepartmentAsync(facility);
        var techRankings = await GetTechnicianRankingsAsync(facility);

        var facilityLabel = !string.IsNullOrWhiteSpace(facility) && !facility.Equals("All", StringComparison.OrdinalIgnoreCase)
            ? $"Facility: {facility}"
            : "All Facilities & Organizations";

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Fixawyy Maintenance Platform").FontSize(20).Bold().FontColor(Colors.Blue.Darken3);
                            c.Item().Text($"Executive Operations & Analytics Report — {facilityLabel}").FontSize(11).FontColor(Colors.Grey.Medium);
                        });

                        row.ConstantItem(150).Column(c =>
                        {
                            c.Item().AlignRight().Text($"Date: {DateTime.UtcNow:yyyy-MM-dd}").FontSize(9);
                            c.Item().AlignRight().Text("Facility Admin Confidential").FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                    });

                    col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().Column(col =>
                {
                    // KPI cards
                    col.Item().PaddingBottom(15).Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                        {
                            c.Item().Text("TOTAL TICKETS").FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                            c.Item().Text($"{kpi.TotalTickets}").FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                        });
                        row.Spacing(8);
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                        {
                            c.Item().Text("RESOLVED").FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                            c.Item().Text($"{kpi.ResolvedTickets}").FontSize(16).Bold().FontColor(Colors.Green.Darken2);
                        });
                        row.Spacing(8);
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                        {
                            c.Item().Text("PENDING").FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                            c.Item().Text($"{kpi.PendingTickets}").FontSize(16).Bold().FontColor(Colors.Amber.Darken2);
                        });
                        row.Spacing(8);
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                        {
                            c.Item().Text("AVG RESOLUTION").FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                            c.Item().Text($"{kpi.AverageResolutionHours} hrs").FontSize(16).Bold().FontColor(Colors.Purple.Darken2);
                        });
                    });

                    // Department breakdown table
                    col.Item().Text("Incidents Distribution by Location / Facility").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().PaddingTop(4).PaddingBottom(15).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(1);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Facility / Location").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Ticket Count").Bold();
                        });

                        foreach (var d in deptData)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(d.Department);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{d.TicketCount}");
                        }
                    });

                    // Technician Ranking table
                    col.Item().Text("Technician Performance Ranking").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().PaddingTop(4).PaddingBottom(15).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Technician").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Specialization").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Rating").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Completed").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Avg Speed").Bold();
                        });

                        foreach (var tech in techRankings)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(tech.FullName);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(tech.Specialization);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text($"★ {tech.Rating:F1}");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text($"{tech.CompletedTickets}");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text($"{tech.AvgResolutionHours} hrs");
                        }
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Fixawyy Platform - Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }
}
