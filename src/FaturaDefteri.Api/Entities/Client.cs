namespace FaturaDefteri.Api.Entities;

public class Client
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public string Name { get; set; } = "";
    public string? TaxNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public List<Invoice> Invoices { get; set; } = [];
}
