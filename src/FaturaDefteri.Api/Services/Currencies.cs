namespace FaturaDefteri.Api.Services;

public static class Currencies
{
    public static readonly IReadOnlyList<CurrencyOption> All =
    [
        new("TRY", "Türk Lirası", "₺"),
        new("USD", "ABD Doları", "$"),
        new("EUR", "Euro", "€"),
        new("GBP", "İngiliz Sterlini", "£"),
        new("CHF", "İsviçre Frangı", "Fr"),
        new("JPY", "Japon Yeni", "¥"),
        new("CNY", "Çin Yuanı", "¥"),
        new("AUD", "Avustralya Doları", "$"),
        new("CAD", "Kanada Doları", "$"),
        new("NZD", "Yeni Zelanda Doları", "$"),
        new("SEK", "İsveç Kronu", "kr"),
        new("NOK", "Norveç Kronu", "kr"),
        new("DKK", "Danimarka Kronu", "kr"),
        new("PLN", "Polonya Zlotisi", "zł"),
        new("CZK", "Çek Korunası", "Kč"),
        new("HUF", "Macar Forinti", "Ft"),
        new("RON", "Rumen Leyi", "lei"),
        new("BGN", "Bulgar Levası", "лв"),
        new("RUB", "Rus Rublesi", "₽"),
        new("INR", "Hint Rupisi", "₹"),
        new("KRW", "Güney Kore Wonu", "₩"),
        new("SGD", "Singapur Doları", "$"),
        new("HKD", "Hong Kong Doları", "$"),
        new("BRL", "Brezilya Reali", "R$"),
        new("MXN", "Meksika Pesosu", "$"),
        new("ZAR", "Güney Afrika Randı", "R"),
        new("AED", "BAE Dirhemi", "د.إ"),
        new("SAR", "Suudi Riyali", "﷼"),
        new("QAR", "Katar Riyali", "﷼"),
        new("KWD", "Kuveyt Dinarı", "د.ك"),
        new("BHD", "Bahreyn Dinarı", ".د.ب"),
        new("OMR", "Umman Riyali", "﷼"),
        new("EGP", "Mısır Lirası", "£"),
        new("ILS", "İsrail Şekeli", "₪"),
        new("AZN", "Azerbaycan Manatı", "₼"),
        new("GEL", "Gürcistan Larisi", "₾"),
        new("KZT", "Kazak Tengesi", "₸"),
        new("UAH", "Ukrayna Grivnası", "₴")
    ];

    public static bool IsSupported(string? code) =>
        !string.IsNullOrWhiteSpace(code) && All.Any(c => c.Code == code.Trim().ToUpperInvariant());

    public static string Normalize(string? code) =>
        IsSupported(code) ? code!.Trim().ToUpperInvariant() : "TRY";
}

public sealed record CurrencyOption(string Code, string Name, string Symbol);
