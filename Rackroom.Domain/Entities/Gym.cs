namespace Rackroom.Domain.Entities;

public class Gym
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PanNumber { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<Member> Members { get; set; } = new List<Member>();
    public ICollection<MembershipPlan> MembershipPlans { get; set; } = new List<MembershipPlan>();
    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    public ICollection<MembershipPause> MembershipPauses { get; set; } = new List<MembershipPause>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<WorkoutPlanTemplate> WorkoutPlanTemplates { get; set; } = new List<WorkoutPlanTemplate>();
    public ICollection<WorkoutPlanAssignment> WorkoutPlanAssignments { get; set; } = new List<WorkoutPlanAssignment>();
    public ICollection<BodyMeasurement> BodyMeasurements { get; set; } = new List<BodyMeasurement>();
    public ICollection<CancellationRecord> CancellationRecords { get; set; } = new List<CancellationRecord>();
    public ICollection<ChurnPrediction> ChurnPredictions { get; set; } = new List<ChurnPrediction>();
}
