using FaturaDefteri.Api.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFaturaDefteri(builder.Configuration, builder.Environment);

var app = builder.Build();

await app.InitializeDatabaseAsync();

app.ConfigureFaturaDefteri();

app.Run();

public partial class Program;
