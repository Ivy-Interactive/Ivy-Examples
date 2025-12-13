namespace MicrosoftAgentFramework.Views;

/// <summary>
/// Multi-agent tabbed chat view that shares state with parent
/// </summary>
public class AgentMultiChatView : ViewBase
{
    private readonly IState<List<AgentConfiguration>> _agents;
    private readonly IState<string?> _ollamaUrl;
    private readonly IState<string?> _ollamaModel;
    private readonly IState<List<AgentConfiguration>> _openAgentsList;

    public AgentMultiChatView(
        IState<List<AgentConfiguration>> agents,
        IState<string?> ollamaUrl,
        IState<string?> ollamaModel,
        IState<List<AgentConfiguration>> openAgentsList)
    {
        _agents = agents;
        _ollamaUrl = ollamaUrl;
        _ollamaModel = ollamaModel;
        _openAgentsList = openAgentsList;
    }

    public override object? Build()
    {
        var selectedTabIndex = UseState<int?>(Math.Max(0, _openAgentsList.Value.Count - 1));

        // Handle tab selection
        async ValueTask OnTabSelect(Event<TabsLayout, int> e)
        {
            selectedTabIndex.Set(e.Value);
            await ValueTask.CompletedTask;
        }

        // Handle tab close
        async ValueTask OnTabClose(Event<TabsLayout, int> e)
        {
            var tabIndex = e.Value;
            if (tabIndex < 0 || tabIndex >= _openAgentsList.Value.Count) 
            {
                await ValueTask.CompletedTask;
                return;
            }

            var updatedAgents = _openAgentsList.Value.ToList();
            updatedAgents.RemoveAt(tabIndex);
            _openAgentsList.Set(updatedAgents);

            // Adjust selected index
            selectedTabIndex.Set(updatedAgents.Count == 0 
                ? (int?)null 
                : Math.Min(selectedTabIndex.Value ?? 0, updatedAgents.Count - 1));
            
            await ValueTask.CompletedTask;
        }

        // Create tabs - AgentChatView uses AgentManager which handles model selection
        var tabs = _openAgentsList.Value
            .Select(agent => new Tab(agent.Name, 
                new AgentChatView(agent, _agents, _ollamaUrl.Value!, _ollamaModel.Value))
                .Icon(Icons.MessageSquare))
            .ToArray();

        if (tabs.Length == 0)
        {
            return Layout.Vertical().Gap(3).Padding(4).Align(Align.Center)
                | Text.Large("No agents open")
                | Text.Muted("Select an agent from the list to start chatting");
        }

        // Ensure selected index is valid
        var currentSelectedIndex = Math.Min(selectedTabIndex.Value ?? 0, tabs.Length - 1);
        if (currentSelectedIndex != selectedTabIndex.Value)
        {
            selectedTabIndex.Set(currentSelectedIndex);
        }

        return CreateTabsLayout(OnTabSelect, OnTabClose, currentSelectedIndex, tabs).Variant(TabsVariant.Tabs);
    }

    // Helper to create TabsLayout (max 10 tabs due to constructor limitation)
    private static TabsLayout CreateTabsLayout(
        Func<Event<TabsLayout, int>, ValueTask> onSelect,
        Func<Event<TabsLayout, int>, ValueTask> onClose,
        int index,
        Tab[] t) => t.Length switch
    {
        1 => new TabsLayout(onSelect, onClose, null, null, index, t[0]),
        2 => new TabsLayout(onSelect, onClose, null, null, index, t[0], t[1]),
        3 => new TabsLayout(onSelect, onClose, null, null, index, t[0], t[1], t[2]),
        4 => new TabsLayout(onSelect, onClose, null, null, index, t[0], t[1], t[2], t[3]),
        5 => new TabsLayout(onSelect, onClose, null, null, index, t[0], t[1], t[2], t[3], t[4]),
        6 => new TabsLayout(onSelect, onClose, null, null, index, t[0], t[1], t[2], t[3], t[4], t[5]),
        7 => new TabsLayout(onSelect, onClose, null, null, index, t[0], t[1], t[2], t[3], t[4], t[5], t[6]),
        8 => new TabsLayout(onSelect, onClose, null, null, index, t[0], t[1], t[2], t[3], t[4], t[5], t[6], t[7]),
        9 => new TabsLayout(onSelect, onClose, null, null, index, t[0], t[1], t[2], t[3], t[4], t[5], t[6], t[7], t[8]),
        _ => new TabsLayout(onSelect, onClose, null, null, index, t[0], t[1], t[2], t[3], t[4], t[5], t[6], t[7], t[8], t[9])
    };
}

