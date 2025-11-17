namespace ZStringExample;

[App(icon: Icons.Code, title: "ZString")]
public class ZStringApp : ViewBase
{
    public override object Build()
    {
        var selectedOperation = this.UseState<string?>(() => null);
        var resultState = this.UseState<string?>(() => null);
        var operations = new Dictionary<string, (string code, Func<string> execute)>
        {
            ["Concat"] = (
                "var output = ZString.Concat(\"Hello\", \" \", \"Ivy\", \" \", 2025);",
                () => ZString.Concat("Hello", " ", "Ivy", " ", 2025)
            ),
            ["Format"] = (
                "var output = ZString.Format(\"Pi is {0:0.00}\", 3.14159);",
                () => ZString.Format("Pi is {0:0.00}", 3.14159)
            ),
            ["Join"] = (
                "var output = ZString.Join(\", \", new[] { \"A\", \"B\", \"C\" });",
                () => ZString.Join(", ", new[] { "A", "B", "C" })
            ),
            ["CreateStringBuilder"] = (
                "using var sb = ZString.CreateStringBuilder();\n" +
                "sb.Append(\"foo\");\n" +
                "sb.AppendLine(42);\n" +
                "sb.AppendFormat(\"{0} {1:.###}\", \"bar\", 123.456789);\n" +
                "var output = sb.ToString();",
                () =>
                {
                    using var sb = ZString.CreateStringBuilder();
                    sb.Append("foo");
                    sb.AppendLine(42);
                    sb.AppendFormat("{0} {1:.###}", "bar", 123.456789);
                    return sb.ToString();
                }
            ),
            ["Prepared Format"] = (
                "var tpl = ZString.PrepareUtf16<int, int>(\"x:{0}, y:{1:000}\");\n" +
                "var output = tpl.Format(10, 20);",
                () =>
                {
                    var tpl = ZString.PrepareUtf16<int, int>("x:{0}, y:{1:000}");
                    return tpl.Format(10, 20);
                }
            )
        };
        UseEffect(() => {
            if (selectedOperation.Value != null && operations.TryGetValue(selectedOperation.Value, out var op))
            {
                try
                {
                    resultState.Value = op.execute();
                }
                catch (Exception ex)
                {
                    resultState.Value = $"Error: {ex.Message}";
                }
            }
            else
            {
                resultState.Value = null;
            }
        }, selectedOperation);

        var operationOptions = operations.Keys
            .Select(key => new Option<string>(key, key))
            .ToArray();

        object? codeBlocks = null;
        if (selectedOperation.Value != null && operations.TryGetValue(selectedOperation.Value, out var selectedOp))
        {
            codeBlocks = Layout.Vertical()
                | Text.Label("Function Code")
                | new Code(selectedOp.code, Languages.Csharp)
                    .ShowCopyButton()
                | Text.Label("Result")
                | new Code(resultState.Value ?? "Computing...", Languages.Text)
                    .ShowCopyButton();
        }

        return
            Layout.Center()
            | new Card(
                Layout.Vertical()
                | Text.H2("ZString")
                | Text.Muted("This demo showcases basic ZString operations with pre-configured examples. Select an operation to view the function code and its immediate result. All data is pre-prepared for demonstration purposes.")
                | selectedOperation
                    .ToSelectInput(operationOptions)
                    .Placeholder("Choose an operation...")
                    .WithField()
                    .Label("Select Operation")
                | (codeBlocks ?? Text.Muted("Please select an operation from the dropdown above"))
            ).Width(Size.Fraction(0.4f));
    }
}
