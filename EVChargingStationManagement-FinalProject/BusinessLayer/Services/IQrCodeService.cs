namespace BusinessLayer.Services
{
    public interface IQrCodeService
    {
        string GenerateQrCodeForSpot(Guid spotId);
        Guid? ParseQrCode(string qrCode);
        bool ValidateQrCode(string qrCode, Guid spotId);
    }
}

