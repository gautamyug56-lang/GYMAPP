using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Rackroom.Application.Abstractions;

namespace Rackroom.Infrastructure.Security;

public sealed class AttendanceTokenService : IAttendanceTokenService
{
    private readonly ITimeLimitedDataProtector _protector;

    public AttendanceTokenService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider
            .CreateProtector("Rackroom.Attendance.QrCode.v1")
            .ToTimeLimitedDataProtector();
    }

    public string CreateToken(Guid gymId, DateTimeOffset expiresAtUtc)
    {
        return _protector.Protect(gymId.ToString(), expiresAtUtc);
    }

    public bool TryValidateToken(
        string token,
        out Guid gymId,
        out DateTimeOffset expiresAtUtc)
    {
        gymId = Guid.Empty;
        expiresAtUtc = default;

        try
        {
            string payload = _protector.Unprotect(token, out expiresAtUtc);

            return Guid.TryParse(payload, out gymId);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
