/*
 * File Name: JwtSettings.cs
 * Description: JWT authentication configuration settings.
 */

namespace EadChargingBookingBackend.Configuration;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;

    public void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(SecretKey))
            throw new InvalidOperationException("JWT SecretKey is required");

        if (string.IsNullOrWhiteSpace(Issuer))
            throw new InvalidOperationException("JWT Issuer is required");

        if (string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("JWT Audience is required");

        if (ExpirationMinutes <= 0)
            throw new InvalidOperationException("JWT ExpirationMinutes must be greater than 0");
    }
}