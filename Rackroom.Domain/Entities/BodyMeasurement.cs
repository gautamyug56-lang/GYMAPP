namespace Rackroom.Domain.Entities;

public class BodyMeasurement
{
    public Guid Id { get; set; }
    public Guid GymId { get; set; }
    public Guid MemberId { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public decimal WeightKg { get; set; }
    public decimal HeightCm { get; set; }

    public Gym? Gym { get; set; }
    public Member? Member { get; set; }
}
