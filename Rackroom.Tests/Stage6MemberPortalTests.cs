using Moq;
using Rackroom.Application.Abstractions;
using Rackroom.Application.Exceptions;
using Rackroom.Application.Services;
using Rackroom.Domain.Entities;

namespace Rackroom.Tests;

public class Stage6MemberPortalTests
{
    private static readonly Guid GymId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid MemberId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task Monthly_measurement_rejects_second_entry_in_same_calendar_month()
    {
        var store = new Mock<IRackroomDataStore>();
        store.Setup(x => x.HasMeasurementInCalendarMonthAsync(GymId, MemberId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var context = new Mock<ICurrentGymContext>();
        context.Setup(x => x.GymId).Returns(GymId);
        context.Setup(x => x.MemberId).Returns(MemberId);
        var service = new MemberPortalService(context.Object, store.Object);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.RecordMonthlyMeasurementAsync(70m, 175m, DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc)));
    }

    [Fact]
    public async Task Monthly_measurement_rounds_values_and_saves()
    {
        var store = new Mock<IRackroomDataStore>();
        store.Setup(x => x.HasMeasurementInCalendarMonthAsync(GymId, MemberId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var context = new Mock<ICurrentGymContext>();
        context.Setup(x => x.GymId).Returns(GymId);
        context.Setup(x => x.MemberId).Returns(MemberId);
        var service = new MemberPortalService(context.Object, store.Object);
        BodyMeasurement result = await service.RecordMonthlyMeasurementAsync(72.567m, 175.456m, DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc));
        Assert.Equal(72.57m, result.WeightKg);
        Assert.Equal(175.46m, result.HeightCm);
        store.Verify(x => x.AddBodyMeasurementAsync(result, It.IsAny<CancellationToken>()), Times.Once);
        store.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Workout_assignment_ends_previous_active_assignment()
    {
        var template = new WorkoutPlanTemplate { Id = Guid.NewGuid(), GymId = GymId, Name = "PPL", PlanJson = "{\"days\":[]}", IsActive = true };
        var store = new Mock<IRackroomDataStore>();
        store.Setup(x => x.MemberExistsAsync(GymId, MemberId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        store.Setup(x => x.GetWorkoutPlanTemplateAsync(GymId, template.Id, It.IsAny<CancellationToken>())).ReturnsAsync(template);
        var context = new Mock<ICurrentGymContext>();
        context.Setup(x => x.GymId).Returns(GymId);
        var service = new WorkoutPlanManagementService(context.Object, store.Object);
        await service.AssignTemplateAsync(template.Id, MemberId);
        store.Verify(x => x.EndActiveWorkoutPlanAssignmentsAsync(GymId, MemberId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        store.Verify(x => x.AddWorkoutPlanAssignmentAsync(It.IsAny<WorkoutPlanAssignment>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}