using System.Text;
using System.Text.Json;

namespace ColdCallTracker.Apps;

/*
 * EXAMPLE API RESPONSE DATA FROM EXA.AI WEBSETS
 *
 * GET /websets/v0/websets/{id}/items
 *
 * Response structure:
 * {
 *   "data": [
 *     {
 *       "id": "item-id-123",
 *       "properties": {
 *         "url": "https://example.com",
 *         "description": "Company description text",
 *         "company": {
 *           "name": "Company Name AB"
 *         },
 *         "evaluations": [
 *           {
 *             "description": "criteria description",
 *             "satisfied": "yes",
 *             "reasoning": "explanation text"
 *           }
 *         ]
 *       },
 *       "enrichments": [
 *         {
 *           "description": "Contact Email/Form",
 *           "value": "contact@example.com",
 *           "format": "text"
 *         },
 *         {
 *           "description": "LinkedIn URL",
 *           "value": "https://linkedin.com/company/example",
 *           "format": "url"
 *         },
 *         {
 *           "description": "Phone Number",
 *           "value": "+46 18 123 456",
 *           "format": "text"
 *         },
 *         {
 *           "description": "Services Summary",
 *           "value": "Accounting and bookkeeping services...",
 *           "format": "text"
 *         }
 *       ]
 *     }
 *   ],
 *   "pagination": {
 *     "total": 60,
 *     "limit": 100,
 *     "offset": 0
 *   }
 * }
 */

/// <summary>
/// Lead generation app that fetches data from Exa.ai API and displays it in a DataTable.
/// </summary>
public record LeadRecord(
    string Url,
    string? Title,
    string? ContactEmail,
    string? LinkedInUrl,
    string? PhoneNumber,
    string? ServicesSummary,
    double? Score
);

[App(icon: Icons.Users, title: "Lead Generator")]
public class DataTableApp : ViewBase
{
    private const string ExaApiKey = "your-api-key-here";
    private const string ExaApiUrl = "https://api.exa.ai/websets/v0/websets/";

