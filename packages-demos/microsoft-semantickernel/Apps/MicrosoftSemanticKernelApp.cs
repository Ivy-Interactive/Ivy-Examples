

using Ivy;
using Ivy.Core.Hooks;
using Microsoft.SemanticKernel;
using System.Net;

namespace MicrosoftSemanticKernelDemo.Apps;

[App(icon: Icons.PartyPopper, title: "MicrosoftSemanticKernel Demo")]
public class MicrosoftSemanticKernelApp : ViewBase
{

    public override object? Build()
    {
        //initialize Open Ai
        //***  REPLACE API_KEY WITH YOUR OPENAI APIKEY  ***
        var kernel = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion("gpt-4o-mini", "API_KEY")
            .Build();

        // Create the function for extracting an action list from meeting notes
        var extractTasks = kernel.CreateFunctionFromPrompt("Extract action items as a list without title \n{{$input}}");

        var notesFromMeeting = UseState(() => "Today we discussed project deadlines. John will send the report tomorrow. Maria will set up the next meeting.");
        var isLoading = UseState(false);
        var triggerRefresh = UseState(0);
        var tasks = UseState<string[]>([]);
        
        // Execute the extraction function asynchronously
        UseEffect(async () =>
        {
            isLoading.Set(true);
            var extractedTasks = await kernel.InvokeAsync(extractTasks, new() { ["input"] = notesFromMeeting });
            tasks.Set(extractedTasks.GetValue<string>().Split('\n'));
            isLoading.Set(false);
        }, triggerRefresh,EffectTrigger.AfterInit());
        
        return Layout.Horizontal()
            | new Card(
                Layout.Vertical()
                    | notesFromMeeting.ToTextAreaInput()
                    | new Button("Update tasks").HandleClick(_ => triggerRefresh.Set(triggerRefresh.Value + 1))
            ).Title("Input").Width(1 / 2f)
            | new Card(
                Layout.Vertical()
                    | Text.H3("Tasks")
                    | (isLoading.Value ? "Loading..." : tasks.Value)
            ).Title("Result").Width(1 / 2f);
    }

}