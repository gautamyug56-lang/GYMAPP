namespace Rackroom.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; }
    public Guid GymId { get; set; }
    public Guid MembershipId { get; set; }
    public Guid MemberId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAtUtc { get; set; }
    public PaymentMethod Method { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Gym? Gym { get; set; }
    public Membership? Membership { get; set; }
    public Member? Member { get; set; }
    public Invoice? Invoice { get; set; }
}
