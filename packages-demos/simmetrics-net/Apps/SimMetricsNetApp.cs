namespace SimMetricsNetExample;

[App(icon: Icons.SpellCheck)]
public class SimMetricsNetApp : ViewBase
{
    public override object? Build()
    {
        var inputString = UseState(string.Empty);
        var inputMetric = UseState(SimMetricType.Levenstein);
        var shortDescription = UseState(string.Empty);
        var longDescription = UseState(string.Empty);

        // Using Bogus to generate a list of random names
        var nameList = UseState(Enumerable.Range(1, 10).Select(_ => new NameSimilarity(new Faker().Name.FullName(), 0.0)).ToList());

        // Define the action to compute the metric calculation based on the input
        Action computeMetricInput = () =>
        {
            var metric = MetricsFactory[inputMetric.Value];

            shortDescription.Set(metric.ShortDescriptionString);
            longDescription.Set(metric.LongDescriptionString);

            var results = nameList.Value.Select(n => n with { Score = metric.GetSimilarity(inputString.Value, n.Name) })
                .OrderByDescending(r => r.Score)
                .ToList();

            nameList.Set(results);
        };

        // Hook to rerender when inputs change
        UseEffect(computeMetricInput, inputString, inputMetric);

        return Layout.Vertical()
            | new Card(Layout.Horizontal()
                    | new TextInput(inputString).Placeholder("Input a name here...")
                    | inputMetric.ToSelectInput(typeof(SimMetricType).ToOptions())
                ).Description("Input a string and then select the SimMetrics>net function to compute")
            | (longDescription.Value != string.Empty ? Text.Muted(longDescription) : null)
            | nameList.Value.ToTable().Header(x => x.Score, shortDescription.Value);
    }

    internal record NameSimilarity(string Name, double Score);
    internal static readonly Dictionary<SimMetricType, AbstractStringMetric> MetricsFactory = new()
    {
        // Edit-based metrics
        [SimMetricType.Levenstein] = new Levenstein(),
        [SimMetricType.NeedlemanWunch] = new NeedlemanWunch(),
        [SimMetricType.SmithWaterman] = new SmithWaterman(),
        [SimMetricType.SmithWatermanGotoh] = new SmithWatermanGotoh(),
        [SimMetricType.SmithWatermanGotohWindowedAffine] = new SmithWatermanGotohWindowedAffine(),

        // Token-based metrics
        [SimMetricType.Jaro] = new Jaro(),
        [SimMetricType.JaroWinkler] = new JaroWinkler(),
        [SimMetricType.ChapmanLengthDeviation] = new ChapmanLengthDeviation(),
        [SimMetricType.ChapmanMeanLength] = new ChapmanMeanLength(),

        // Q-gram and block metrics
        [SimMetricType.QGramsDistance] = new QGramsDistance(),
        [SimMetricType.BlockDistance] = new BlockDistance(),

        // Vector space metrics
        [SimMetricType.CosineSimilarity] = new CosineSimilarity(),
        [SimMetricType.DiceSimilarity] = new DiceSimilarity(),
        [SimMetricType.EuclideanDistance] = new EuclideanDistance(),
        [SimMetricType.JaccardSimilarity] = new JaccardSimilarity(),
        [SimMetricType.MatchingCoefficient] = new MatchingCoefficient(),
        [SimMetricType.OverlapCoefficient] = new OverlapCoefficient(),

        // Additional metrics
        [SimMetricType.MongeElkan] = new MongeElkan(),
    };
}
