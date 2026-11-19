namespace Rackroom.Domain.Entities;

public class Member
{
    public Guid Id { get; set; }
    public Guid GymId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public Gym? Gym { get; set; }
}
