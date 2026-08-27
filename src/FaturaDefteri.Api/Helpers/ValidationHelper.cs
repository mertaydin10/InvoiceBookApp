using System.Text.RegularExpressions;

namespace FaturaDefteri.Api.Helpers;

public static class ValidationHelper
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PhoneRegex = new(
        @"^[\d\s\-\+\(\)]+$",
        RegexOptions.Compiled);

    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        
        email = email.Trim();
        return email.Length <= 254 && EmailRegex.IsMatch(email);
    }

    public static bool IsValidPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return true;
        
        phone = phone.Trim();
        var digitsOnly = Regex.Replace(phone, @"[^\d]", "");
        return digitsOnly.Length >= 10 && digitsOnly.Length <= 15 && PhoneRegex.IsMatch(phone);
    }

    public static bool IsValidTaxNumber(string? taxNumber)
    {
        if (string.IsNullOrWhiteSpace(taxNumber))
            return true;
        
        taxNumber = taxNumber.Trim();
        var digitsOnly = Regex.Replace(taxNumber, @"[^\d]", "");
        return digitsOnly.Length >= 10 && digitsOnly.Length <= 11;
    }

    public static bool IsValidIban(string? iban)
    {
        if (string.IsNullOrWhiteSpace(iban))
            return true;
        
        iban = Regex.Replace(iban.Trim().ToUpperInvariant(), @"\s", "");
        return iban.Length >= 15 && iban.Length <= 34 && iban.StartsWith("TR");
    }
}
