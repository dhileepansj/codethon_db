using QRCoder;

namespace DCView.Hackathon.Application.Services;

/// <summary>
/// Generates short codes and QR code images for survey links.
/// </summary>
public static class SurveyShortUrlService
{
    private static readonly Random _random = new();
    private const string CHARS = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

    /// <summary>
    /// Generate a 6-character alphanumeric short code (no ambiguous chars like 0/O/1/l).
    /// </summary>
    public static string GenerateShortCode(int length = 6)
    {
        var code = new char[length];
        for (var i = 0; i < length; i++)
            code[i] = CHARS[_random.Next(CHARS.Length)];
        return new string(code);
    }

    /// <summary>
    /// Build the short URL from a code. Example: https://yourdomain.com/hackathonapi/s/Ax7kQ3
    /// </summary>
    public static string BuildShortUrl(string baseApiUrl, string shortCode)
    {
        return $"{baseApiUrl.TrimEnd('/')}/s/{shortCode}";
    }

    /// <summary>
    /// Generate a QR code image as a PNG byte array for the given URL.
    /// </summary>
    public static byte[] GenerateQrCodePng(string url, int pixelsPerModule = 8)
    {
        using var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(pixelsPerModule);
    }

    /// <summary>
    /// Generate a QR code as a base64-encoded PNG for embedding in HTML emails.
    /// </summary>
    public static string GenerateQrCodeBase64(string url, int pixelsPerModule = 6)
    {
        var pngBytes = GenerateQrCodePng(url, pixelsPerModule);
        return Convert.ToBase64String(pngBytes);
    }
}
