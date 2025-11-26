namespace AutodealerCrm.Apps.Views;

public class UserCallRecordsCreateDialog(IState<bool> isOpen, RefreshToken refreshToken, int? managerId) : ViewBase
{
    private record CallRecordCreateRequest
    {
        [Required]
        public int CustomerId { get; init; }

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
            var callRecordId = CreateCallRecord(factory, callRecord.Value, managerId);
            refreshToken.Refresh(callRecordId);
        }, [callRecord]);

        return callRecord
            .ToForm()
            .Builder(e => e.CustomerId, e => e.ToAsyncSelectInput(QueryCustomers(factory), LookupCustomer(factory), placeholder: "Select Customer"))
            .Builder(e => e.CallDirectionId, e => e.ToAsyncSelectInput(QueryCallDirections(factory), LookupCallDirection(factory), placeholder: "Select Call Direction"))
            .Builder(e => e.StartTime, e => e.ToDateTimeInput())
            .Builder(e => e.EndTime, e => e.ToDateTimeInput())
            .ToDialog(isOpen, title: "Create Call Record", submitTitle: "Create");
    }

    private int CreateCallRecord(AutodealerCrmContextFactory factory, CallRecordCreateRequest request, int? managerId)
    {
        using var db = factory.CreateDbContext();

        var callRecord = new CallRecord
        {
            CustomerId = request.CustomerId,
            CallDirectionId = request.CallDirectionId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Duration = request.Duration,
            RecordingUrl = request.RecordingUrl,
            ScriptScore = request.ScriptScore,
            Sentiment = request.Sentiment,
            ManagerId = managerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.CallRecords.Add(callRecord);
        db.SaveChanges();

        return callRecord.Id;
    }

    private static AsyncSelectQueryDelegate<int> QueryCustomers(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.Customers
                    .Where(e => e.FirstName.Contains(query) || e.LastName.Contains(query))
                    .Select(e => new { e.Id, Name = $"{e.FirstName} {e.LastName}" })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int>(e.Name, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int> LookupCustomer(AutodealerCrmContextFactory factory)
    {
        return async id =>
        {
            await using var db = factory.CreateDbContext();
            var customer = await db.Customers.FirstOrDefaultAsync(e => e.Id == id);
            if (customer == null) return null;
            return new Option<int>($"{customer.FirstName} {customer.LastName}", customer.Id);
        };
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