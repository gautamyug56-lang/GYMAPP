namespace Rackroom.Domain.Entities;

public class Gym
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PanNumber { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public ICollection<Member> Members { get; set; } = new List<Member>();
}
