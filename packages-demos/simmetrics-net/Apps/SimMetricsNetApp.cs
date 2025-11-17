namespace SimMetricsNetExample;

[App(icon: Icons.SpellCheck, title: "SimMetrics.Net")]
public class SimMetricsNetApp : ViewBase
{
    public override object? Build()
    {
        List<NameSimilarity> CreateInitialNameList() =>
            Enumerable.Range(1, 10)
                .Select(_ => new NameSimilarity(new Faker().Name.FullName(), 0.0))
                .ToList();

        var inputString = UseState(string.Empty);
        var inputMetric = UseState<SimMetricType?>(() => null);
        var shortDescription = UseState(string.Empty);
        var longDescription = UseState(string.Empty);

        // Using Bogus to generate a list of random names
        var nameList = UseState(CreateInitialNameList());

        // Hook to rerender when inputs change
        UseEffect(() =>
        {
            if (string.IsNullOrWhiteSpace(inputString.Value) || inputMetric.Value is not SimMetricType metricType)
            {
                shortDescription.Set(string.Empty);
                longDescription.Set(string.Empty);
                return;
            }

            var metric = MetricsFactory[metricType];

            shortDescription.Set(metric.ShortDescriptionString);
            longDescription.Set(metric.LongDescriptionString);

            var results = nameList.Value.Select(n => n with { Score = metric.GetSimilarity(inputString.Value, n.Name) })
                .OrderByDescending(r => r.Score)
                .ToList();

            nameList.Set(results);
        }, inputString, inputMetric);

        var hasInput = !string.IsNullOrWhiteSpace(inputString.Value);
        var hasMetric = inputMetric.Value is SimMetricType;
        var hasResults = hasInput && hasMetric;
        var inputError = hasInput ? null : "Name is required.";
        var metricError = hasMetric ? null : "Select a similarity metric.";
        var tableRows = nameList.Value
            .Select(n => new
            {
                n.Name,
                Score = n.Score.ToString("P1")
            })
            .ToList();

        return Layout.Horizontal()
            | new Card(Layout.Vertical()
                    | Text.H3("Similarity Setup")
                    | Text.Muted("Compare custom input against randomly generated names using configurable string similarity algorithms from SimMetrics.Net.")
                    | new TextInput(inputString)
                        .Placeholder("Input a name here...")
                        .Invalid(inputError)
                        .WithField()
                        .Label("Name")
                    | inputMetric.ToSelectInput(typeof(SimMetricType).ToOptions())
                        .Placeholder("Select a metric...")
                        .Invalid(metricError)
                        .WithField()
                        .Label("Metric")
                    | new Spacer().Height(Size.Units(10))
                    | Text.Small("This demo uses SimMetrics.Net library to calculate similarity scores between names.")
                    | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [SimMetrics.Net](https://github.com/StefH/SimMetrics.Net)")
                ).Height(Size.Fit().Min(Size.Full()))
            | new Card(Layout.Vertical()
                    | Text.H3(shortDescription.Value != string.Empty ? shortDescription.Value : "Similarity Results")
                    | Text.Muted(longDescription.Value != string.Empty
                        ? longDescription.Value
                        : "Enter a name and metric on the left to calculate similarities against the sample names.")
                    | (hasResults
                        ? tableRows.ToTable().Header(x => x.Score, shortDescription.Value).Width(Size.Full())
                        : null)
                ).Height(Size.Fit().Min(Size.Full()));
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
