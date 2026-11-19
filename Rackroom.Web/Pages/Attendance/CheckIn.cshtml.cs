using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.Abstractions;
using Rackroom.Application.Exceptions;
using Rackroom.Application.Services;

namespace Rackroom.Web.Pages.Attendance;

[Authorize(Policy = "MemberOnly")]
public class CheckInModel : PageModel
{
    private readonly IAttendanceManagementService _attendanceService;
    private readonly IAttendanceTokenService _tokenService;

    public CheckInModel(
        IAttendanceManagementService attendanceService,
        IAttendanceTokenService tokenService)
    {
        _attendanceService = attendanceService;
        _tokenService = tokenService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    public bool TokenIsValid { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool CheckedIn { get; private set; }

    public void OnGet()
    {
        TokenIsValid = IsValidToken(Token);

        if (!TokenIsValid)
        {
            ErrorMessage = "This QR code is invalid or has expired. Please scan the current code at the gym desk.";
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        TokenIsValid = IsValidToken(Token);

        if (!TokenIsValid)
        {
            ErrorMessage = "This QR code is invalid or has expired. Please scan the current code at the gym desk.";
            return Page();
        }

        string? memberIdValue = User.FindFirstValue("MemberId");

        if (!Guid.TryParse(memberIdValue, out Guid memberId))
        {
            ErrorMessage = "Your account is not linked to a gym member profile.";
            return Page();
        }

        try
        {
            await _attendanceService.CheckInWithQrAsync(
                memberId,
                Token!,
                DateTime.UtcNow,
                cancellationToken);

            CheckedIn = true;
        }
        catch (BusinessRuleException exception)
        {
            ErrorMessage = exception.Message;
        }

        return Page();
    }

    private bool IsValidToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        return _tokenService.TryValidateToken(
            token,
            out _,
            out DateTimeOffset expiresAtUtc) &&
            expiresAtUtc > DateTimeOffset.UtcNow;
    }
}
