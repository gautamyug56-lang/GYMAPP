using QRCoder;

namespace Rackroom.Infrastructure.QrCode;

public interface IQrCodeGenerator
{
    byte[] GeneratePng(string payload);
}

public sealed class QrCodeGeneratorService : IQrCodeGenerator
{
    public byte[] GeneratePng(string payload)
    {
        using QRCodeGenerator generator = new();
        using QRCodeData data = generator.CreateQrCode(
            payload,
            QRCodeGenerator.ECCLevel.Q);

        using PngByteQRCode qrCode = new(data);

        return qrCode.GetGraphic(12);
    }
}
