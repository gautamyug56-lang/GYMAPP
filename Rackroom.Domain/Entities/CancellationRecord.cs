namespace Rackroom.Domain.Entities;

public class CancellationRecord
{
    public Guid Id { get; set; }
    public Guid GymId { get; set; }
    public Guid MembershipId { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public DateTime EffectiveAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;

    public Gym? Gym { get; set; }
    public Membership? Membership { get; set; }
}
