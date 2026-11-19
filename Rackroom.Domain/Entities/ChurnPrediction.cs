namespace Rackroom.Domain.Entities;

public class ChurnPrediction
{
    public Guid Id { get; set; }
    public Guid GymId { get; set; }
    public Guid MemberId { get; set; }
    public DateTime ScoredAtUtc { get; set; }
    public double Probability { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
    public string? FeatureImportanceJson { get; set; }

    public Gym? Gym { get; set; }
    public Member? Member { get; set; }
}
