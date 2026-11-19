using Rackroom.Infrastructure.QrCode;

namespace Rackroom.Tests;

public class Stage4QrCodeTests
{
    [Fact]
    public void Qr_code_generator_returns_a_png()
    {
        var generator = new QrCodeGeneratorService();

        byte[] png = generator.GeneratePng(
            "https://rackroom.local/Attendance/CheckIn?token=test");

        Assert.NotEmpty(png);

        // PNG files begin with the standard 8-byte PNG signature.
        byte[] signature = [137, 80, 78, 71, 13, 10, 26, 10];

        Assert.Equal(signature, png[..8]);
    }
}
