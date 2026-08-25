using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FaturaDefteri.Api.Auth;
using FaturaDefteri.Api.Data;
using FaturaDefteri.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FaturaDefteri.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    FaturaDbContext db,
    IConfiguration configuration,
    PasswordHasher<User> passwordHasher) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "E-posta ve şifre gerekli." });
        if (request.Password.Length < 4)
            return BadRequest(new { error = "Şifre en az 4 karakter olmalı." });
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            return Conflict(new { error = "Bu e-posta zaten kayıtlı." });

        var user = new User
        {
            Email = email,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? email : request.DisplayName.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return Ok(Issue(user));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
            return Unauthorized(new { error = "E-posta veya şifre yanlış." });

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized(new { error = "E-posta veya şifre yanlış." });

        return Ok(Issue(user));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<MeResponse>> Me(CancellationToken ct)
    {
        var id = User.GetRequiredUserId();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
            return Unauthorized();
        return Ok(new MeResponse(user.Id, user.Email, user.DisplayName));
    }

    private LoginResponse Issue(User user)
    {
        var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key eksik.");
        var expires = DateTimeOffset.UtcNow.AddHours(8);
        var token = new JwtSecurityToken(
            configuration["Jwt:Issuer"] ?? "FaturaDefteri",
            configuration["Jwt:Audience"] ?? "FaturaDefteri",
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            ],
            expires: expires.UtcDateTime,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256));
        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, user.DisplayName);
    }
}

public record RegisterRequest(string Email, string Password, string? DisplayName);
public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, DateTimeOffset ExpiresAt, string DisplayName);
public record MeResponse(long Id, string Email, string DisplayName);
