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
    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<WorkoutPlanAssignment> WorkoutPlanAssignments { get; set; } = new List<WorkoutPlanAssignment>();
    public ICollection<BodyMeasurement> BodyMeasurements { get; set; } = new List<BodyMeasurement>();
    public ICollection<ChurnPrediction> ChurnPredictions { get; set; } = new List<ChurnPrediction>();
}
