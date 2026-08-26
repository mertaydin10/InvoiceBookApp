namespace FaturaDefteri.Api.Dtos;

public record IssuerRequest(
    string TradeName,
    string? TaxOffice,
    string? TaxNumber,
    string? Address,
    string? Email,
    string? Phone,
    string? Iban,
    string Currency);

public record IssuerResponse(
    long Id,
    string TradeName,
    string? TaxOffice,
    string? TaxNumber,
    string? Address,
    string? Email,
    string? Phone,
    string? Iban,
    string Currency);

public record ClientRequest(
    string Name,
    string? TaxNumber,
    string? Email,
    string? Phone,
    string? Address);

public record ClientResponse(
    long Id,
    string Name,
    string? TaxNumber,
    string? Email,
    string? Phone,
    string? Address,
    DateTime CreatedAtUtc);

public record ClientBalanceResponse(
    long Id,
    string Name,
    string? TaxNumber,
    string? Email,
    string? Phone,
    decimal TotalInvoiced,
    decimal TotalPaid,
    decimal Outstanding,
    int OverdueCount);

public record InvoiceLineRequest(string Description, decimal Quantity, decimal UnitPrice, decimal VatRate);

public record InvoiceLineResponse(
    long Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal VatRate,
    decimal Net,
    decimal Vat,
    decimal Gross);

public record CreateInvoiceRequest(
    long ClientId,
    DateOnly IssueDate,
    DateOnly DueDate,
    string? Notes,
    List<InvoiceLineRequest> Lines);

public record UpdateInvoiceRequest(
    long ClientId,
    DateOnly IssueDate,
    DateOnly DueDate,
    string? Notes,
    List<InvoiceLineRequest> Lines);

public record InvoiceListItem(
    long Id,
    string Number,
    long ClientId,
    string ClientName,
    DateOnly IssueDate,
    DateOnly DueDate,
    string Status,
    bool Overdue,
    decimal Gross);

public record InvoiceDetail(
    long Id,
    string Number,
    long ClientId,
    string ClientName,
    DateOnly IssueDate,
    DateOnly DueDate,
    string Status,
    bool Overdue,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime? PaidAtUtc,
    decimal Net,
    decimal Vat,
    decimal Gross,
    List<InvoiceLineResponse> Lines);

public record SummaryResponse(
    int ClientCount,
    int OpenInvoiceCount,
    int OverdueCount,
    decimal OpenGross,
    decimal OverdueGross,
    decimal PaidThisMonthGross,
    string Currency);

public record MonthlyRevenueItem(
    string Month,
    string Label,
    decimal Revenue);

public record ActivityItem(
    string Action,
    string Description,
    DateTime Timestamp);

public record HealthResponse(string Status, string Database);
