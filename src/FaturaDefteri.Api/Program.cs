using FaturaDefteri.Api.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddFaturaDefteri(builder.Configuration, builder.Environment);

var app = builder.Build();

// Initialize database
await app.InitializeDatabaseAsync();

// Configure middleware
app.ConfigureFaturaDefteri();

app.Run();

public partial class Program;
