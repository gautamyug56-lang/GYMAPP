using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Infrastructure.QrCode;
using Rackroom.Application.Abstractions;

namespace Rackroom.Web.Pages.Attendance;

[Authorize(Policy = "StaffOrAdmin")]
public class DeskModel : PageModel
{
    private static readonly TimeSpan QrLifetime = TimeSpan.FromSeconds(30);

    private readonly ICurrentGymContext _currentGymContext;
    private readonly IAttendanceTokenService _tokenService;
    private readonly IQrCodeGenerator _qrCodeGenerator;

    public DeskModel(
        ICurrentGymContext currentGymContext,
        IAttendanceTokenService tokenService,
        IQrCodeGenerator qrCodeGenerator)
    {
        _currentGymContext = currentGymContext;
        _tokenService = tokenService;
        _qrCodeGenerator = qrCodeGenerator;
    }

    public string QrImageBase64 { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public void OnGet()
    {
        Guid gymId = _currentGymContext.GymId
            ?? throw new InvalidOperationException("No gym is associated with the current user.");

        ExpiresAtUtc = DateTimeOffset.UtcNow.Add(QrLifetime);

        string token = _tokenService.CreateToken(gymId, ExpiresAtUtc);
        string checkInUrl = $"/Attendance/CheckIn?token={Uri.EscapeDataString(token)}";

        QrImageBase64 = Convert.ToBase64String(
            _qrCodeGenerator.GeneratePng(checkInUrl));
    }
}
