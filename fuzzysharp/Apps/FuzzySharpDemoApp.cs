using System;
using System.Linq;
using System.Collections.Generic;
using static FuzzySharp.Process;
using FuzzySharp.Extractor;
using Ivy.Core;

[App(icon: Icons.Sheet)]
public class FuzzySharpDemoApp : ViewBase
{
    public override object? Build()
    {
        var searchTerm = UseState("");

        // Expanded dataset
        var data = new[]
        {
            "Apple",
            "Banana",
            "Orange",
            "Grapefruit",
            "Watermelon",
            "Strawberry",
            "Blueberry",
            "Blackberry",
            "Pineapple",
            "Mango",
            "Papaya",
            "Cherry",
            "Peach",
            "Pear",
            "Plum",
            "Golden Delicious Apple",
            "Granny Smith Apple",
            "Honeycrisp Apple",
            "Wild Strawberry Jam",
            "Organic Blueberry Muffin",
            "Tropical Pineapple Smoothie",
            "Fresh Mango Salsa",
            "Dried Papaya Slices",
            "Dark Red Cherry Pie",
            "White Peach Tea",
            "Bartlett Pear Juice",
            "Plum Wine (Japanese Umeshu)",
            "Mixed Berry Yogurt",
            "Citrus Orange Soda",
            "Ruby Red Grapefruit Sparkling Water",
            "Seedless Watermelon Candy"
        };

        IEnumerable<ExtractedResult<string>> results =
            string.IsNullOrWhiteSpace(searchTerm.Value)
                ? Enumerable.Empty<ExtractedResult<string>>()
                : ExtractTop(searchTerm.Value, data, limit: 8);

        return Layout.Vertical()
            | Layout.Horizontal()
                | Text.Block("Search: ")
                | new TextInput(searchTerm)
                    .Placeholder("type to search...")
                    .Variant(TextInputs.Search)
                | new Button("Clear", () => searchTerm.Value = "")
            | Layout.Vertical(
                results.Select(r => (object)Text.Block($"{r.Value} ({r.Score}%)")).ToArray()
              );
    }
}