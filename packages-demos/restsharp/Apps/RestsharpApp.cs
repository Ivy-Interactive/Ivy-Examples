namespace RestsharpExample;

[App(icon: Icons.Webhook, title: "RestSharp Demo")]
public class RestSharpApp : ViewBase
{
    private const string BaseApiUrl = "https://api.restful-api.dev/objects";

    private static bool RequiresResourceId(string? method) => 
        method?.ToUpper() is "DELETE" or "PUT" or "PATCH";

    private static string BuildUrlWithResourceId(string? resourceId) =>
        !string.IsNullOrWhiteSpace(resourceId) ? $"{BaseApiUrl}/{resourceId}" : BaseApiUrl;

    public override object? Build()
    {
        var method = UseState<string?>(() => "GET");
        var url = UseState<string>(() => BaseApiUrl);
        var resourceId = UseState<string>(() => "");
        var requestBody = UseState<string>(() => "");
        var response = UseState<string>(() => "");
        var statusCode = UseState<string?>(() => "");
        var formatJson = UseState<bool>(() => true);

        // Update URL when ID changes for methods that need it
        UseEffect(() =>
        {
            if (RequiresResourceId(method.Value))
            {
                url.Set(BuildUrlWithResourceId(resourceId.Value));
            }
        }, resourceId, method);

        // Update URL based on method
        var updateUrlForMethod = (string? newMethod) =>
        {
            if (newMethod == null) return;

            switch (newMethod.ToUpper())
            {
                case "GET":
                    url.Set(BaseApiUrl);
                    break;
                case "POST":
                    url.Set(BaseApiUrl);
                    if (string.IsNullOrWhiteSpace(requestBody.Value))
                    {
                        requestBody.Set(@"{
  ""name"": ""New Object"",
  ""data"": {
    ""color"": ""Red"",
    ""capacity"": ""128 GB""
  }
}");
                    }
                    break;
                case "PUT":
                case "PATCH":
                    url.Set(BuildUrlWithResourceId(resourceId.Value));
                    if (string.IsNullOrWhiteSpace(requestBody.Value))
                    {
                        requestBody.Set(@"{
  ""name"": ""Updated Object"",
  ""data"": {
    ""color"": ""Blue"",
    ""capacity"": ""256 GB""
  }
}");
                    }
                    break;
                case "DELETE":
                    url.Set(BuildUrlWithResourceId(resourceId.Value));
                    break;
            }
        };

        var onSend = () =>
        {
            response.Value = string.Empty;
            statusCode.Value = string.Empty;
            try
            {
                // Build final URL with ID if needed
                var finalUrl = RequiresResourceId(method.Value) 
                    ? BuildUrlWithResourceId(resourceId.Value)
                    : url.Value;

                var options = new RestClientOptions(finalUrl)
                {
                    ThrowOnAnyError = false
                };
                var client = new RestClient(options);
                var request = new RestRequest();

                if (requestBody.Value.Length > 0)
                    request.AddBody(requestBody.Value);

                RestResponse? restResponse = null;

                if (Method.Get.ToString().Equals(method.Value, StringComparison.CurrentCultureIgnoreCase))
                    restResponse = client.ExecuteGet(request);
                else if (Method.Post.ToString().Equals(method.Value, StringComparison.CurrentCultureIgnoreCase))
                    restResponse = client.ExecutePost(request);
                else if (Method.Put.ToString().Equals(method.Value, StringComparison.CurrentCultureIgnoreCase))
                    restResponse = client.ExecutePut(request);
                else if (Method.Patch.ToString().Equals(method.Value, StringComparison.CurrentCultureIgnoreCase))
                    restResponse = client.ExecutePatch(request);
                else if (Method.Delete.ToString().Equals(method.Value, StringComparison.CurrentCultureIgnoreCase))
                    restResponse = client.ExecuteDelete(request);
                else { throw new Exception("This method is not implemented."); }

                statusCode.Set($"{restResponse.StatusCode.ToString()} ({(int)restResponse.StatusCode})");
                response.Set(restResponse?.Content ?? string.Empty);

            }
            catch (Exception ex)
            {
                statusCode.Set(string.Empty);
                response.Set(ex.Message);
            }
        };

        // Left card - Actions (Request)
        var requestControls = new List<object>
        {
            new Button(method.Value ?? "GET")
                .Outline()
                .WithDropDown(
                    Methods
                        .Select(o => MenuItem.Default(o.Label).HandleSelect(() =>
                        {
                            method.Set(o.Label);
                            updateUrlForMethod(o.Label);
                        }))
                        .ToArray()
                ),
            new TextInput(url, placeholder: "URL")
                .Variant(TextInputs.Url)
        };

        if (RequiresResourceId(method.Value))
        {
            requestControls.Add(new TextInput(resourceId, placeholder: "ID"));
        }

        requestControls.Add(new Button("Send", onClick: onSend).Width(50));

        var statusCallout = string.IsNullOrWhiteSpace(statusCode.Value)
            ? null
            : statusCode.Value.Contains(HttpStatusCode.OK.ToString())
                ? Callout.Success($"Request successful! Status code: {statusCode.Value}", "Success")
                : Callout.Error($"Request failed. Status code: {statusCode.Value}", "Error");

        var isRequestBodyEnabled = method.Value?.ToUpper() == "POST" || method.Value?.ToUpper() == "PUT" || method.Value?.ToUpper() == "PATCH";
        var hasResponse = !string.IsNullOrWhiteSpace(response.Value);

        var mainCard = new Card(
            Layout.Vertical()
            | Text.H3("RestSharp Demo")
            | Text.Muted("This is a simple RestSharp demo. It allows you to send HTTP requests to a RESTful API and see the response.")
            | new Card(
                Layout.Vertical()
            | Text.H3("Request")
            | Text.Muted("Configure and send your HTTP request.")
            | new StackLayout(
                requestControls.ToArray(),
                Orientation.Horizontal
            )
            | requestBody.ToCodeInput()
                .Language(Languages.Json)
                .Placeholder(isRequestBodyEnabled ? "Request Body" : "Request Body (not used for this method)")
                .Height(Size.Fit().Max(50))
                .Disabled(!isRequestBodyEnabled)
            )
            | new Card(
                Layout.Vertical()
                | Text.H3("Response")
                | Text.Muted("This is the response from the API. It is displayed in JSON format.")
                | (hasResponse
                    ? Layout.Vertical()
                        | new Code(formatJson.Value ? FormatStringToJson(response.Value) : response.Value, Languages.Json)
                            .Height(Size.Fit().Max(70))
                        | formatJson.ToInput("Format JSON")
                    : Layout.Vertical()
                        | new Code("Please execute a request to see the response here", Languages.Json)
                            .Height(Size.Fit().Max(70)))
                | statusCallout
                )
            | Text.Small("This demo uses RestSharp library to send HTTP requests to a RESTful API.")
            | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [RestSharp](https://github.com/restsharp/RestSharp)")
        ).Width(Size.Fraction(0.45f));

        return Layout.Vertical().Align(Align.TopCenter)
            | mainCard.Height(Size.Fit().Min(Size.Full()));
    }

    private static readonly Option<Method>[] Methods = [
        new Option<Method>("GET", Method.Get),
        new Option<Method>("POST", Method.Post),
        new Option<Method>("PUT", Method.Put),
        new Option<Method>("PATCH", Method.Patch),
        new Option<Method>("DELETE", Method.Delete)
        ];

    public string FormatStringToJson(string input)
    {
        if (!string.IsNullOrWhiteSpace(input))
        {
            try
            {
                using var doc = JsonDocument.Parse(input);
                input = JsonSerializer.Serialize(doc, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch
            {
                // ignoring invalid json
            }
        }
        return input;
    }

}

