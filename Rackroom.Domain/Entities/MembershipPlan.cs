namespace Rackroom.Domain.Entities;

public class MembershipPlan
{
    public Guid Id { get; set; }
    public Guid GymId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DurationMonths { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }

    public Gym? Gym { get; set; }
    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
}
