namespace Rackroom.Domain.Entities;

public class WorkoutPlanTemplate
{
    public Guid Id { get; set; }
    public Guid GymId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string PlanJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }

    public Gym? Gym { get; set; }
    public ICollection<WorkoutPlanAssignment> Assignments { get; set; } = new List<WorkoutPlanAssignment>();
}
