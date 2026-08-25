using FaturaDefteri.Api.Entities;

namespace FaturaDefteri.Api.Services;

public static class Money
{
    public static decimal LineNet(InvoiceLine line) =>
        decimal.Round(line.Quantity * line.UnitPrice, 2, MidpointRounding.AwayFromZero);

    public static decimal LineVat(InvoiceLine line) =>
        decimal.Round(LineNet(line) * line.VatRate / 100m, 2, MidpointRounding.AwayFromZero);

    public static decimal LineGross(InvoiceLine line) => LineNet(line) + LineVat(line);

    public static (decimal Net, decimal Vat, decimal Gross) Totals(IEnumerable<InvoiceLine> lines)
    {
        var net = 0m;
        var vat = 0m;
        foreach (var line in lines)
        {
            net += LineNet(line);
            vat += LineVat(line);
        }

        return (net, vat, net + vat);
    }

    public static InvoiceStatus EffectiveStatus(Invoice invoice, DateOnly today)
    {
        if (invoice.Status is InvoiceStatus.Paid or InvoiceStatus.Cancelled or InvoiceStatus.Draft)
            return invoice.Status;
        return invoice.DueDate < today ? InvoiceStatus.Sent : invoice.Status;
    }
}
