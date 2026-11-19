namespace Rackroom.Domain.Entities;

public class Attendance
{
    public Guid Id { get; set; }
    public Guid GymId { get; set; }
    public Guid MemberId { get; set; }
    public DateTime CheckedInAtUtc { get; set; }
    public string Source { get; set; } = "QrCode";

    public Gym? Gym { get; set; }
    public Member? Member { get; set; }
}
