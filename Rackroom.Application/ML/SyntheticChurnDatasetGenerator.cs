namespace Rackroom.Application.ML;

public sealed class SyntheticChurnDatasetGenerator
{
    private const int DefaultRowCount = 3000;

    public IReadOnlyList<ChurnData> Generate(int rowCount = DefaultRowCount, int seed = 20261119)
    {
        if (rowCount < 100)
            throw new ArgumentOutOfRangeException(nameof(rowCount), "At least 100 rows are required.");

        Random random = new(seed);
        var rows = new List<ChurnData>(rowCount);

        for (int i = 0; i < rowCount; i++)
        {
            float visitsPerWeek = MathF.Round((float)(random.NextDouble() * 6.0), 2);
            float recentVisits = MathF.Round((float)(random.NextDouble() * 18.0), 2);
            float attendanceDecline = MathF.Round((float)(random.NextDouble() * 1.0), 3);
            float daysSinceLastVisit = random.Next(0, 46);
            float membershipLength = random.Next(1, 25);
            float membershipType = new[] { 1f, 3f, 12f }[random.Next(3)];
            float paymentHistory = MathF.Round((float)(random.NextDouble() * 12.0), 2);
            float pauseCount = random.Next(0, 3);

            double risk =
                -1.7
                - 0.55 * visitsPerWeek
                - 0.08 * recentVisits
                + 2.2 * attendanceDecline
                + 0.075 * daysSinceLastVisit
                - 0.025 * membershipLength
                - 0.08 * paymentHistory
                + 0.22 * pauseCount;

            if (membershipType == 1f)
                risk += 0.25;

            double probability = 1.0 / (1.0 + Math.Exp(-risk));
            bool label = random.NextDouble() < probability;

            rows.Add(new ChurnData
            {
                VisitsPerWeek = visitsPerWeek,
                RecentVisits = recentVisits,
                AttendanceDecline = attendanceDecline,
                DaysSinceLastVisit = daysSinceLastVisit,
                MembershipLengthMonths = membershipLength,
                MembershipTypeMonths = membershipType,
                PaymentHistory = paymentHistory,
                PauseCount = pauseCount,
                Label = label
            });
        }

        // Keep the synthetic dataset approximately balanced so the evaluation
        // does not become dominated by the majority class.
        int positives = rows.Count(x => x.Label);
        int negatives = rows.Count - positives;

        if (positives < rowCount * 0.30 || negatives < rowCount * 0.30)
        {
            return Generate(rowCount, seed + 1);
        }

        return rows;
    }

    public async Task<string> WriteCsvAsync(
        string path,
        int rowCount = DefaultRowCount,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        IReadOnlyList<ChurnData> rows = Generate(rowCount);

        await using StreamWriter writer = new(path, false);
        await writer.WriteLineAsync(
            "VisitsPerWeek,RecentVisits,AttendanceDecline,DaysSinceLastVisit,MembershipLengthMonths,MembershipTypeMonths,PaymentHistory,PauseCount,Label");

        foreach (ChurnData row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await writer.WriteLineAsync(
                $"{row.VisitsPerWeek},{row.RecentVisits},{row.AttendanceDecline},{row.DaysSinceLastVisit},{row.MembershipLengthMonths},{row.MembershipTypeMonths},{row.PaymentHistory},{row.PauseCount},{row.Label}");
        }

        return path;
    }
}
