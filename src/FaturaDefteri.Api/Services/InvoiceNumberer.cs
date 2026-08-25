using FaturaDefteri.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace FaturaDefteri.Api.Services;

public class InvoiceNumberer(FaturaDbContext db)
{
    public async Task<string> NextAsync(long userId, int year, CancellationToken ct)
    {
        var prefix = $"FAT-{year}-";
        var last = await db.Invoices
            .Where(i => i.UserId == userId && i.Number.StartsWith(prefix))
            .Select(i => i.Number)
            .ToListAsync(ct);

        var max = 0;
        foreach (var n in last)
        {
            var tail = n[prefix.Length..];
            if (int.TryParse(tail, out var value) && value > max)
                max = value;
        }

        return $"{prefix}{(max + 1):D3}";
    }
}
