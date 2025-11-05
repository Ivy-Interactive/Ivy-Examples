namespace RestsharpExample;

[App(icon: Icons.Webhook, title: "RestSharp Demo")]
public class RestSharpApp : ViewBase
{
    public override object? Build()
    {
        var method = UseState<string?>(() => "GET");
        var url = UseState<string>(() => "https://api.restful-api.dev/objects");
        var requestBody = UseState<string>(() => "");
        var response = UseState<string>(() => "");
        var statusCode = UseState<string?>(() => "");
        var formatJson = UseState<bool>(() => false);
        var headerKeyTemp = UseState<string>(() => string.Empty);
        var headerValueTemp = UseState<string>(() => string.Empty);
        var headers = UseState(ImmutableArray.Create<HeaderItem>());


        var addHeader = (Event<Button> e) =>
        {
            var newHeader = new HeaderItem(headerKeyTemp.Value, headerValueTemp.Value);
            var updatedHeaders = headers.Value.Add(newHeader);
            headers.Set(updatedHeaders);
            headerKeyTemp.Set(String.Empty);
            headerValueTemp.Set(String.Empty);
        };

        var onSend = () =>
        {
            response.Value = string.Empty;
            statusCode.Value = string.Empty;
            try
            {
                var options = new RestClientOptions(url.Value)
                {
                    ThrowOnAnyError = false
                };
                var client = new RestClient(options);
                var request = new RestRequest();

                if (headers.Value.Count() > 0)
                {
                    foreach (var headerItem in headers.Value)
                    {
                        request.AddHeader(headerItem.Key, headerItem.Value);
                    }
                }

                if (requestBody.Value.Length > 0)
                    request.AddBody(requestBody.Value);

                RestResponse restResponse = null;

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
        //Align the items
        return Layout.Vertical()
            | new StackLayout([
                    new Button(method.Value ?? "Get").Outline().Width(50)
                              .WithDropDown(
                                  Methods
                                      .Select(o => MenuItem.Default(o.Label).HandleSelect(() => method.Set(o.Label)))
                                      .ToArray()
                              )
                   ,new TextInput(url,placeholder:"URL").Variant(TextInputs.Url).Width(250)
                   ,new Button("Send", onClick: onSend).Width(50)

                ], Orientation.Horizontal, gap: 3)

            | Layout.Tabs(
                        new Tab(
                            "Request Body",
                            new TextInput(requestBody)
                                .Placeholder("Request Body")
                                .Variant(TextInputs.Textarea)
                                .Height(30)
                        ),

                        new Tab(
                            "Request Headers",
                            Layout.Vertical()
                            | new StackLayout(
                                [
                                    headerKeyTemp.ToInput(placeholder: "Header Key"),
                                    headerValueTemp.ToInput(placeholder: "Header Value"),
                                    new Button("Add", onClick: addHeader)
                                ],
                                Orientation.Horizontal,
                                gap: 3
                            )


                   | new HeaderView(
                            new HeaderItem("Header Key", "Header Value"),
                            true,
                            () => { }
                        )

                           | headers.Value.Select(header =>
                            new HeaderView(
                                header,
                                false,
                                () =>
                                {
                                    headers.Set(headers.Value.Remove(header));
                                }
                            )

)

                )
            )



                   | new Separator()
                   | Text.Strong("Response Body")

                   | formatJson.ToInput("Format JSON")
                           | new TextInput(formatJson.Value ? FormatStringToJson(response.Value) : response.Value)
                           .Placeholder("Response will show here")
                           .Variant(TextInputs.Textarea)
                            .Height(30)
                    | Text.Block($"Status code: {statusCode.Value}").Color(statusCode.Value.Contains(HttpStatusCode.OK.ToString()) ? Colors.Green : Colors.Black)




                 .Width(Size.Units(120).Max(700)

               );
    }
    public class HeaderItem
    {
        public HeaderItem(string key, string value)
        {
            Key = key;
            Value = value;
        }
        public string Key { get; set; }
        public string Value { get; set; }
    }
    public class HeaderView(HeaderItem headerItem, bool isHeader, Action deleteHeader) : ViewBase
    {
        public override object? Build()
        {
            if (isHeader)
                return Layout.Horizontal(
               Text.Strong(headerItem.Key).Width(100),
               Text.Strong(headerItem.Value).Width(100),
               Text.Block("")
           )
           .Align(Align.Left)
           .Width(Size.Full());


            return Layout.Horizontal(
               Text.Block(headerItem.Key).Width(100),
               Text.Block(headerItem.Value).Width(100),
                new Button(null, _ => deleteHeader())
                    .Icon(Icons.Trash)
                    .Variant(ButtonVariant.Outline)
           )
           .Align(Align.Left)
           .Width(Size.Full());

        }
    }

    private static readonly Option<Method>[] Methods = [
        new Option<Method>(Method.Get.ToString(), Method.Get),
        new Option<Method>(Method.Post.ToString(), Method.Post),
        new Option<Method>(Method.Put.ToString(), Method.Put),
        new Option<Method>(Method.Patch.ToString(), Method.Patch),
        new Option<Method>(Method.Delete.ToString(), Method.Delete)
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
