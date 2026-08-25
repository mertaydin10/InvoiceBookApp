namespace FaturaDefteri.Api.Entities;

public class InvoiceLine
{
    public long Id { get; set; }
    public long InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public string Description { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
}
