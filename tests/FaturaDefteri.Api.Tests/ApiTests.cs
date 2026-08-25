using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace FaturaDefteri.Api.Tests;

public class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Health_ok()
    {
        var client = factory.CreateClient();
        var res = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Invoice_flow()
    {
        var http = factory.CreateClient();
        var register = await http.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"u{Guid.NewGuid():N}@local.test",
            password = "demo",
            displayName = "Test"
        });
        register.EnsureSuccessStatusCode();
        var login = await register.Content.ReadFromJsonAsync<LoginDto>();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);

        var createdClient = await http.PostAsJsonAsync("/api/clients", new
        {
            name = "Acme Ltd",
            taxNumber = "123",
            email = "a@b.com",
            phone = (string?)null,
            address = (string?)null
        });
        createdClient.EnsureSuccessStatusCode();
        var client = await createdClient.Content.ReadFromJsonAsync<ClientDto>();
        Assert.NotNull(client);

        var created = await http.PostAsJsonAsync("/api/invoices", new
        {
            clientId = client!.Id,
            issueDate = "2026-08-01",
            dueDate = "2026-08-15",
            notes = "web sitesi",
            lines = new[]
            {
                new { description = "Tasarım", quantity = 1m, unitPrice = 1000m, vatRate = 20m }
            }
        });
        created.EnsureSuccessStatusCode();
        var invoice = await created.Content.ReadFromJsonAsync<InvoiceDto>();
        Assert.Equal("FAT-2026-001", invoice!.Number);
        Assert.Equal(1200m, invoice.Gross);

        var send = await http.PostAsync($"/api/invoices/{invoice.Id}/send", null);
        send.EnsureSuccessStatusCode();
        var pay = await http.PostAsync($"/api/invoices/{invoice.Id}/pay", null);
        pay.EnsureSuccessStatusCode();

        var summary = await http.GetFromJsonAsync<SummaryDto>("/api/stats/summary");
        Assert.Equal(1, summary!.ClientCount);
        Assert.Equal(0, summary.OpenInvoiceCount);
    }

    private sealed record LoginDto(string Token);
    private sealed record ClientDto(long Id, string Name);
    private sealed record InvoiceDto(long Id, string Number, decimal Gross);
    private sealed record SummaryDto(int ClientCount, int OpenInvoiceCount);
}
