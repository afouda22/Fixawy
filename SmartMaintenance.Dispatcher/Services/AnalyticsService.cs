using System.Text;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SmartMaintenance.Dispatcher.Data;
using SmartMaintenance.Dispatcher.DTOs;
using SmartMaintenance.Dispatcher.Models;

namespace SmartMaintenance.Dispatcher.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _context;

    public AnalyticsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<KpiSummaryDto> GetKpiSummaryAsync()
    {
        var totalTickets = await _context.Tickets.CountAsync();
        var pendingTickets = await _context.Tickets.CountAsync(t => t.Status == TicketStatus.Pending);
        var inProgressTickets = await _context.Tickets.CountAsync(t => t.Status == TicketStatus.InProgress || t.Status == TicketStatus.Assigned);
        var resolvedTickets = await _context.Tickets.CountAsync(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed);
        var totalTechnicians = await _context.Technicians.CountAsync();

        // حساب متوسط وقت الحل للتذاكر المنجزة بالساعات
        var resolvedList = await _context.Tickets
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

    public async Task<List<DepartmentBreakdownDto>> GetTicketsByDepartmentAsync()
    {
        return await _context.Tickets
            .GroupBy(t => t.Department)
            .Select(g => new DepartmentBreakdownDto
            {
                Department = g.Key,
                TicketCount = g.Count()
            })
            .OrderByDescending(d => d.TicketCount)
            .ToListAsync();
    }

    public async Task<List<StatusBreakdownDto>> GetTicketsByStatusAsync()
    {
        return await _context.Tickets
            .GroupBy(t => t.Status)
            .Select(g => new StatusBreakdownDto
            {
                Status = g.Key.ToString(),
                TicketCount = g.Count()
            })
            .ToListAsync();
    }

    public async Task<List<TechnicianRankingDto>> GetTechnicianRankingsAsync()
    {
        var technicians = await _context.Technicians
            .Include(t => t.Assignments)
            .ThenInclude(a => a.Ticket)
            .ToListAsync();

        var result = new List<TechnicianRankingDto>();

        foreach (var tech in technicians)
        {
            var completedAssignments = tech.Assignments
                .Where(a => a.Status == "Completed" && a.Ticket != null && a.Ticket.ResolvedAt != null)
                .ToList();

            double avgHours = 0;
            if (completedAssignments.Any())
            {
                avgHours = Math.Round(completedAssignments.Average(a => (a.Ticket!.ResolvedAt!.Value - a.AssignedAt).TotalHours), 1);
            }

            result.Add(new TechnicianRankingDto
            {
                TechnicianId = tech.Id,
                FullName = tech.FullName,
                Specialization = tech.Specialization,
                Rating = tech.Rating,
                CompletedTickets = completedAssignments.Count,
                ActiveTickets = tech.Assignments.Count(a => a.Status == "Active"),
                AvgResolutionHours = avgHours
            });
        }

        return result
            .OrderByDescending(r => r.Rating)
            .ThenByDescending(r => r.CompletedTickets)
            .ToList();
    }

    public async Task<byte[]> GenerateCsvReportAsync()
    {
        var tickets = await _context.Tickets
            .Include(t => t.Assignments)
            .ThenInclude(a => a.Technician)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("TicketId,Title,Department,RequiredSkill,Priority,Status,CreatedAt,ResolvedAt,DurationHours,AssignedTechnician");

        foreach (var t in tickets)
        {
            var techName = t.Assignments.OrderByDescending(a => a.AssignedAt).FirstOrDefault()?.Technician?.FullName ?? "Unassigned";
            double duration = t.ResolvedAt.HasValue ? Math.Round((t.ResolvedAt.Value - t.CreatedAt).TotalHours, 1) : 0;
            var resolvedStr = t.ResolvedAt.HasValue ? t.ResolvedAt.Value.ToString("yyyy-MM-dd HH:mm") : "N/A";

            sb.AppendLine($"{t.Id},\"{t.Title.Replace("\"", "\"\"")}\",\"{t.Department}\",\"{t.RequiredSkill}\",{t.Priority},{t.Status},{t.CreatedAt:yyyy-MM-dd HH:mm},{resolvedStr},{duration},\"{techName}\"");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> GeneratePdfReportAsync()
    {
        var kpi = await GetKpiSummaryAsync();
        var deptData = await GetTicketsByDepartmentAsync();
        var techRankings = await GetTechnicianRankingsAsync();

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
                            c.Item().Text("Smart Maintenance Platform").FontSize(20).Bold().FontColor(Colors.Blue.Darken3);
                            c.Item().Text("Executive Operations & Analytics Report").FontSize(11).FontColor(Colors.Grey.Medium);
                        });

                        row.ConstantItem(150).Column(c =>
                        {
                            c.Item().AlignRight().Text($"Date: {DateTime.UtcNow:yyyy-MM-dd}").FontSize(9);
                            c.Item().AlignRight().Text("Confidential - Operations").FontSize(9).FontColor(Colors.Grey.Medium);
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
                    col.Item().Text("Incidents Distribution by Department").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().PaddingTop(4).PaddingBottom(15).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(1);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Department").Bold();
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
                    x.Span("Smart Maintenance Platform - Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }
}
