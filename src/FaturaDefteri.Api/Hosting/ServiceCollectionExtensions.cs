using System.Text;
using FaturaDefteri.Api.Data;
using FaturaDefteri.Api.Entities;
using FaturaDefteri.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FaturaDefteri.Api.Hosting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFaturaDefteri(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var dataDir = Path.Combine(environment.ContentRootPath, "data");
        Directory.CreateDirectory(dataDir);
        var connection = configuration.GetConnectionString("Sqlite") ?? "Data Source=data/fatura.db";
        services.AddDbContext<FaturaDbContext>(o => o.UseSqlite(connection));

        services.AddScoped<InvoiceNumberer>();
        services.AddSingleton<PasswordHasher<User>>();

        services.AddControllers();
        services.AddOpenApi();
        services.AddEndpointsApiExplorer();

        var jwtKey = configuration["Jwt:Key"] ?? "fatura-defteri-dev-key-min-32-characters!";
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"] ?? "FaturaDefteri",
                    ValidAudience = configuration["Jwt:Audience"] ?? "FaturaDefteri",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.Zero
                };
                o.Events = new JwtBearerEvents
                {
                    OnChallenge = async ctx =>
                    {
                        ctx.HandleResponse();
                        ctx.Response.StatusCode = 401;
                        ctx.Response.ContentType = "application/json";
                        await ctx.Response.WriteAsJsonAsync(new { error = "Oturum gerekli veya süresi doldu." });
                    }
                };
            });
        services.AddAuthorization();

        return services;
    }
}
