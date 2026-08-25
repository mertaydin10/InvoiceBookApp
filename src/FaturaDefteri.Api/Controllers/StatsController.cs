using FaturaDefteri.Api.Data;
using FaturaDefteri.Api.Dtos;
using FaturaDefteri.Api.Entities;
using FaturaDefteri.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaturaDefteri.Api.Controllers;

[ApiController]
[Route("api/stats")]
public class StatsController(FaturaDbContext db) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<SummaryResponse>> Summary(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var issuer = await db.IssuerProfiles.AsNoTracking().OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        var currency = issuer?.Currency ?? "TRY";

        var invoices = await db.Invoices.AsNoTracking().Include(i => i.Lines).ToListAsync(ct);
        var clients = await db.Clients.CountAsync(ct);

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

    private static decimal SumGross(IEnumerable<Invoice> invoices) =>
        invoices.Sum(i => Money.Totals(i.Lines).Gross);
}
