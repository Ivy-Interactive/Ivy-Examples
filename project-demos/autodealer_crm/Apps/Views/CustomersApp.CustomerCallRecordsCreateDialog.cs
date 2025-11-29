namespace AutodealerCrm.Apps.Views;

public class CustomerCallRecordsCreateDialog(IState<bool> isOpen, RefreshToken refreshToken, int customerId) : ViewBase
{
    private record CallRecordCreateRequest
    {
        [Required]
        public int CallDirectionId { get; init; }

        [Required]
        public DateTime StartTime { get; init; }

        [Required]
        public DateTime EndTime { get; init; }

        public int? Duration { get; init; }

        public string? RecordingUrl { get; init; }

        public string? ScriptScore { get; init; }

        public string? Sentiment { get; init; }
    }

    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var callRecord = UseState(() => new CallRecordCreateRequest());

        UseEffect(() =>
        {
            var callRecordId = CreateCallRecord(factory, callRecord.Value);
            refreshToken.Refresh(callRecordId);
        }, [callRecord]);

        return callRecord
            .ToForm()
            .Builder(e => e.CallDirectionId, e => e.ToAsyncSelectInput(QueryCallDirections(factory), LookupCallDirection(factory), placeholder: "Select Call Direction"))
            .Builder(e => e.StartTime, e => e.ToDateTimeInput())
            .Builder(e => e.EndTime, e => e.ToDateTimeInput())
            .Builder(e => e.Duration, e => e.ToNumberInput())
            .Builder(e => e.RecordingUrl, e => e.ToUrlInput())
            .Builder(e => e.ScriptScore, e => e.ToTextInput())
            .Builder(e => e.Sentiment, e => e.ToTextInput())
            .ToDialog(isOpen, title: "Create Call Record", submitTitle: "Create");
    }

    private int CreateCallRecord(AutodealerCrmContextFactory factory, CallRecordCreateRequest request)
    {
        using var db = factory.CreateDbContext();

        var callRecord = new CallRecord
        {
            CustomerId = customerId,
            CallDirectionId = request.CallDirectionId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Duration = request.Duration,
            RecordingUrl = request.RecordingUrl,
            ScriptScore = request.ScriptScore,
            Sentiment = request.Sentiment,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.CallRecords.Add(callRecord);
        db.SaveChanges();

        return callRecord.Id;
    }

    private static AsyncSelectQueryDelegate<int> QueryCallDirections(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.CallDirections
                    .Where(e => e.DescriptionText.Contains(query))
                    .Select(e => new { e.Id, e.DescriptionText })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int>(e.DescriptionText, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int> LookupCallDirection(AutodealerCrmContextFactory factory)
    {
        return async id =>
        {
            await using var db = factory.CreateDbContext();
            var callDirection = await db.CallDirections.FirstOrDefaultAsync(e => e.Id == id);
            if (callDirection == null) return null;
            return new Option<int>(callDirection.DescriptionText, callDirection.Id);
        };
    }
}