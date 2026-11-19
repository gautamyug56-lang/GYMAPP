namespace Rackroom.Application.Abstractions;

public interface IAttendanceTokenService
{
    string CreateToken(Guid gymId, DateTimeOffset expiresAtUtc);

    bool TryValidateToken(
        string token,
        out Guid gymId,
        out DateTimeOffset expiresAtUtc);
}
