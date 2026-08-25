using FaturaDefteri.Api.Data;
using FaturaDefteri.Api.Dtos;
using FaturaDefteri.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaturaDefteri.Api.Controllers;

[ApiController]
[Route("api/issuer")]
public class IssuerController(FaturaDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IssuerResponse>> Get(CancellationToken ct)
    {
        var row = await db.IssuerProfiles.OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        if (row is null)
            return Ok(new IssuerResponse(0, "", null, null, null, null, null, null, "TRY"));
        return Ok(Map(row));
    }

    [HttpPut]
    public async Task<ActionResult<IssuerResponse>> Put(IssuerRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.TradeName))
            return BadRequest(new { error = "Unvan gerekli." });

        var row = await db.IssuerProfiles.OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        if (row is null)
        {
            row = new IssuerProfile();
            db.IssuerProfiles.Add(row);
        }

        row.TradeName = request.TradeName.Trim();
        row.TaxOffice = EmptyToNull(request.TaxOffice);
        row.TaxNumber = EmptyToNull(request.TaxNumber);
        row.Address = EmptyToNull(request.Address);
        row.Email = EmptyToNull(request.Email);
        row.Phone = EmptyToNull(request.Phone);
        row.Iban = EmptyToNull(request.Iban);
        row.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency.Trim().ToUpperInvariant();
        await db.SaveChangesAsync(ct);
        return Ok(Map(row));
    }

    private static IssuerResponse Map(IssuerProfile row) =>
        new(row.Id, row.TradeName, row.TaxOffice, row.TaxNumber, row.Address, row.Email, row.Phone, row.Iban, row.Currency);

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
