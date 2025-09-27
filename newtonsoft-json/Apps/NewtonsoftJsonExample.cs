using Ivy;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reflection;
[App(icon: Icons.Box)]
public class NewtonsoftJsonExample : ViewBase
{
    private string _inputJson = SampleJson.Pretty;
    private string _jsonPath = "$.store.book[?(@.price < 10)].title";
    private string _typedResult = "", _linqResult = "", _streamResult = "";
    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        Converters = { new StringEnumConverter(), new UnixEpochDateConverter() }
    };

    public override object? Build()
    {
        var attribute = typeof(JsonConvert).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
        var htmlResultTyped = UseState(Text.Html(_typedResult));
        var htmlResultLinq = UseState(Text.Html(_linqResult));
        var htmlResultStream = UseState(Text.Html(_streamResult));

        RunJsonPath();
        DeserializeToTypes();
        StreamRead();

        var jsonCode = UseState(_inputJson);
        var jsonPath = UseState(_jsonPath);

        return new Card()
            .Title("Newtonsoft Json Example")
            .Description("Newtonsoft Json.NET Version: " + attribute.Version)
            | Layout.Vertical(
                Layout.Horizontal(
                    new Card(
                        Layout.Vertical(
                            jsonCode.ToCodeInput()
                                .Width(Size.Auto())
                                .Height(Size.Auto())
                                .Language(Languages.Json),
                            Text.P("JSONPath:"),
                            jsonPath.ToCodeInput()
                                .Width(Size.Auto())
                                .Height(Size.Auto())
                                .Language(Languages.Json),
                            Layout.Horizontal(
                                new Button("Run", _ =>
                                {
                                    _inputJson = jsonCode.Value;
                                    _jsonPath = jsonPath.Value;
                                    RunJsonPath();
                                    DeserializeToTypes();
                                    StreamRead();

                                    htmlResultTyped.Set(Text.Html(_typedResult));
                                    htmlResultLinq.Set(Text.Html(_linqResult));
                                    htmlResultStream.Set(Text.Html(_streamResult));
                                })
                                .Icon(Icons.Play)
                                .Variant(ButtonVariant.Outline),
                            new Button("Reset", _ =>
                            {
                                _inputJson = SampleJson.Pretty;
                                _jsonPath = "$.store.book[?(@.price < 10)].title";

                                _typedResult = "";
                                _linqResult = "";
                                _streamResult = "";

                                RunJsonPath();
                                DeserializeToTypes();
                                StreamRead();

                                htmlResultTyped.Set(Text.Html(_typedResult));
                                htmlResultLinq.Set(Text.Html(_linqResult));
                                htmlResultStream.Set(Text.Html(_streamResult));
                            })
                                    .Icon(Icons.Reply)
                                    .Variant(ButtonVariant.Outline)

                                    )
                        )
                    )
                    .Title("Input JSON")
                    .Width(400)
                ),
                Layout.Horizontal(
                    new Card()
                        .Title("Typed Result")
                        .Width(400)
                        | htmlResultTyped.Value,
                    new Card()
                        .Title("Linq Result")
                        .Width(400)
                        | htmlResultLinq.Value,
                    new Card()
                        .Title("Stream Result")
                        .Width(400)
                        | htmlResultStream.Value
                )
            );
    }

    private void RunJsonPath()
    {
        var token = JToken.Parse(_inputJson);
        var matches = token.SelectTokens(_jsonPath);
        _linqResult = string.Join("\n", matches.Select(m => m.ToString(Formatting.Indented)));
        if (string.IsNullOrWhiteSpace(_linqResult))
            _linqResult = "(no matches)";
    }

    private void DeserializeToTypes()
    {
        var store = JsonConvert.DeserializeObject<Store>(_inputJson, Settings);
        var cheap = store?.Book?.Where(b => b.Price < 10)
            .Select(b => $"- {b.Title} by {b.Author} (${b.Price})").ToList() ?? new();
        _typedResult = "Cheap books (Price < 10):\n" + string.Join("\n", cheap);
    }

    private void StreamRead()
    {
        using var sr = new StringReader(_inputJson);
        using var r = new JsonTextReader(sr);
        var lines = new List<string>();
        while (r.Read())
            lines.Add($"TokenType={r.TokenType}, Value={r.Value}");
        _streamResult = string.Join('\n', lines);
    }
}

public sealed class UnixEpochDateConverter : JsonConverter<DateTime>
{
    public override DateTime ReadJson(JsonReader reader, Type t, DateTime v, bool hasV, JsonSerializer s) =>
        reader.TokenType == JsonToken.Integer && reader.Value is long secs
            ? DateTimeOffset.FromUnixTimeSeconds(secs).UtcDateTime
            : s.Deserialize<DateTime>(reader);

    public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer s) =>
        writer.WriteValue(new DateTimeOffset(value.ToUniversalTime()).ToUnixTimeSeconds());
}