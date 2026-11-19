namespace Rackroom.Domain.Entities;

public class Membership
{
    public Guid Id { get; set; }
    public Guid GymId { get; set; }
    public Guid MemberId { get; set; }
    public Guid MembershipPlanId { get; set; }
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public MembershipStatus Status { get; set; } = MembershipStatus.Active;
    public DateTime? LastPaidAtUtc { get; set; }
    public DateTime? ExpiryReminderSentAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Gym? Gym { get; set; }
    public Member? Member { get; set; }
    public MembershipPlan? MembershipPlan { get; set; }
    public ICollection<MembershipPause> Pauses { get; set; } = new List<MembershipPause>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public CancellationRecord? CancellationRecord { get; set; }
}