    private const string ExampleResponseData = @"{
  ""data"": [
    {
      ""id"": ""item-id-123"",
      ""properties"": {
        ""url"": ""https://example.com"",
        ""description"": ""Company description text"",
        ""company"": {
          ""name"": ""Company Name AB""
        },
        ""evaluations"": [
          {
            ""description"": ""criteria description"",
            ""satisfied"": ""yes"",
            ""reasoning"": ""explanation text""
          }
        ]
      },
      ""enrichments"": [
        {
          ""description"": ""Contact Email/Form"",
          ""value"": ""contact@example.com"",
          ""format"": ""text""
        },
        {
          ""description"": ""LinkedIn URL"",
          ""value"": ""https://linkedin.com/company/example"",
          ""format"": ""url""
        },
        {
          ""description"": ""Phone Number"",
          ""value"": ""+46 18 123 456"",
          ""format"": ""text""
        },
        {
          ""description"": ""Services Summary"",
          ""value"": ""Accounting and bookkeeping services..."",
          ""format"": ""text""
        }
      ]
    }
  ],
  ""pagination"": {
    ""total"": 60,
    ""limit"": 100,
    ""offset"": 0
  }
}";

    public override object? Build()
    {
        // State to hold the leads data
        var leadsState = this.UseState<List<LeadRecord>>(() => new List<LeadRecord>());
        var loadingState = this.UseState(() => false);
        var errorState = this.UseState<string?>(() => null);

        // State for available websets
        var websetsState = this.UseState<List<(string Id, string Title, DateTime CreatedAt)>>(() => new List<(string, string, DateTime)>());
        var selectedWebsetId = this.UseState<string?>(() => null);
        var loadingWebsets = this.UseState(() => false);

        // State for showing API response data
        var showExampleData = this.UseState(() => false);
        var apiResponseData = this.UseState<string?>(() => null);

        // Handler for listing available websets
        var listWebsets = new Func<ValueTask>(async () =>
        {
            var listId = Guid.NewGuid();
            Console.WriteLine($"[{listId}] ===== LISTING WEBSETS =====");

            loadingWebsets.Set(_ => true);

            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(2);
                httpClient.DefaultRequestHeaders.Add("accept", "application/json");
                httpClient.DefaultRequestHeaders.Add("x-api-key", ExaApiKey);

                Console.WriteLine($"[{listId}] Fetching websets list...");
                var response = await httpClient.GetAsync(ExaApiUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[{listId}] ERROR: {response.StatusCode}");
                    Console.WriteLine($"[{listId}] Response: {responseContent}");
                    loadingWebsets.Set(_ => false);
                    return;
                }

                Console.WriteLine($"[{listId}] Parsing websets...");
                var jsonDoc = JsonDocument.Parse(responseContent);
                var websets = new List<(string, string, DateTime)>();

                if (jsonDoc.RootElement.TryGetProperty("data", out var data))
                {
                    foreach (var webset in data.EnumerateArray())
                    {
                        var id = webset.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                        var title = webset.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : null;
                        var createdAt = webset.TryGetProperty("createdAt", out var createdProp) ?
                            DateTime.Parse(createdProp.GetString()!) : DateTime.MinValue;

                        if (!string.IsNullOrEmpty(id))
                        {
                            // Use first part of search query as title if no title set
                            if (string.IsNullOrEmpty(title))
                            {
                                if (webset.TryGetProperty("searches", out var searches) && searches.GetArrayLength() > 0)
                                {
                                    var firstSearch = searches[0];
                                    if (firstSearch.TryGetProperty("query", out var query))
                                    {
                                        var queryText = query.GetString();
                                        title = queryText?.Length > 50 ? queryText.Substring(0, 47) + "..." : queryText;
                                    }
                                }
                            }

                            websets.Add((id!, title ?? "Untitled", createdAt));
                        }
                    }
                }

                Console.WriteLine($"[{listId}] Found {websets.Count} websets");
                websetsState.Set(websets.OrderByDescending(w => w.Item3).ToList());
                loadingWebsets.Set(_ => false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{listId}] ERROR: {ex.Message}");
                loadingWebsets.Set(_ => false);
            }
        });

        // Handler for loading a specific webset
        var loadWebset = new Func<string, ValueTask>(async (websetId) =>
        {
            var loadId = Guid.NewGuid();
            Console.WriteLine($"[{loadId}] ===== LOADING WEBSET {websetId} =====");

            loadingState.Set(_ => true);
            errorState.Set(_ => null);

            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(5);
                httpClient.DefaultRequestHeaders.Add("accept", "application/json");
                httpClient.DefaultRequestHeaders.Add("x-api-key", ExaApiKey);

                // Fetch items from the webset
                var listUrl = $"{ExaApiUrl}/{websetId}/items";
                Console.WriteLine($"[{loadId}] Fetching from: {listUrl}");

                var listResponse = await httpClient.GetAsync(listUrl);
                var listContent = await listResponse.Content.ReadAsStringAsync();

                if (!listResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[{loadId}] ERROR: {listResponse.StatusCode}");
                    Console.WriteLine($"[{loadId}] Response: {listContent}");
                    errorState.Set(_ => $"Error loading webset: {listResponse.StatusCode}");
                    loadingState.Set(_ => false);
                    return;
                }

                Console.WriteLine($"[{loadId}] ===== LOAD WEBSET RESPONSE =====");
                Console.WriteLine(listContent);
                Console.WriteLine($"[{loadId}] ================================");

                // Store the raw API response for viewing
                apiResponseData.Set(_ => listContent);

                var listDoc = JsonDocument.Parse(listContent);
                var leads = new List<LeadRecord>();

                if (listDoc.RootElement.TryGetProperty("data", out var items))
                {
                    Console.WriteLine($"[{loadId}] Found {items.GetArrayLength()} items");

                    foreach (var item in items.EnumerateArray())
                    {
                        string? url = null;
                        string? title = null;
                        double? score = null;

                        if (item.TryGetProperty("properties", out var properties))
                        {
                            url = properties.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
                            var description = properties.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;

                            if (properties.TryGetProperty("company", out var company))
                            {
                                title = company.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
                                if (string.IsNullOrEmpty(title)) title = description;
                            }
                            else
                            {
                                title = description;
                            }

                            if (properties.TryGetProperty("evaluations", out var evaluations) && evaluations.GetArrayLength() > 0)
                            {
                                var firstEval = evaluations[0];
                                if (firstEval.TryGetProperty("satisfied", out var satisfied))
                                {
                                    score = satisfied.GetString() == "yes" ? 1.0 : 0.0;
                                }
                            }
                        }

                        string? contactEmail = null;
                        string? linkedInUrl = null;
                        string? phoneNumber = null;
                        string? servicesSummary = null;

                        if (item.TryGetProperty("enrichments", out var enrichments) && enrichments.GetArrayLength() > 0)
                        {
                            foreach (var enrichment in enrichments.EnumerateArray())
                            {
                                var description = enrichment.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;
                                var value = enrichment.TryGetProperty("value", out var valueProp) ? valueProp.GetString() : null;

                                switch (description)
                                {
                                    case "Contact Email/Form":
                                        contactEmail = value;
                                        break;
                                    case "LinkedIn URL":
                                        linkedInUrl = value;
                                        break;
                                    case "Phone Number":
                                        phoneNumber = value;
                                        break;
                                    case "Services Summary":
                                        servicesSummary = value;
                                        break;
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(url))
                        {
                            leads.Add(new LeadRecord(url!, title, contactEmail, linkedInUrl, phoneNumber, servicesSummary, score));
                        }
                    }
                }

                Console.WriteLine($"[{loadId}] Loaded {leads.Count} leads from webset");
                leadsState.Set(leads);
                loadingState.Set(_ => false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{loadId}] ERROR: {ex.Message}");
                errorState.Set(_ => $"Error: {ex.Message}");
                loadingState.Set(_ => false);
            }
        });

        // Don't auto-load websets - only load when user clicks the button

        // Handler for fetching leads (creates new webset)
        var fetchLeads = new Func<ValueTask>(async () =>
        {
            var fetchId = Guid.NewGuid();
            Console.WriteLine($"[{fetchId}] ===== FETCH BUTTON CLICKED =====");
            Console.WriteLine($"[{fetchId}] Timestamp: {DateTime.Now:HH:mm:ss.fff}");

            loadingState.Set(_ => true);
            errorState.Set(_ => null);

            try
            {
                Console.WriteLine($"[{fetchId}] Calling Exa.ai API...");

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(30); // 30 minute timeout for entire operation (enrichments take longer)
                httpClient.DefaultRequestHeaders.Add("accept", "application/json");
                httpClient.DefaultRequestHeaders.Add("x-api-key", ExaApiKey);

                // Full request with criteria and enrichments
                var requestBody = new
                {
                    search = new
                    {
                        query = "I'm looking for accounting and bookkeeping firms in Uppsala, Sweden that specialize in Fortnox",
                        criteria = new[]
                        {
                            new { description = "bookkeeping or accounting firm located in uppsala, sweden" },
                            new { description = "specializes in fortnox or works with clients using fortnox" },
                            new { description = "firm size is between 1 and 50 employees" },
                            new { description = "utilizes fortnox integrations for automation or uses ai to improve accounting processes" }
                        },
                        count = 60
                    },
                    enrichments = new[]
                    {
                        new { description = "Contact Email/Form", format = "text" },
                        new { description = "LinkedIn URL", format = "url" },
                        new { description = "Phone Number", format = "text" },
                        new { description = "Services Summary", format = "text" }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                Console.WriteLine($"[{fetchId}] Sending POST request to Exa.ai...");
                var response = await httpClient.PostAsync(ExaApiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"[{fetchId}] Response Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[{fetchId}] ERROR: API returned {response.StatusCode}");
                    Console.WriteLine($"[{fetchId}] Response: {responseContent}");
                    errorState.Set(_ => $"API Error: {response.StatusCode} - {responseContent}");
                    loadingState.Set(_ => false);
                    Console.WriteLine($"[{fetchId}] ==============================");
                    return;
                }

                Console.WriteLine($"[{fetchId}] Parsing JSON response...");
                var jsonDoc = JsonDocument.Parse(responseContent);

                // Check if this is a webset creation response (async job)
                if (jsonDoc.RootElement.TryGetProperty("id", out var websetIdProp) &&
                    jsonDoc.RootElement.TryGetProperty("status", out var statusProp))
                {
                    var websetId = websetIdProp.GetString();
                    var status = statusProp.GetString();

                    Console.WriteLine($"[{fetchId}] Webset created with ID: {websetId}");
                    Console.WriteLine($"[{fetchId}] Initial status: {status}");
                    Console.WriteLine($"[{fetchId}] Polling for results...");

                    // Poll for results (no maximum, poll until completed or failed)
                    var leads = new List<LeadRecord>();
                    var pollDelay = 2000; // 2 seconds between polls
                    var attempt = 0;

                    while (true)
                    {
                        attempt++;
                        await Task.Delay(pollDelay);

                        Console.WriteLine($"[{fetchId}] Poll attempt {attempt}...");

                        var getResponse = await httpClient.GetAsync($"{ExaApiUrl}/{websetId}");
                        var getContent = await getResponse.Content.ReadAsStringAsync();

                        if (!getResponse.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"[{fetchId}] ERROR polling: {getResponse.StatusCode}");
                            continue;
                        }

                        var pollDoc = JsonDocument.Parse(getContent);

                        if (pollDoc.RootElement.TryGetProperty("status", out var pollStatus))
                        {
                            var currentStatus = pollStatus.GetString();
                            Console.WriteLine($"[{fetchId}] Current status: {currentStatus}");

                            if (currentStatus == "idle" || currentStatus == "completed")
                            {
                                Console.WriteLine($"[{fetchId}] Webset {currentStatus}! Fetching results from list endpoint...");

                                // Use the list endpoint to get items
                                var listUrl = $"{ExaApiUrl}/{websetId}/items";
                                Console.WriteLine($"[{fetchId}] Fetching from: {listUrl}");

                                var listResponse = await httpClient.GetAsync(listUrl);
                                var listContent = await listResponse.Content.ReadAsStringAsync();

                                if (!listResponse.IsSuccessStatusCode)
                                {
                                    Console.WriteLine($"[{fetchId}] ERROR fetching items: {listResponse.StatusCode}");
                                    Console.WriteLine($"[{fetchId}] Response: {listContent}");
                                    errorState.Set(_ => $"Error fetching results: {listResponse.StatusCode}");
                                    loadingState.Set(_ => false);
                                    Console.WriteLine($"[{fetchId}] ==============================");
                                    return;
                                }

                                Console.WriteLine($"[{fetchId}] ===== LIST ENDPOINT RESPONSE =====");
                                Console.WriteLine(listContent);
                                Console.WriteLine($"[{fetchId}] ==================================");

                                // Store the raw API response for viewing
                                apiResponseData.Set(_ => listContent);

                                var listDoc = JsonDocument.Parse(listContent);

                                // Parse items from list response
                                if (listDoc.RootElement.TryGetProperty("data", out var items))
                                {
                                    Console.WriteLine($"[{fetchId}] Found {items.GetArrayLength()} items in response");

                                    foreach (var item in items.EnumerateArray())
                                    {
                                        string? url = null;
                                        string? title = null;
                                        double? score = null;

                                        // Get properties object
                                        if (item.TryGetProperty("properties", out var properties))
                                        {
                                            // Get URL from properties
                                            url = properties.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;

                                            // Get description (we'll use this as title if no company name)
                                            var description = properties.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;

                                            // Try to get company info
                                            if (properties.TryGetProperty("company", out var company))
                                            {
                                                title = company.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;

                                                // Fallback to description if no company name
                                                if (string.IsNullOrEmpty(title))
                                                {
                                                    title = description;
                                                }
                                            }
                                            else
                                            {
                                                title = description;
                                            }

                                            // Try to get score from evaluations
                                            if (properties.TryGetProperty("evaluations", out var evaluations) && evaluations.GetArrayLength() > 0)
                                            {
                                                var firstEval = evaluations[0];
                                                if (firstEval.TryGetProperty("satisfied", out var satisfied))
                                                {
                                                    var satisfiedValue = satisfied.GetString();
                                                    score = satisfiedValue == "yes" ? 1.0 : 0.0;
                                                }
                                            }
                                        }

                                        // Parse enrichments if available (currently empty in this test)
                                        string? contactEmail = null;
                                        string? linkedInUrl = null;
                                        string? phoneNumber = null;
                                        string? servicesSummary = null;

                                        if (item.TryGetProperty("enrichments", out var enrichments) && enrichments.GetArrayLength() > 0)
                                        {
                                            Console.WriteLine($"[{fetchId}] Found enrichments for {title}");
                                            foreach (var enrichment in enrichments.EnumerateArray())
                                            {
                                                var description = enrichment.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;
                                                var value = enrichment.TryGetProperty("value", out var valueProp) ? valueProp.GetString() : null;

                                                switch (description)
                                                {
                                                    case "Contact Email/Form":
                                                        contactEmail = value;
                                                        break;
                                                    case "LinkedIn URL":
                                                        linkedInUrl = value;
                                                        break;
                                                    case "Phone Number":
                                                        phoneNumber = value;
                                                        break;
                                                    case "Services Summary":
                                                        servicesSummary = value;
                                                        break;
                                                }
                                            }
                                        }

                                        if (!string.IsNullOrEmpty(url))
                                        {
                                            leads.Add(new LeadRecord(
                                                Url: url!,
                                                Title: title,
                                                ContactEmail: contactEmail,
                                                LinkedInUrl: linkedInUrl,
                                                PhoneNumber: phoneNumber,
                                                ServicesSummary: servicesSummary,
                                                Score: score
                                            ));

                                            Console.WriteLine($"[{fetchId}] Added lead: {title} ({url})");
                                        }
                                    }

                                    Console.WriteLine($"[{fetchId}] Successfully parsed {leads.Count} leads from items");
                                }
                                else
                                {
                                    Console.WriteLine($"[{fetchId}] WARNING: No 'data' property found in list response");
                                }

                                break; // Exit polling loop
                            }
                            else if (currentStatus == "failed")
                            {
                                Console.WriteLine($"[{fetchId}] ERROR: Webset failed");
                                errorState.Set(_ => "API Error: Webset processing failed");
                                loadingState.Set(_ => false);
                                Console.WriteLine($"[{fetchId}] ==============================");
                                return;
                            }
                            // else status is still "running", continue polling
                        }
                        else
                        {
                            Console.WriteLine($"[{fetchId}] WARNING: No status property in poll response");
                            break; // Exit if response format is unexpected
                        }
                    }

                    if (leads.Count == 0)
                    {
                        Console.WriteLine($"[{fetchId}] WARNING: Polling completed but no results found");
                    }

                    leadsState.Set(leads);
                    loadingState.Set(_ => false);

                    Console.WriteLine($"[{fetchId}] Data loaded successfully - {leads.Count} leads");
                    Console.WriteLine($"[{fetchId}] ==============================");
                }
                else
                {
                    Console.WriteLine($"[{fetchId}] ERROR: Unexpected API response format");
                    Console.WriteLine($"[{fetchId}] Response: {responseContent.Substring(0, Math.Min(1000, responseContent.Length))}");
                    errorState.Set(_ => "Unexpected API response format");
                    loadingState.Set(_ => false);
                    Console.WriteLine($"[{fetchId}] ==============================");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{fetchId}] ERROR: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                errorState.Set($"Error: {ex.Message}");
                loadingState.Set(false);
                Console.WriteLine($"[{fetchId}] ==============================");
            }
        });

        // Show loading state
        if (loadingState.Value)
        {
            return Layout.Center()
                | new Card(
                    new StackLayout([
                        new Loading(),
                        Text.Block("Fetching leads from Exa.ai API..."),
                        Text.Muted("This may take 5-10 minutes as the AI searches the web, verifies criteria, and enriches lead data with contact information")
                    ], gap: 8)
                ).Width(Size.Units(100));
        }

        // Show error state
        if (errorState.Value != null)
        {
            return Layout.Center()
                | new Card(
                    new StackLayout([
                        new Error("Failed to load leads"),
                        Text.Block(errorState.Value),
                        new Button("Try Again", _ => fetchLeads())
                    ], gap: 8)
                ).Width(Size.Units(100));
        }

        // Show empty state / initial state with buttons
        if (leadsState.Value.Count == 0)
        {
            // Build webset selector dropdown
            // Option<T>(label, value) - label is display text, value is what gets passed to onChange
            var websetOptions = websetsState.Value
                .Select(w => new Option<string>(
                    label: $"{w.Title} ({w.CreatedAt:yyyy-MM-dd HH:mm})",  // Display text
                    value: w.Id  // Actual webset ID that gets passed to loadWebset()
                ))
                .ToList();

            object websetSelector = websetOptions.Count > 0
                ? new StackLayout([
                    new SelectInput<string>(
                        value: selectedWebsetId.Value,
                        options: websetOptions,
                        onChange: async (e) =>
                        {
                            selectedWebsetId.Set(_ => e.Value);
                            if (!string.IsNullOrEmpty(e.Value))
                            {
                                await loadWebset(e.Value);
                            }
                        }
                    ).Placeholder("Select a webset..."),
                    Text.Muted($"{websetOptions.Count} saved webset(s) available")
                ], gap: 8)
                : new Button("Load Websets List", _ => listWebsets());

            // Show API response data if toggled
            if (showExampleData.Value)
            {
                var responseToShow = apiResponseData.Value ?? ExampleResponseData;
                var responseTitle = apiResponseData.Value != null ? "API Response" : "Example API Response";

                return new StackLayout([
                    new Card(
                        new StackLayout([
                            Text.H2(responseTitle),
                            Text.Block("GET /websets/v0/websets/{id}/items"),
                            new Button("Back to Data", _ => showExampleData.Set(_ => false))
                                .Variant(ButtonVariant.Outline)
                        ], gap: 8)
                    ),
                    new Code(responseToShow, Languages.Json)
                ], gap: 12);
            }

            return Layout.Center()
                | new Card(
                    new StackLayout([
                        Text.H2("Lead Generator"),
                        Text.Block("Load an existing webset to view lead data"),
                        new Separator(),
                        loadingWebsets.Value ? new Loading() : websetSelector,
                        apiResponseData.Value != null ? new Fragment([
                            new Separator(),
                            new Button("View API Response", _ => showExampleData.Set(_ => true))
                                .Variant(ButtonVariant.Ghost)
                        ]) : new Empty()
                    ], gap: 12)
                ).Width(Size.Units(140));
        }

        // Show API response data if toggled (from data view)
        if (showExampleData.Value)
        {
            var responseToShow = apiResponseData.Value ?? ExampleResponseData;
            var responseTitle = apiResponseData.Value != null ? "API Response" : "Example API Response";

            return new StackLayout([
                new Card(
                    new StackLayout([
                        Text.H2(responseTitle),
                        Text.Block("GET /websets/v0/websets/{id}/items"),
                        new WrapLayout([
                            new Button("Back to Data", _ => showExampleData.Set(_ => false))
                                .Variant(ButtonVariant.Outline),
                            Text.Muted($"{leadsState.Value.Count} leads loaded")
                        ], gap: 12)
                    ], gap: 8)
                ),
                new Code(responseToShow, Languages.Json)
            ], gap: 12);
        }

        // Build DataTable
        var dataTable = leadsState.Value.AsQueryable().ToDataTable()
            .Width(Size.Full())
            .Height(Size.Full())

            // Column headers
            .Header(e => e.Title, "Company")
            .Header(e => e.Url, "Website")
            .Header(e => e.ContactEmail, "Contact Email")
            .Header(e => e.PhoneNumber, "Phone")
            .Header(e => e.LinkedInUrl, "LinkedIn")
            .Header(e => e.ServicesSummary, "Services")
            .Header(e => e.Score, "Relevance Score")

            // Column widths
            .Width(e => e.Title, Size.Px(200))
            .Width(e => e.Url, Size.Px(250))
            .Width(e => e.ContactEmail, Size.Px(200))
            .Width(e => e.PhoneNumber, Size.Px(150))
            .Width(e => e.LinkedInUrl, Size.Px(200))
            .Width(e => e.ServicesSummary, Size.Px(300))
            .Width(e => e.Score, Size.Px(120))

            // Column types - URLs should be clickable links
            .DataTypeHint(e => e.Url, ColType.Link)
            .DataTypeHint(e => e.LinkedInUrl, ColType.Link)

            // Alignments
            .Align(e => e.Title, Align.Left)
            .Align(e => e.Url, Align.Left)
            .Align(e => e.ContactEmail, Align.Left)
            .Align(e => e.PhoneNumber, Align.Left)
            .Align(e => e.LinkedInUrl, Align.Left)
            .Align(e => e.ServicesSummary, Align.Left)
            .Align(e => e.Score, Align.Right)

            // Configuration
            .Config(config =>
            {
                config.AllowSorting = true;
                config.AllowFiltering = true;
                config.AllowLlmFiltering = true;
                config.AllowColumnReordering = true;
                config.AllowColumnResizing = true;
                config.AllowCopySelection = true;
                config.SelectionMode = SelectionModes.Rows;
                config.ShowIndexColumn = true;
                config.ShowSearch = true;
                config.BatchSize = 50;
                config.LoadAllRows = false;
            });

        // Return data table with a floating action button to view API response
        return new Fragment([
            dataTable,
            new FloatingPanel(
                new Button("View API Response", _ => showExampleData.Set(_ => true))
                    .Variant(ButtonVariant.Outline),
                align: Align.BottomRight
            )
        ]);
    }
}
