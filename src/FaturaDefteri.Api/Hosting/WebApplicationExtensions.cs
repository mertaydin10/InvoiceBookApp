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
            // If Users table doesn't exist, recreate the database
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
        }

        return app;
    }

    public static WebApplication ConfigureFaturaDefteri(this WebApplication app)
    {
        // Swagger (Development only)
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "FaturaDefteri"));
        }

        // Static files & SPA
        app.UseDefaultFiles();
        app.UseStaticFiles();

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // API Controllers
        app.MapControllers();

        // Fallback to index.html for SPA
        app.MapFallbackToFile("index.html");

        return app;
    }
}
