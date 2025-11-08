using Ivy;
using Ivy.Shared;      // for Colors.*
using Cysharp.Text;

namespace IvyExamples.ZStringDemo.Apps;

[App(icon: Icons.PartyPopper, title: "ZString Demo")]
public class HelloApp : ViewBase
{
    public override object Build()
    {
        // States for visible outputs
        var concatState   = this.UseState<string>();
        var formatState   = this.UseState<string>();
        var joinState     = this.UseState<string>();
        var builderState  = this.UseState<string>();
        var preparedState = this.UseState<string>();

        return
            Layout.Center()
            | (
                Layout.Vertical().Gap(14).Padding(3)

                | Text.H2("ZString Demo in Ivy")
                | Text.Block("Each section shows the **input** and the computed **output** so it’s clear what ZString does.")

                // ── Concat ───────────────────────────────────────────────────────────
                | new Card(
                    Layout.Vertical().Gap(8)
                    | Text.H3("1) Concat")
                    | Text.Markdown("**Input:** `\"Hello\" + \" \" + \"Ivy\" + \" \" + 2025`")
                    | new Button("Run Concat", () =>
                    {
                        var output = ZString.Concat("Hello", " ", "Ivy", " ", 2025);
                        concatState.Value = output;
                    })
                    | Text.Block($"Output: {concatState.Value ?? string.Empty}").Color(Colors.Green)
                  )

                // ── Format ──────────────────────────────────────────────────────────
                | new Card(
                    Layout.Vertical().Gap(8)
                    | Text.H3("2) Format")
                    | Text.Markdown("**Input:** `ZString.Format(\"Pi is {0:0.00}\", 3.14159)`")
                    | new Button("Run Format", () =>
                    {
                        var output = ZString.Format("Pi is {0:0.00}", 3.14159);
                        formatState.Value = output;
                    })
                    | Text.Block($"Output: {formatState.Value ?? string.Empty}").Color(Colors.Green)
                  )

                // ── Join ────────────────────────────────────────────────────────────
                | new Card(
                    Layout.Vertical().Gap(8)
                    | Text.H3("3) Join")
                    | Text.Markdown("**Input:** `ZString.Join(\", \", new[] { \"A\", \"B\", \"C\" })`")
                    | new Button("Run Join", () =>
                    {
                        var output = ZString.Join(", ", new[] { "A", "B", "C" });
                        joinState.Value = output;
                    })
                    | Text.Block($"Output: {joinState.Value ?? string.Empty}").Color(Colors.Green)
                  )

                // ── StringBuilder ───────────────────────────────────────────────────
                | new Card(
                    Layout.Vertical().Gap(8)
                    | Text.H3("4) CreateStringBuilder")
                    | Text.Markdown("**Input:**" +
                                    "\n```csharp\nusing var sb = ZString.CreateStringBuilder();\n" +
                                    "sb.Append(\"foo\");\n" +
                                    "sb.AppendLine(42);\n" +
                                    "sb.AppendFormat(\"{0} {1:.###}\", \"bar\", 123.456789);\n" +
                                    "var output = sb.ToString();\n```")
                    | new Button("Run StringBuilder", () =>
                    {
                        using var sb = ZString.CreateStringBuilder();
                        sb.Append("foo");
                        sb.AppendLine(42);
                        sb.AppendFormat("{0} {1:.###}", "bar", 123.456789);
                        builderState.Value = sb.ToString();
                    })
                    | Text.Block($"Output:\n{builderState.Value ?? string.Empty}")
                        .Color(Colors.Purple)
                  )

                // ── Prepared Format ─────────────────────────────────────────────────
                | new Card(
                    Layout.Vertical().Gap(8)
                    | Text.H3("5) Prepared Format")
                    | Text.Markdown("**Input:** `var tpl = ZString.PrepareUtf16<int,int>(\"x:{0}, y:{1:000}\"); tpl.Format(10, 20)`")
                    | new Button("Run Prepared Format", () =>
                    {
                        var tpl = ZString.PrepareUtf16<int, int>("x:{0}, y:{1:000}");
                        preparedState.Value = tpl.Format(10, 20);
                    })
                    | Text.Block($"Output: {preparedState.Value ?? string.Empty}").Color(Colors.Blue)
                  )
            );
    }
}
