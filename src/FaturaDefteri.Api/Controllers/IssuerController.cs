using FaturaDefteri.Api.Auth;
using FaturaDefteri.Api.Data;
using FaturaDefteri.Api.Dtos;
using FaturaDefteri.Api.Entities;
using FaturaDefteri.Api.Helpers;
using FaturaDefteri.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaturaDefteri.Api.Controllers;

[ApiController]
[Route("api/issuer")]
[Authorize]
public class IssuerController(FaturaDbContext db) : ControllerBase
{
    private long UserId => User.GetRequiredUserId();

    [HttpGet]
    public async Task<ActionResult<IssuerResponse>> Get(CancellationToken ct)
    {
        var row = await db.IssuerProfiles.FirstOrDefaultAsync(x => x.UserId == UserId, ct);
        if (row is null)
            return Ok(new IssuerResponse(0, "", null, null, null, null, null, null, "TRY"));
        return Ok(Map(row));
    }

    [HttpPut]
    public async Task<ActionResult<IssuerResponse>> Put(IssuerRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.TradeName))
            return BadRequest(new { error = "Unvan gerekli." });
        if (!Currencies.IsSupported(request.Currency))
            return BadRequest(new { error = "Para birimi listeden seçilmeli." });
        if (!string.IsNullOrWhiteSpace(request.Email) && !ValidationHelper.IsValidEmail(request.Email))
            return BadRequest(new { error = "Geçerli bir e-posta adresi girin." });
        if (!string.IsNullOrWhiteSpace(request.Phone) && !ValidationHelper.IsValidPhone(request.Phone))
            return BadRequest(new { error = "Geçerli bir telefon numarası girin." });
        if (!string.IsNullOrWhiteSpace(request.TaxNumber) && !ValidationHelper.IsValidTaxNumber(request.TaxNumber))
            return BadRequest(new { error = "Vergi numarası 10 veya 11 haneli olmalı." });
        if (!string.IsNullOrWhiteSpace(request.Iban) && !ValidationHelper.IsValidIban(request.Iban))
            return BadRequest(new { error = "Geçerli bir TR IBAN girin." });

        var row = await db.IssuerProfiles.FirstOrDefaultAsync(x => x.UserId == UserId, ct);
        if (row is null)
        {
            row = new IssuerProfile { UserId = UserId };
            db.IssuerProfiles.Add(row);
        }

        row.TradeName = request.TradeName.Trim();
        row.TaxOffice = EmptyToNull(request.TaxOffice);
        row.TaxNumber = EmptyToNull(request.TaxNumber);
        row.Address = EmptyToNull(request.Address);
        row.Email = EmptyToNull(request.Email);
        row.Phone = EmptyToNull(request.Phone);
        row.Iban = EmptyToNull(request.Iban);
        row.Currency = Currencies.Normalize(request.Currency);
        await db.SaveChangesAsync(ct);
        return Ok(Map(row));
    }

    private static IssuerResponse Map(IssuerProfile row) =>
        new(row.Id, row.TradeName, row.TaxOffice, row.TaxNumber, row.Address, row.Email, row.Phone, row.Iban, row.Currency);

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
