namespace FaturaDefteri.Api.Entities;

public class IssuerProfile
{
    public long Id { get; set; }
    public string TradeName { get; set; } = "";
    public string? TaxOffice { get; set; }
    public string? TaxNumber { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Iban { get; set; }
    public string Currency { get; set; } = "TRY";
}
