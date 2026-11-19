using Moq;
using Rackroom.Application.Abstractions;
using Rackroom.Application.Exceptions;
using Rackroom.Application.Services;
using Rackroom.Domain.Entities;

namespace Rackroom.Tests;

public class Stage4AttendanceTests
{
    private static readonly Guid GymId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static readonly Guid MemberId =
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static readonly DateTime CheckInTime =
        new(2026, 11, 19, 7, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Qr_check_in_requires_valid_token_and_active_membership()
    {
        var store = CreateStore();
        var tokenService = new Mock<IAttendanceTokenService>();

        Guid tokenGymId = GymId;
        DateTimeOffset tokenExpires = CheckInTime.AddMinutes(1);

        tokenService
            .Setup(x => x.TryValidateToken(
                "valid-token",
                out tokenGymId,
                out tokenExpires))
            .Returns(true);

        var service = CreateService(store, tokenService.Object);

        Attendance result = await service.CheckInWithQrAsync(
            MemberId,
            "valid-token",
            CheckInTime);

        Assert.Equal(MemberId, result.MemberId);
        Assert.Equal(GymId, result.GymId);
        Assert.Equal("QrCode", result.Source);

        store.Verify(
            x => x.AddAttendanceAsync(
                It.Is<Attendance>(a =>
                    a.MemberId == MemberId &&
                    a.Source == "QrCode"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        store.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Qr_check_in_rejects_a_token_from_another_gym()
    {
        var store = CreateStore();
        var tokenService = new Mock<IAttendanceTokenService>();

        Guid otherGymId = Guid.NewGuid();
        DateTimeOffset expires = new(2026, 11, 19, 8, 0, 0, TimeSpan.Zero);

        tokenService
            .Setup(x => x.TryValidateToken(
                "foreign-token",
                out otherGymId,
                out expires))
            .Returns(true);

        var service = CreateService(store, tokenService.Object);

        BusinessRuleException exception =
            await Assert.ThrowsAsync<BusinessRuleException>(
                () => service.CheckInWithQrAsync(
                    MemberId,
                    "foreign-token",
                    CheckInTime));

        Assert.Equal(
            "The QR code belongs to a different gym.",
            exception.Message);
    }

    [Fact]
    public async Task Qr_check_in_rejects_inactive_members()
    {
        var store = CreateStore();
        Member inactiveMember = CreateMember();
        inactiveMember.IsActive = false;

        store.Setup(x => x.GetMemberAsync(
                GymId,
                MemberId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveMember);

        var tokenService = CreateValidTokenService();
        var service = CreateService(store, tokenService.Object);

        BusinessRuleException exception =
            await Assert.ThrowsAsync<BusinessRuleException>(
                () => service.CheckInWithQrAsync(
                    MemberId,
                    "valid-token",
                    CheckInTime));

        Assert.Equal(
            "Inactive members cannot check in.",
            exception.Message);
    }

    [Fact]
    public async Task Qr_check_in_rejects_members_without_active_membership()
    {
        var store = CreateStore();
        store.Setup(x => x.GetActiveMembershipForMemberAsync(
                GymId,
                MemberId,
                CheckInTime,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Membership?)null);

        var tokenService = CreateValidTokenService();
        var service = CreateService(store, tokenService.Object);

        BusinessRuleException exception =
            await Assert.ThrowsAsync<BusinessRuleException>(
                () => service.CheckInWithQrAsync(
                    MemberId,
                    "valid-token",
                    CheckInTime));

        Assert.Equal(
            "The member does not have an active membership at the time of check-in.",
            exception.Message);
    }

    [Fact]
    public async Task Staff_assisted_check_in_uses_staff_source()
    {
        var store = CreateStore();
        var service = CreateService(
            store,
            Mock.Of<IAttendanceTokenService>());

        Attendance result = await service.StaffCheckInAsync(
            MemberId,
            CheckInTime);

        Assert.Equal("StaffAssisted", result.Source);

        store.Verify(
            x => x.AddAttendanceAsync(
                It.Is<Attendance>(a => a.Source == "StaffAssisted"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Attendance_history_size_is_validated()
    {
        var store = CreateStore();
        var service = CreateService(
            store,
            Mock.Of<IAttendanceTokenService>());

        BusinessRuleException exception =
            await Assert.ThrowsAsync<BusinessRuleException>(
                () => service.GetMemberHistoryAsync(MemberId, 0));

        Assert.Equal(
            "Attendance history size must be between 1 and 200.",
            exception.Message);
    }

    private static Mock<IRackroomDataStore> CreateStore()
    {
        var store = new Mock<IRackroomDataStore>();

        store.Setup(x => x.GetMemberAsync(
                GymId,
                MemberId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMember());

        store.Setup(x => x.GetActiveMembershipForMemberAsync(
                GymId,
                MemberId,
                CheckInTime,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Membership
            {
                Id = Guid.NewGuid(),
                GymId = GymId,
                MemberId = MemberId,
                StartDateUtc = CheckInTime.AddDays(-10),
                EndDateUtc = CheckInTime.AddDays(20),
                Status = MembershipStatus.Active
            });

        store.Setup(x => x.AddAttendanceAsync(
                It.IsAny<Attendance>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        store.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return store;
    }

    private static Mock<IAttendanceTokenService> CreateValidTokenService()
    {
        var tokenService = new Mock<IAttendanceTokenService>();
        Guid tokenGymId = GymId;
        DateTimeOffset expires = CheckInTime.AddMinutes(1);

        tokenService
            .Setup(x => x.TryValidateToken(
                "valid-token",
                out tokenGymId,
                out expires))
            .Returns(true);

        return tokenService;
    }

    private static AttendanceManagementService CreateService(
        Mock<IRackroomDataStore> store,
        IAttendanceTokenService tokenService)
    {
        var gymContext = new Mock<ICurrentGymContext>();
        gymContext.Setup(x => x.GymId).Returns(GymId);

        return new AttendanceManagementService(
            gymContext.Object,
            store.Object,
            tokenService);
    }

    private static Member CreateMember()
    {
        return new Member
        {
            Id = MemberId,
            GymId = GymId,
            FirstName = "Demo",
            LastName = "Member",
            Email = "member@example.com",
            IsActive = true
        };
    }
}
