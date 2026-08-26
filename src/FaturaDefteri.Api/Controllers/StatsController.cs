using FaturaDefteri.Api.Auth;
using FaturaDefteri.Api.Data;
using FaturaDefteri.Api.Dtos;
using FaturaDefteri.Api.Entities;
using FaturaDefteri.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaturaDefteri.Api.Controllers;

[ApiController]
[Route("api/stats")]
[Authorize]
public class StatsController(FaturaDbContext db) : ControllerBase
{
    private long UserId => User.GetRequiredUserId();

    [HttpGet("summary")]
    public async Task<ActionResult<SummaryResponse>> Summary(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var issuer = await db.IssuerProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == UserId, ct);
        var currency = issuer?.Currency ?? "TRY";

        var invoices = await db.Invoices.AsNoTracking().Include(i => i.Lines).Where(i => i.UserId == UserId).ToListAsync(ct);
        var clients = await db.Clients.CountAsync(c => c.UserId == UserId, ct);

        var open = invoices.Where(i => i.Status == InvoiceStatus.Sent).ToList();
        var overdue = open.Where(i => i.DueDate < today).ToList();
        var paidThisMonth = invoices
            .Where(i => i.Status == InvoiceStatus.Paid && i.PaidAtUtc is not null)
            .Where(i => DateOnly.FromDateTime(DateTime.SpecifyKind(i.PaidAtUtc!.Value, DateTimeKind.Utc)) >= monthStart)
            .ToList();

        return Ok(new SummaryResponse(
            clients,
            open.Count,
            overdue.Count,
            SumGross(open),
            SumGross(overdue),
            SumGross(paidThisMonth),
            currency));
    }

    [HttpGet("monthly-revenue")]
    public async Task<ActionResult<List<MonthlyRevenueItem>>> MonthlyRevenue(CancellationToken ct)
    {
        var today = DateTime.UtcNow;
        var invoices = await db.Invoices.AsNoTracking()
            .Include(i => i.Lines)
            .Where(i => i.UserId == UserId && i.Status == InvoiceStatus.Paid && i.PaidAtUtc != null)
            .ToListAsync(ct);

        var months = new List<MonthlyRevenueItem>();
        for (int i = 5; i >= 0; i--)
        {
            var month = today.AddMonths(-i);
            var monthStart = new DateTime(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);
            
            var monthInvoices = invoices
                .Where(inv => inv.PaidAtUtc >= monthStart && inv.PaidAtUtc < monthEnd)
                .ToList();
            
            months.Add(new MonthlyRevenueItem(
                $"{month:yyyy-MM}",
                $"{month:MMM}",
                SumGross(monthInvoices)
            ));
        }

        return Ok(months);
    }

    private static decimal SumGross(IEnumerable<Invoice> invoices) =>
        invoices.Sum(i => Money.Totals(i.Lines).Gross);
}
