using FaturaDefteri.Api.Data;
using FaturaDefteri.Api.Dtos;
using FaturaDefteri.Api.Entities;
using FaturaDefteri.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaturaDefteri.Api.Controllers;

[ApiController]
[Route("api/invoices")]
public class InvoicesController(FaturaDbContext db, InvoiceNumberer numbers) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<InvoiceListItem>>> List(
        [FromQuery] string? status,
        [FromQuery] long? clientId,
        CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = db.Invoices.AsNoTracking().Include(i => i.Client).Include(i => i.Lines).AsQueryable();
        if (clientId is not null)
            query = query.Where(i => i.ClientId == clientId);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InvoiceStatus>(status, true, out var parsed))
            query = query.Where(i => i.Status == parsed);

        var rows = await query.OrderByDescending(i => i.IssueDate).ThenByDescending(i => i.Id).ToListAsync(ct);
        if (string.Equals(status, "overdue", StringComparison.OrdinalIgnoreCase))
            rows = rows.Where(i => i.Status == InvoiceStatus.Sent && i.DueDate < today).ToList();

        return Ok(rows.Select(i => MapList(i, today)).ToList());
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<InvoiceDetail>> Get(long id, CancellationToken ct)
    {
        var row = await db.Invoices.AsNoTracking()
            .Include(i => i.Client)
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
        return row is null ? NotFound() : Ok(MapDetail(row, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceDetail>> Create(CreateInvoiceRequest request, CancellationToken ct)
    {
        var error = ValidateLines(request.Lines);
        if (error is not null)
            return BadRequest(new { error });

        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == request.ClientId, ct);
        if (client is null)
            return BadRequest(new { error = "Müşteri bulunamadı." });
        if (request.DueDate < request.IssueDate)
            return BadRequest(new { error = "Vade, fatura tarihinden önce olamaz." });

        var invoice = new Invoice
        {
            ClientId = client.Id,
            Number = await numbers.NextAsync(request.IssueDate.Year, ct),
            IssueDate = request.IssueDate,
            DueDate = request.DueDate,
            Status = InvoiceStatus.Draft,
            Notes = EmptyToNull(request.Notes),
            CreatedAtUtc = DateTime.UtcNow,
            Lines = request.Lines.Select(ToLine).ToList()
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);
        invoice.Client = client;
        return CreatedAtAction(nameof(Get), new { id = invoice.Id }, MapDetail(invoice, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<InvoiceDetail>> Update(long id, UpdateInvoiceRequest request, CancellationToken ct)
    {
        var invoice = await db.Invoices.Include(i => i.Lines).Include(i => i.Client).FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invoice is null)
            return NotFound();
        if (invoice.Status is InvoiceStatus.Paid or InvoiceStatus.Cancelled)
            return Conflict(new { error = "Ödenmiş veya iptal fatura düzenlenemez." });

        var error = ValidateLines(request.Lines);
        if (error is not null)
            return BadRequest(new { error });

        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == request.ClientId, ct);
        if (client is null)
            return BadRequest(new { error = "Müşteri bulunamadı." });
        if (request.DueDate < request.IssueDate)
            return BadRequest(new { error = "Vade, fatura tarihinden önce olamaz." });

        invoice.ClientId = client.Id;
        invoice.Client = client;
        invoice.IssueDate = request.IssueDate;
        invoice.DueDate = request.DueDate;
        invoice.Notes = EmptyToNull(request.Notes);
        db.InvoiceLines.RemoveRange(invoice.Lines);
        invoice.Lines = request.Lines.Select(ToLine).ToList();
        await db.SaveChangesAsync(ct);
        return Ok(MapDetail(invoice, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [HttpPost("{id:long}/send")]
    public async Task<ActionResult<InvoiceDetail>> Send(long id, CancellationToken ct)
    {
        var invoice = await Load(id, ct);
        if (invoice is null)
            return NotFound();
        if (invoice.Status != InvoiceStatus.Draft)
            return Conflict(new { error = "Yalnızca taslak fatura gönderilebilir." });
        if (invoice.Lines.Count == 0)
            return BadRequest(new { error = "Kalemsiz fatura gönderilemez." });
        invoice.Status = InvoiceStatus.Sent;
        await db.SaveChangesAsync(ct);
        return Ok(MapDetail(invoice, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [HttpPost("{id:long}/pay")]
    public async Task<ActionResult<InvoiceDetail>> Pay(long id, CancellationToken ct)
    {
        var invoice = await Load(id, ct);
        if (invoice is null)
            return NotFound();
        if (invoice.Status is InvoiceStatus.Paid or InvoiceStatus.Cancelled)
            return Conflict(new { error = "Bu fatura ödenemez." });
        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(MapDetail(invoice, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [HttpPost("{id:long}/cancel")]
    public async Task<ActionResult<InvoiceDetail>> Cancel(long id, CancellationToken ct)
    {
        var invoice = await Load(id, ct);
        if (invoice is null)
            return NotFound();
        if (invoice.Status == InvoiceStatus.Paid)
            return Conflict(new { error = "Ödenmiş fatura iptal edilemez." });
        invoice.Status = InvoiceStatus.Cancelled;
        await db.SaveChangesAsync(ct);
        return Ok(MapDetail(invoice, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invoice is null)
            return NotFound();
        if (invoice.Status != InvoiceStatus.Draft)
            return Conflict(new { error = "Yalnızca taslak fatura silinebilir." });
        db.Invoices.Remove(invoice);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private Task<Invoice?> Load(long id, CancellationToken ct) =>
        db.Invoices.Include(i => i.Client).Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == id, ct);

    private static string? ValidateLines(List<InvoiceLineRequest>? lines)
    {
        if (lines is null || lines.Count == 0)
            return "En az bir kalem gerekli.";
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line.Description))
                return "Kalem açıklaması boş olamaz.";
            if (line.Quantity <= 0)
                return "Miktar 0'dan büyük olmalı.";
            if (line.UnitPrice < 0)
                return "Birim fiyat negatif olamaz.";
            if (line.VatRate is < 0 or > 100)
                return "KDV oranı 0–100 arasında olmalı.";
        }

        return null;
    }

    private static InvoiceLine ToLine(InvoiceLineRequest line) => new()
    {
        Description = line.Description.Trim(),
        Quantity = decimal.Round(line.Quantity, 2, MidpointRounding.AwayFromZero),
        UnitPrice = decimal.Round(line.UnitPrice, 2, MidpointRounding.AwayFromZero),
        VatRate = decimal.Round(line.VatRate, 2, MidpointRounding.AwayFromZero)
    };

    private static InvoiceListItem MapList(Invoice invoice, DateOnly today)
    {
        var (_, _, gross) = Money.Totals(invoice.Lines);
        var overdue = invoice.Status == InvoiceStatus.Sent && invoice.DueDate < today;
        return new InvoiceListItem(
            invoice.Id,
            invoice.Number,
            invoice.ClientId,
            invoice.Client.Name,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.Status.ToString(),
            overdue,
            gross);
    }

    private static InvoiceDetail MapDetail(Invoice invoice, DateOnly today)
    {
        var (net, vat, gross) = Money.Totals(invoice.Lines);
        var overdue = invoice.Status == InvoiceStatus.Sent && invoice.DueDate < today;
        var lines = invoice.Lines.Select(l => new InvoiceLineResponse(
            l.Id,
            l.Description,
            l.Quantity,
            l.UnitPrice,
            l.VatRate,
            Money.LineNet(l),
            Money.LineVat(l),
            Money.LineGross(l))).ToList();
        return new InvoiceDetail(
            invoice.Id,
            invoice.Number,
            invoice.ClientId,
            invoice.Client.Name,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.Status.ToString(),
            overdue,
            invoice.Notes,
            invoice.CreatedAtUtc,
            invoice.PaidAtUtc,
            net,
            vat,
            gross,
            lines);
    }

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
