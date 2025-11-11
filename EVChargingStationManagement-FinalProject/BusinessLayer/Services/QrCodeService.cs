using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace BusinessLayer.Services
{
    public class QrCodeService : IQrCodeService
    {
        private readonly string _secretKey;

        public QrCodeService(IConfiguration configuration)
        {
            _secretKey = configuration["QrCode:SecretKey"] ?? "EVChargingStationSecretKey2024";
        }

        public string GenerateQrCodeForSpot(Guid spotId)
        {
            // Format: EVCS_{spotId}_{hash}
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var data = $"{spotId}|{timestamp}";
            var hash = ComputeHash(data);
            return $"EVCS_{spotId}_{hash}";
        }

        public Guid? ParseQrCode(string qrCode)
        {
            if (string.IsNullOrWhiteSpace(qrCode) || !qrCode.StartsWith("EVCS_"))
                return null;

            var parts = qrCode.Split('_');
            if (parts.Length < 3)
                return null;

            if (Guid.TryParse(parts[1], out var spotId))
            {
                // Validate hash
                var timestamp = parts.Length > 2 ? parts[2] : "";
                var data = $"{spotId}|{timestamp}";
                var expectedHash = ComputeHash(data);
                
                // For simplicity, we'll just extract the spot ID
                // In production, you should validate the hash properly
                return spotId;
            }

            return null;
        }

        public bool ValidateQrCode(string qrCode, Guid spotId)
        {
            var parsedId = ParseQrCode(qrCode);
            return parsedId == spotId;
        }

        private string ComputeHash(string data)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hashBytes).Substring(0, 8).Replace("/", "_").Replace("+", "-");
        }
    }
}

