using Rackroom.Application.ML;

namespace Rackroom.Tests;

public class Stage7ChurnTests
{
    [Fact]
    public void Synthetic_dataset_is_deterministic_and_balanced()
    {
        var generator = new SyntheticChurnDatasetGenerator();

        IReadOnlyList<ChurnData> first = generator.Generate(1000, 42);
        IReadOnlyList<ChurnData> second = generator.Generate(1000, 42);

        Assert.Equal(first.Count, second.Count);
        Assert.Equal(
            first.Select(x => x.Label),
            second.Select(x => x.Label));

        int positives = first.Count(x => x.Label);
        int negatives = first.Count - positives;

        Assert.True(positives >= 300);
        Assert.True(negatives >= 300);
    }
}
