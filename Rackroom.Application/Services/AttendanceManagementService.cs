using Rackroom.Application.Abstractions;
using Rackroom.Application.Exceptions;
using Rackroom.Domain.Entities;

namespace Rackroom.Application.Services;

public interface IAttendanceManagementService
{
    Task<Attendance> CheckInWithQrAsync(
        Guid memberId,
        string token,
        DateTime checkedInAtUtc,
        CancellationToken cancellationToken = default);

    Task<Attendance> StaffCheckInAsync(
        Guid memberId,
        DateTime checkedInAtUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Attendance>> GetMemberHistoryAsync(
        Guid memberId,
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Attendance>> GetGymHistoryAsync(
        int take = 100,
        CancellationToken cancellationToken = default);

    Task<AttendanceStatistics> GetMemberStatisticsAsync(
        Guid memberId,
        CancellationToken cancellationToken = default);

    Task<AttendanceStatistics> GetGymStatisticsAsync(
        CancellationToken cancellationToken = default);
}

public sealed record AttendanceStatistics(
    int TotalCheckIns,
    int CurrentMonthCheckIns,
    int Last30DaysCheckIns,
    int DistinctDaysVisited);

public sealed class AttendanceManagementService : IAttendanceManagementService
{
    private readonly ICurrentGymContext _currentGymContext;
    private readonly IRackroomDataStore _dataStore;
    private readonly IAttendanceTokenService _tokenService;

    public AttendanceManagementService(
        ICurrentGymContext currentGymContext,
        IRackroomDataStore dataStore,
        IAttendanceTokenService tokenService)
    {
        _currentGymContext = currentGymContext;
        _dataStore = dataStore;
        _tokenService = tokenService;
    }

    public async Task<Attendance> CheckInWithQrAsync(
        Guid memberId,
        string token,
        DateTime checkedInAtUtc,
        CancellationToken cancellationToken = default)
    {
        Guid gymId = GetGymId();

        if (checkedInAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new BusinessRuleException("Check-in time must be UTC.");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new BusinessRuleException("The QR code token is required.");
        }

        if (!_tokenService.TryValidateToken(
                token,
                out Guid tokenGymId,
                out DateTimeOffset tokenExpiresAtUtc))
        {
            throw new BusinessRuleException("The QR code is invalid or expired.");
        }

        if (tokenGymId != gymId)
        {
            throw new BusinessRuleException("The QR code belongs to a different gym.");
        }

        if (tokenExpiresAtUtc <= new DateTimeOffset(checkedInAtUtc))
        {
            throw new BusinessRuleException("The QR code has expired.");
        }

        return await CreateValidatedAttendanceAsync(
            gymId,
            memberId,
            checkedInAtUtc,
            "QrCode",
            cancellationToken);
    }

    public Task<Attendance> StaffCheckInAsync(
        Guid memberId,
        DateTime checkedInAtUtc,
        CancellationToken cancellationToken = default)
    {
        Guid gymId = GetGymId();

        if (checkedInAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new BusinessRuleException("Check-in time must be UTC.");
        }

        return CreateValidatedAttendanceAsync(
            gymId,
            memberId,
            checkedInAtUtc,
            "StaffAssisted",
            cancellationToken);
    }

    public Task<IReadOnlyList<Attendance>> GetMemberHistoryAsync(
        Guid memberId,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (take is < 1 or > 200)
        {
            throw new BusinessRuleException("Attendance history size must be between 1 and 200.");
        }

        return _dataStore.GetMemberAttendanceHistoryAsync(
            GetGymId(),
            memberId,
            take,
            cancellationToken);
    }

    public Task<IReadOnlyList<Attendance>> GetGymHistoryAsync(
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        if (take is < 1 or > 500)
        {
            throw new BusinessRuleException("Attendance history size must be between 1 and 500.");
        }

        return _dataStore.GetGymAttendanceHistoryAsync(
            GetGymId(),
            take,
            cancellationToken);
    }

    public async Task<AttendanceStatistics> GetMemberStatisticsAsync(
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        DateTime nowUtc = DateTime.UtcNow;
        DateTime fromUtc = nowUtc.AddYears(-1);

        AttendanceStatisticsData data =
            await _dataStore.GetMemberAttendanceStatisticsAsync(
                GetGymId(),
                memberId,
                fromUtc,
                nowUtc.AddSeconds(1),
                cancellationToken);

        return MapStatistics(data);
    }

    public async Task<AttendanceStatistics> GetGymStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        DateTime nowUtc = DateTime.UtcNow;
        DateTime fromUtc = nowUtc.AddYears(-1);

        AttendanceStatisticsData data =
            await _dataStore.GetGymAttendanceStatisticsAsync(
                GetGymId(),
                fromUtc,
                nowUtc.AddSeconds(1),
                cancellationToken);

        return MapStatistics(data);
    }

    private async Task<Attendance> CreateValidatedAttendanceAsync(
        Guid gymId,
        Guid memberId,
        DateTime checkedInAtUtc,
        string source,
        CancellationToken cancellationToken)
    {
        Member member = await _dataStore.GetMemberAsync(
                gymId,
                memberId,
                cancellationToken)
            ?? throw new BusinessRuleException("Member was not found in the current gym.");

        if (!member.IsActive)
        {
            throw new BusinessRuleException("Inactive members cannot check in.");
        }

        _ = await _dataStore.GetActiveMembershipForMemberAsync(
                gymId,
                memberId,
                checkedInAtUtc,
                cancellationToken)
            ?? throw new BusinessRuleException(
                "The member does not have an active membership at the time of check-in.");

        Attendance attendance = new()
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            MemberId = memberId,
            CheckedInAtUtc = checkedInAtUtc,
            Source = source,
        };

        await _dataStore.AddAttendanceAsync(attendance, cancellationToken);
        await _dataStore.SaveChangesAsync(cancellationToken);

        return attendance;
    }

    private static AttendanceStatistics MapStatistics(AttendanceStatisticsData data)
    {
        return new AttendanceStatistics(
            data.TotalCheckIns,
            data.CurrentMonthCheckIns,
            data.Last30DaysCheckIns,
            data.DistinctDaysVisited);
    }

    private Guid GetGymId()
    {
        return _currentGymContext.GymId
            ?? throw new BusinessRuleException("No gym is associated with the current user.");
    }
}
