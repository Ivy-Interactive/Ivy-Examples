using Ivy;

namespace Todo.Apps;

public record TodoItem(string Text, bool IsDone = false);

[App(icon: Icons.ListChecks)]
public class TodoApp : ViewBase
{
    public override object? Build()
    {
        var todos = UseState(() => new List<TodoItem>
        {
            new("Buy milk"),
            new("Wash dishes"),
            new("Write a novel")
        });
        var newItemText = UseState("");

        void AddItem()
        {
            if (string.IsNullOrWhiteSpace(newItemText.Value)) return;
            todos.Set([.. todos.Value, new TodoItem(newItemText.Value)]);
            newItemText.Set("");
        }

        var layout = Layout.Vertical();

        // Title
        layout |= Text.H1("✅ To-do list").Align(TextAlignment.Center);

        // Add form
        layout |= (Layout.Horizontal().Gap(2)
            | newItemText.ToTextInput(placeholder: "Add to-do item").OnSubmit(AddItem)
            | new Button("Add", AddItem, icon: Icons.Plus));

        if (todos.Value.Count == 0)
        {
            layout |= Callout.Info("No to-do items. Go fly a kite! 🪁");
        }
        else
        {
            var cardContent = Layout.Vertical().Gap(0);

            for (var i = 0; i < todos.Value.Count; i++)
            {
                var todo = todos.Value[i];
                var index = i;
                cardContent |= new TodoItemRow(todo, index, todos);
            }

            layout |= new Card(cardContent);

            if (todos.Value.Any(t => t.IsDone))
            {
                layout |= (Layout.Horizontal().AlignContent(Align.Center)
                    | new Button("Delete all checked", () =>
                    {
                        todos.Set(todos.Value.Where(t => !t.IsDone).ToList());
                    }, icon: Icons.Trash2).Ghost().Small());
            }
        }

        return Layout.Vertical().AlignContent(Align.TopCenter)
            | (layout.Width(Size.Units(160)));
    }
}

public class TodoItemRow(TodoItem todo, int index, IState<List<TodoItem>> todos) : ViewBase
{
    public override object? Build()
    {
        var isDone = UseState(todo.IsDone);

        UseEffect(() =>
        {
            var list = todos.Value.ToList();
            if (index < list.Count)
            {
                list[index] = list[index] with { IsDone = isDone.Value };
                todos.Set(list);
            }
        }, isDone);

        var label = todo.IsDone
            ? Text.Block(todo.Text).StrikeThrough().Color(Colors.Muted)
            : Text.Block(todo.Text);

        return Layout.Horizontal().AlignContent(Align.Left)
            | isDone.ToBoolInput().Variant(BoolInputVariant.Checkbox)
            | label.Width(Size.Full())
            | new Button(onClick: () =>
            {
                var list = todos.Value.ToList();
                list.RemoveAt(index);
                todos.Set(list);
            }, icon: Icons.Trash2).Ghost().Small();
    }
}
