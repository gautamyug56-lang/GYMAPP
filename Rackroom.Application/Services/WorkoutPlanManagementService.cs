using System.Text.Json;
using Rackroom.Application.Abstractions;
using Rackroom.Application.Exceptions;
using Rackroom.Domain.Entities;

namespace Rackroom.Application.Services;

public interface IWorkoutPlanManagementService
{
    Task<IReadOnlyList<WorkoutPlanTemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default);
    Task<WorkoutPlanTemplate> CreateTemplateAsync(string name, string? description, string planJson, CancellationToken cancellationToken = default);
    Task AssignTemplateAsync(Guid templateId, Guid memberId, CancellationToken cancellationToken = default);
}

public sealed class WorkoutPlanManagementService : IWorkoutPlanManagementService
{
    private readonly ICurrentGymContext _gymContext;
    private readonly IRackroomDataStore _store;

    public WorkoutPlanManagementService(ICurrentGymContext gymContext, IRackroomDataStore store)
    {
        _gymContext = gymContext;
        _store = store;
    }

    public Task<IReadOnlyList<WorkoutPlanTemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default) =>
        _store.GetWorkoutPlanTemplatesAsync(GetGymId(), cancellationToken);

    public async Task<WorkoutPlanTemplate> CreateTemplateAsync(
        string name,
        string? description,
        string planJson,
        CancellationToken cancellationToken = default)
    {
        Guid gymId = GetGymId();

        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleException("Workout plan name is required.");

        if (string.IsNullOrWhiteSpace(planJson))
            throw new BusinessRuleException("Workout plan content is required.");

        try
        {
            using JsonDocument document = JsonDocument.Parse(planJson);
            if (!document.RootElement.TryGetProperty("days", out JsonElement days) ||
                days.ValueKind != JsonValueKind.Array)
            {
                throw new BusinessRuleException("Workout plan must contain a days array.");
            }
        }
        catch (JsonException)
        {
            throw new BusinessRuleException("Workout plan must contain valid JSON.");
        }

        if (await _store.WorkoutPlanTemplateNameExistsAsync(gymId, name.Trim(), null, cancellationToken))
            throw new BusinessRuleException("A workout plan with this name already exists.");

        WorkoutPlanTemplate template = new()
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            PlanJson = planJson.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _store.AddWorkoutPlanTemplateAsync(template, cancellationToken);
        await _store.SaveChangesAsync(cancellationToken);
        return template;
    }

    public async Task AssignTemplateAsync(
        Guid templateId,
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        Guid gymId = GetGymId();

        if (!await _store.MemberExistsAsync(gymId, memberId, cancellationToken))
            throw new BusinessRuleException("Member was not found in the current gym.");

        WorkoutPlanTemplate template = await _store.GetWorkoutPlanTemplateAsync(
                gymId, templateId, cancellationToken)
            ?? throw new BusinessRuleException("Workout plan was not found.");

        if (!template.IsActive)
            throw new BusinessRuleException("An inactive workout plan cannot be assigned.");

        await _store.EndActiveWorkoutPlanAssignmentsAsync(
            gymId, memberId, DateTime.UtcNow, cancellationToken);

        await _store.AddWorkoutPlanAssignmentAsync(new WorkoutPlanAssignment
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            MemberId = memberId,
            WorkoutPlanTemplateId = templateId,
            AssignedAtUtc = DateTime.UtcNow,
            IsActive = true
        }, cancellationToken);

        await _store.SaveChangesAsync(cancellationToken);
    }

    private Guid GetGymId() =>
        _gymContext.GymId ??
        throw new BusinessRuleException("No gym is associated with the current user.");
}
