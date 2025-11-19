namespace MicrosoftSemanticKernelExample;

[App(icon: Icons.Microsoft, title: "Microsoft.SemanticKernel")]
public class MicrosoftSemanticKernelApp : ViewBase
{

    public override object? Build()
    {
        // Get API key from environment variable
        // Set it using: $env:APIKEY = "your-api-key-here" (PowerShell)
        var apiKey = Environment.GetEnvironmentVariable("APIKEY")
            ?? throw new InvalidOperationException("APIKEY environment variable is not configured. Please set $env:APIKEY with your OpenAI API key.");

        // Initialize OpenAI kernel
        var kernel = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion("gpt-4o-mini", apiKey)
            .Build();

        // Create the function for extracting an action list from meeting notes
        var extractTasks = kernel.CreateFunctionFromPrompt("Extract action items as a list without title \n{{$input}}");

        var notesFromMeeting = UseState(() => @"Meeting Notes - Q4 Planning Session

Date: November 15, 2024

Attendees: Sarah (Project Manager), Michael (Lead Developer), Emily (Designer), David (QA Lead), Lisa (Marketing)

Action items discussed:

- Michael will reach out to the backend team today to schedule API integration work

- Emily will send draft mockups to the team by Monday for initial feedback

- Sarah will update the project timeline and share it with stakeholders by end of week

- David will prepare a testing plan document and share it with Michael before their meeting

- Lisa will create a content calendar for the marketing campaign launch

We also discussed budget concerns. Sarah will review the current spending and prepare a report for the finance team. Michael mentioned we might need additional cloud infrastructure, so he'll get quotes from our vendors and present them in the next meeting.

Next steps: Everyone should update their progress in the project management tool by end of day Friday. We'll reconvene next Tuesday at 2 PM to review progress.");
        var isLoading = UseState(false);
        var triggerRefresh = UseState(0);
        var tasks = UseState<string[]>([]);
        
        // Execute the extraction function asynchronously
        UseEffect(async () =>
        {
            isLoading.Set(true);
            try
            {
                var extractedTasks = await kernel.InvokeAsync(extractTasks, new() { ["input"] = notesFromMeeting.Value });
                var taskLines = extractedTasks.GetValue<string>()
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToArray();
                tasks.Set(taskLines);
            }
            finally
            {
                isLoading.Set(false);
            }
        }, triggerRefresh);
        
        return Layout.Horizontal().Gap(5).Padding(5)
            | new Card(
                Layout.Vertical()
                    | Text.H3("Input Text")
                    | Text.Muted("Enter or paste your text here. The AI will extract action items from it.")
                    | notesFromMeeting.ToTextAreaInput().Height(Size.Units(80))
                    | new Button("Update tasks").HandleClick(_ => triggerRefresh.Set(triggerRefresh.Value + 1))
                    | new Spacer()
                    | Text.Small("This demo uses the Microsoft.SemanticKernel for intelligent text matching.")
                    | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [Microsoft.SemanticKernel](https://github.com/microsoft/semantic-kernel)")
            ).Height(Size.Fit().Min(Size.Full()))
            | new Card(
                Layout.Vertical()
                    | Text.H3("Extracted Action Items")
                    | Text.Muted("Action items extracted from your text will appear here.")
                    | (triggerRefresh.Value == 0
                        ? Text.Muted("Click 'Update tasks' to extract action items")
                        : isLoading.Value
                            ? Text.Muted("Loading...")
                            : tasks.Value.Length > 0
                                ? Layout.Vertical()
                                    | tasks.Value
                                    | Text.Small($"Total: {tasks.Value.Length} action item(s)").Muted()
                                : Text.Muted("No action items found in the text"))
            ).Height(Size.Fit().Min(Size.Full()));
    }
}