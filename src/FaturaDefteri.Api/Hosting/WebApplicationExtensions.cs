using FaturaDefteri.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace FaturaDefteri.Api.Hosting;

public static class WebApplicationExtensions
{
    public static async Task<WebApplication> InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FaturaDbContext>();
        await db.Database.EnsureCreatedAsync();
        
        try
        {
            _ = await db.Users.CountAsync();
        }
        catch
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
        }

        return app;
    }

    public static WebApplication ConfigureFaturaDefteri(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "FaturaDefteri"));
        }

        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.MapFallbackToFile("index.html");

        return app;
    }
}
