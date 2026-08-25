namespace FaturaDefteri.Api.Entities;

public enum InvoiceStatus
{
    Draft = 0,
    Sent = 1,
    Paid = 2,
    Cancelled = 3
}

public class Invoice
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public long ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public string Number { get; set; } = "";
    public DateOnly IssueDate { get; set; }
    public DateOnly DueDate { get; set; }
    public InvoiceStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }

    public List<InvoiceLine> Lines { get; set; } = [];
}
