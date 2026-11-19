namespace Rackroom.Domain.Entities;

public class MembershipPause
{
    public Guid Id { get; set; }
    public Guid GymId { get; set; }
    public Guid MembershipId { get; set; }
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public int PausedDays { get; set; }
    public PauseStatus Status { get; set; } = PauseStatus.Pending;
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Gym? Gym { get; set; }
    public Membership? Membership { get; set; }
}
