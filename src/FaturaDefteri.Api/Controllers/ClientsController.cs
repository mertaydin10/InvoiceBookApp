using FaturaDefteri.Api.Data;
using FaturaDefteri.Api.Dtos;
using FaturaDefteri.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaturaDefteri.Api.Controllers;

[ApiController]
[Route("api/clients")]
public class ClientsController(FaturaDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ClientResponse>>> List(CancellationToken ct)
    {
        var rows = await db.Clients.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);
        return Ok(rows.Select(Map).ToList());
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ClientResponse>> Get(long id, CancellationToken ct)
    {
        var row = await db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        return row is null ? NotFound() : Ok(Map(row));
    }

    [HttpPost]
    public async Task<ActionResult<ClientResponse>> Create(ClientRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Müşteri adı gerekli." });

        var row = new Client
        {
            Name = request.Name.Trim(),
            TaxNumber = EmptyToNull(request.TaxNumber),
            Email = EmptyToNull(request.Email),
            Phone = EmptyToNull(request.Phone),
            Address = EmptyToNull(request.Address),
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Clients.Add(row);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = row.Id }, Map(row));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ClientResponse>> Update(long id, ClientRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Müşteri adı gerekli." });

        var row = await db.Clients.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (row is null)
            return NotFound();

        row.Name = request.Name.Trim();
        row.TaxNumber = EmptyToNull(request.TaxNumber);
        row.Email = EmptyToNull(request.Email);
        row.Phone = EmptyToNull(request.Phone);
        row.Address = EmptyToNull(request.Address);
        await db.SaveChangesAsync(ct);
        return Ok(Map(row));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var row = await db.Clients.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (row is null)
            return NotFound();

        var hasInvoices = await db.Invoices.AnyAsync(i => i.ClientId == id, ct);
        if (hasInvoices)
            return Conflict(new { error = "Bu müşterinin faturaları var; önce faturaları silin." });

        db.Clients.Remove(row);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static ClientResponse Map(Client row) =>
        new(row.Id, row.Name, row.TaxNumber, row.Email, row.Phone, row.Address, row.CreatedAtUtc);

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
