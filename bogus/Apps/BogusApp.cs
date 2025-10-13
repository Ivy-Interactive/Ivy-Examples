using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Immutable;
using Ivy.Shared;
using Ivy.Views.Builders;
using Ivy.Views.Forms;
using Bogus;
using Bogus.DataSets;

namespace BogusExample.Apps
{
    [App(icon: Icons.Check)]
    public class BogusApp : ViewBase
    {
        public override object? Build()
        {
            var orders = this.UseState(ImmutableArray.Create<Order>());
            var fruits = this.UseState(ImmutableArray.Create("apple", "banana", "orange", "strawberry", "kiwi"));
            var fruitInput = this.UseState("");

            return new Card().Title("Orders").Description("Generate sample orders with the Bogus library.")
           | (Layout.Vertical()

              // Fruit editor
              | Text.Muted("Fruits")
              | (Layout.Horizontal().Width(Size.Full())
                 | fruitInput.ToTextInput(placeholder: "Add fruit...").Width(Size.Grow())
                 | new Button("Add", _ =>
                   {
                       var f = (fruitInput.Value ?? "").Trim();
                       if (f.Length == 0) return;

                       if (!fruits.Value.Any(x => x.Equals(f, StringComparison.OrdinalIgnoreCase)))
                           fruits.Set(fruits.Value.Add(f));

                       fruitInput.Set("");
                   }).Icon(Icons.Plus).Variant(ButtonVariant.Secondary)
              )
              | fruits.Value.Select(f =>
                    Layout.Horizontal().Width(Size.Full())
                    | Text.Literal(f).Width(Size.Grow())
                    | new Button(null, _ => fruits.Set(fruits.Value.Remove(f)))
                        .Icon(Icons.Trash).Variant(ButtonVariant.Outline)
                )
              | new Separator()

              // Generator
              | (Layout.Horizontal().Width(Size.Full())
                 | new Button("Generate 10", _ =>
                   {
                       var fruit = (fruits.Value.Length > 0
                           ? fruits.Value
                           : ImmutableArray.Create("apple", "banana", "orange", "strawberry", "kiwi"))
                           .ToArray();

                       var orderIds = 0;

                       var testOrders = new Faker<Order>()
                           .StrictMode(true)
                           .RuleFor(o => o.OrderId, f => orderIds++)
                           .RuleFor(o => o.Item, f => f.PickRandom(fruit))
                           .RuleFor(o => o.Quantity, f => f.Random.Number(1, 10))
                           .RuleFor(o => o.LotNumber, f => f.Random.Int(0, 100).OrNull(f, .8f));

                       var generated = testOrders.Generate(10);
                       orders.Set(ImmutableArray.CreateRange(generated));
                   }).Icon(Icons.RefreshCw).Variant(ButtonVariant.Primary)
              )

              | new Separator()
              | (Layout.Horizontal().Width(Size.Full())
                 | Text.Muted("OrderId").Width(0.2)
                 | Text.Muted("Item").Width(Size.Grow())
                 | Text.Muted("Quantity").Width(0.2)
                 | Text.Muted("LotNumber").Width(0.2)
              )
              | new Separator()
              | orders.Value.Select(o =>
                    Layout.Horizontal().Align(Align.Center).Width(Size.Full())
                    | Text.Literal(o.OrderId.ToString()).Width(0.2)
                    | Text.Literal(o.Item).Width(Size.Grow())
                    | Text.Literal(o.Quantity.ToString()).Width(0.2)
                    | Text.Muted(o.LotNumber?.ToString() ?? "null").Width(0.2)
                )
            );
        }

        internal class Order
        {
            public int OrderId { get; set; }
            public string Item { get; set; }
            public int Quantity { get; set; }
            public int? LotNumber { get; set; }
        }
    }
}
