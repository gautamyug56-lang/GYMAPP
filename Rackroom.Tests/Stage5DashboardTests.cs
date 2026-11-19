using Moq;
using Rackroom.Application.Abstractions;
using Rackroom.Application.Exceptions;
using Rackroom.Application.Services;

namespace Rackroom.Tests;

public class Stage5DashboardTests
{
    private static readonly Guid GymId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task Dashboard_builds_six_month_charts_and_preserves_zero_months()
    {
        var store = new Mock<IRackroomDataStore>();

        store.Setup(x => x.GetDashboardMetricsAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardMetricsData(
                3250.35m,
                4,
                3,
                1,
                12));

        store.Setup(x => x.GetRevenueByMonthAsync(
                GymId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new DashboardChartPoint(
                    DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM"),
                    3250.35m)
            ]);

        store.Setup(x => x.GetNewMembersByMonthAsync(
                GymId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        store.Setup(x => x.GetExpiringMembershipsAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                1,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardPageData<DashboardMembershipRow>([], 0, 1, 10));

        store.Setup(x => x.GetPaymentDeadlinesAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                1,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardPageData<DashboardMembershipRow>([], 0, 1, 10));

        store.Setup(x => x.GetTodaysAttendanceAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                1,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardPageData<DashboardAttendanceRow>([], 0, 1, 10));

        var gymContext = new Mock<ICurrentGymContext>();
        gymContext.Setup(x => x.GymId).Returns(GymId);

        var service = new AdminDashboardService(
            gymContext.Object,
            store.Object);

        AdminDashboardData result =
            await service.GetDashboardAsync(1, 1, 1);

        Assert.Equal(3250.35m, result.Metrics.MonthlyRevenue);
        Assert.Equal(6, result.RevenueChart.Count);
        Assert.Equal(6, result.NewMembersChart.Count);
        Assert.Equal(
            3250.35m,
            result.RevenueChart.Single(x => x.Value == 3250.35m).Value);
    }

    [Fact]
    public async Task Dashboard_rejects_invalid_page_numbers()
    {
        var store = new Mock<IRackroomDataStore>();
        var gymContext = new Mock<ICurrentGymContext>();
        gymContext.Setup(x => x.GymId).Returns(GymId);

        var service = new AdminDashboardService(
            gymContext.Object,
            store.Object);

        BusinessRuleException exception =
            await Assert.ThrowsAsync<BusinessRuleException>(
                () => service.GetDashboardAsync(0, 1, 1));

        Assert.Equal(
            "Dashboard page numbers must be at least 1.",
            exception.Message);
    }
}
