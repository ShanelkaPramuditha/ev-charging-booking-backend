using System.Security.Cryptography;
using System.Text;

namespace EadChargingBookingBackend.Services;

public interface IQRCodeService
{
    string GenerateQRCode(string bookingId, string evOwnerId, string stationId, DateTime bookingDate);
    bool ValidateQRCode(string qrCode, string bookingId, string evOwnerId, string stationId);
}

public class QRCodeService : IQRCodeService
{
    private readonly string _secretKey;

    public QRCodeService(IConfiguration configuration)
    {
        _secretKey = configuration["JwtSettings:SecretKey"] ??
            throw new InvalidOperationException("QR code secret key not configured");
    }

    public string GenerateQRCode(string bookingId, string evOwnerId, string stationId, DateTime bookingDate)
    {
        // Format: bookingId|evOwnerId|stationId|timestamp|hash
        var timestamp = bookingDate.ToString("yyyyMMddHHmm");

        var dataToHash = $"{bookingId}|{evOwnerId}|{stationId}|{timestamp}|{_secretKey}";
        var hash = ComputeSha256Hash(dataToHash);

        // Take first 16 chars of hash for brevity
        var shortHash = hash.Substring(0, 16);

        var qrData = $"{bookingId}|{evOwnerId}|{stationId}|{timestamp}|{shortHash}";

        // Convert to Base64 for shorter QR code
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(qrData));
    }

    public bool ValidateQRCode(string qrCode, string bookingId, string evOwnerId, string stationId)
    {
        try
        {
            // Decode Base64
            var decodedQR = Encoding.UTF8.GetString(Convert.FromBase64String(qrCode));

            // Split into parts
            var parts = decodedQR.Split('|');

            if (parts.Length != 5)
                return false;

            var qrBookingId = parts[0];
            var qrEvOwnerId = parts[1];
            var qrStationId = parts[2];
            var qrTimestamp = parts[3];
            var qrHash = parts[4];

            // Verify booking ID, EV owner ID, and station ID
            if (qrBookingId != bookingId || qrEvOwnerId != evOwnerId || qrStationId != stationId)
                return false;

            // Verify hash
            var dataToHash = $"{qrBookingId}|{qrEvOwnerId}|{qrStationId}|{qrTimestamp}|{_secretKey}";
            var computedHash = ComputeSha256Hash(dataToHash);

            return computedHash.Substring(0, 16) == qrHash;
        }
        catch
        {
            return false;
        }
    }

    private string ComputeSha256Hash(string data)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            var hash = sha256.ComputeHash(bytes);

            var builder = new StringBuilder();
            foreach (var b in hash)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }
    }
}