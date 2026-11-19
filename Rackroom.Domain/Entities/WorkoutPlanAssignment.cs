namespace Rackroom.Domain.Entities;

public class WorkoutPlanAssignment
{
    public Guid Id { get; set; }
    public Guid GymId { get; set; }
    public Guid MemberId { get; set; }
    public Guid WorkoutPlanTemplateId { get; set; }
    public DateTime AssignedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;

    public Gym? Gym { get; set; }
    public Member? Member { get; set; }
    public WorkoutPlanTemplate? WorkoutPlanTemplate { get; set; }
}
