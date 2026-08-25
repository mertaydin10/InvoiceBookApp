using System.Security.Claims;

namespace FaturaDefteri.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static long GetRequiredUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(value, out var id))
            throw new InvalidOperationException("Kullanıcı kimliği yok.");
        return id;
    }
}
