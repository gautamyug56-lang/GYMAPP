using Moq;
using Rackroom.Application.Abstractions;
using Rackroom.Application.Exceptions;
using Rackroom.Application.Services;
using Rackroom.Domain.Entities;
using Xunit;

namespace Rackroom.Tests;

public class Stage2ServiceTests
{
    private static readonly Guid GymId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid MemberId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid PlanId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid MembershipId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid PauseId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid ApproverId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

    private static Mock<ICurrentGymContext> CreateGymContext()
    {
        var mock = new Mock<ICurrentGymContext>();
        mock.SetupGet(x => x.GymId).Returns(GymId);
        return mock;
    }

    [Fact]
    public async Task Add_member_normalizes_values_and_creates_active_member()
    {
        var store = new Mock<IRackroomDataStore>();
        store.Setup(x => x.MemberEmailExistsAsync(
                GymId,
                "member@example.com",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new MemberManagementService(
            CreateGymContext().Object,
            store.Object);

        Member result = await service.AddMemberAsync(
            "  Jane ",
            " Doe ",
            " MEMBER@EXAMPLE.COM ");

        Assert.Equal("Jane", result.FirstName);
        Assert.Equal("Doe", result.LastName);
        Assert.Equal("member@example.com", result.Email);
        Assert.True(result.IsActive);
        Assert.Equal(GymId, result.GymId);

        store.Verify(x => x.AddMemberAsync(
            It.Is<Member>(m => m.Email == "member@example.com" && m.IsActive),
            It.IsAny<CancellationToken>()), Times.Once);
        store.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Add_member_rejects_duplicate_email()
    {
        var store = new Mock<IRackroomDataStore>();
        store.Setup(x => x.MemberEmailExistsAsync(
                GymId,
                "member@example.com",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new MemberManagementService(
            CreateGymContext().Object,
            store.Object);

        BusinessRuleException exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.AddMemberAsync("Jane", "Doe", "member@example.com"));

        Assert.Equal(
            "A member with this email already exists in this gym.",
            exception.Message);
    }

    [Fact]
    public async Task Deactivate_member_sets_is_active_to_false()
    {
        Member member = CreateMember();
        var store = new Mock<IRackroomDataStore>();

        store.Setup(x => x.GetMemberAsync(
                GymId,
                MemberId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var service = new MemberManagementService(
            CreateGymContext().Object,
            store.Object);

        await service.DeactivateMemberAsync(MemberId);

        Assert.False(member.IsActive);
        store.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_plan_rejects_invalid_duration()
    {
        var store = new Mock<IRackroomDataStore>();
        var service = new MembershipManagementService(
            CreateGymContext().Object,
            store.Object);

        BusinessRuleException exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.CreatePlanAsync("Six Months", 6, 15000m));

        Assert.Equal(
            "Plan duration must be 1 month, 3 months, or 12 months.",
            exception.Message);
    }

    [Fact]
    public async Task Assign_membership_calculates_expiry_from_plan_duration()
    {
        Member member = CreateMember();
        MembershipPlan plan = new()
        {
            Id = PlanId,
            GymId = GymId,
            Name = "3 Months",
            DurationMonths = 3,
            Price = 8000m,
            IsActive = true
        };

        var store = new Mock<IRackroomDataStore>();
        store.Setup(x => x.GetMemberAsync(
                GymId,
                MemberId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        store.Setup(x => x.GetMembershipPlanAsync(
                GymId,
                PlanId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        store.Setup(x => x.HasActiveMembershipAsync(
                GymId,
                MemberId,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new MembershipManagementService(
            CreateGymContext().Object,
            store.Object);

        DateTime start = new(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc);

        Membership result = await service.AssignMembershipAsync(
            MemberId,
            PlanId,
            start);

        Assert.Equal(start.AddMonths(3), result.EndDateUtc);
        Assert.Equal(MembershipStatus.Active, result.Status);
    }

    [Fact]
    public async Task Request_pause_rejects_more_than_30_days()
    {
        Membership membership = CreateMembership(
            new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var store = new Mock<IRackroomDataStore>();
        store.Setup(x => x.GetMembershipAsync(
                GymId,
                MembershipId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);

        var service = new MembershipManagementService(
            CreateGymContext().Object,
            store.Object);

        BusinessRuleException exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.RequestPauseAsync(
                MembershipId,
                new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                "Travel"));

        Assert.Equal("A membership pause cannot exceed 30 days.", exception.Message);
    }

    [Fact]
    public async Task Approve_pause_extends_membership_by_paused_days()
    {
        DateTime originalEnd = new(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Membership membership = CreateMembership(
            new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            originalEnd);

        MembershipPause pause = new()
        {
            Id = PauseId,
            GymId = GymId,
            MembershipId = MembershipId,
            StartDateUtc = new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2026, 11, 19, 0, 0, 0, DateTimeKind.Utc),
            PausedDays = 10,
            Status = PauseStatus.Pending
        };

        var store = new Mock<IRackroomDataStore>();
        store.Setup(x => x.GetMembershipPauseAsync(
                GymId,
                PauseId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pause);
        store.Setup(x => x.GetMembershipAsync(
                GymId,
                MembershipId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);
        store.Setup(x => x.CountApprovedPausesInCalendarYearAsync(
                GymId,
                MembershipId,
                2026,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var service = new MembershipManagementService(
            CreateGymContext().Object,
            store.Object);

        await service.ApprovePauseAsync(
            PauseId,
            ApproverId,
            "Staff",
            new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(PauseStatus.Approved, pause.Status);
        Assert.Equal(ApproverId, pause.ApprovedByUserId);
        Assert.Equal(originalEnd.AddDays(10), membership.EndDateUtc);
        store.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Approve_pause_rejects_third_pause_in_calendar_year()
    {
        Membership membership = CreateMembership(
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        MembershipPause pause = new()
        {
            Id = PauseId,
            GymId = GymId,
            MembershipId = MembershipId,
            StartDateUtc = new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2026, 11, 19, 0, 0, 0, DateTimeKind.Utc),
            PausedDays = 10,
            Status = PauseStatus.Pending
        };

        var store = new Mock<IRackroomDataStore>();
        store.Setup(x => x.GetMembershipPauseAsync(
                GymId,
                PauseId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pause);
        store.Setup(x => x.GetMembershipAsync(
                GymId,
                MembershipId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);
        store.Setup(x => x.CountApprovedPausesInCalendarYearAsync(
                GymId,
                MembershipId,
                2026,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var service = new MembershipManagementService(
            CreateGymContext().Object,
            store.Object);

        BusinessRuleException exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.ApprovePauseAsync(
                PauseId,
                ApproverId,
                "Staff",
                new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc)));

        Assert.Equal(
            "A member can have at most two approved pauses per calendar year.",
            exception.Message);
        Assert.Equal(PauseStatus.Pending, pause.Status);
    }

    [Fact]
    public async Task Approve_pause_requires_staff_or_admin()
    {
        MembershipPause pause = new()
        {
            Id = PauseId,
            GymId = GymId,
            MembershipId = MembershipId,
            StartDateUtc = new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2026, 11, 19, 0, 0, 0, DateTimeKind.Utc),
            PausedDays = 10,
            Status = PauseStatus.Pending
        };

        var store = new Mock<IRackroomDataStore>();
        store.Setup(x => x.GetMembershipPauseAsync(
                GymId,
                PauseId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pause);

        var service = new MembershipManagementService(
            CreateGymContext().Object,
            store.Object);

        BusinessRuleException exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.ApprovePauseAsync(
                PauseId,
                ApproverId,
                "Member",
                new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc)));

        Assert.Equal(
            "Only staff or an admin can approve a membership pause.",
            exception.Message);
    }

    [Fact]
    public async Task Cancel_membership_effective_date_is_end_of_paid_period()
    {
        DateTime endDate = new(2027, 2, 20, 0, 0, 0, DateTimeKind.Utc);
        Membership membership = CreateMembership(
            new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc),
            endDate);

        var store = new Mock<IRackroomDataStore>();
        store.Setup(x => x.GetMembershipAsync(
                GymId,
                MembershipId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);
        store.Setup(x => x.CancellationExistsAsync(
                GymId,
                MembershipId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        CancellationRecord? createdRecord = null;
        store.Setup(x => x.AddCancellationRecordAsync(
                It.IsAny<CancellationRecord>(),
                It.IsAny<CancellationToken>()))
            .Callback<CancellationRecord, CancellationToken>((record, _) => createdRecord = record)
            .Returns(Task.CompletedTask);

        var service = new MembershipManagementService(
            CreateGymContext().Object,
            store.Object);

        await service.CancelMembershipAsync(
            MembershipId,
            "Moving away",
            new DateTime(2026, 11, 25, 0, 0, 0, DateTimeKind.Utc));

        Assert.NotNull(createdRecord);
        Assert.Equal(endDate, createdRecord!.EffectiveAtUtc);
        Assert.Equal("Moving away", createdRecord.Reason);
        Assert.Equal(MembershipStatus.Active, membership.Status);
    }

    private static Member CreateMember() => new()
    {
        Id = MemberId,
        GymId = GymId,
        FirstName = "Jane",
        LastName = "Doe",
        Email = "jane@example.com",
        IsActive = true
    };

    private static Membership CreateMembership(
        DateTime startDateUtc,
        DateTime endDateUtc) => new()
    {
        Id = MembershipId,
        GymId = GymId,
        MemberId = MemberId,
        MembershipPlanId = PlanId,
        StartDateUtc = startDateUtc,
        EndDateUtc = endDateUtc,
        Status = MembershipStatus.Active
    };
}
