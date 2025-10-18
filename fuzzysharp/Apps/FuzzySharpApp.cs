using System;
using System.Linq;
using System.Collections.Generic;
using static FuzzySharp.Process;
using FuzzySharp.Extractor;
using Ivy.Core;

[App(icon: Icons.Search, title: "FuzzySharp", path: ["Apps"])]
public class FuzzySharpApp : ViewBase
{
    public override object? Build()
    {
        var searchTerm = UseState("");
        var showInstructions = UseState(true);

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
            "Seedless Watermelon Candy",
            "National Aeronautics Space Administration"
        };

        IEnumerable<ExtractedResult<string>> results =
            string.IsNullOrWhiteSpace(searchTerm.Value)
                ? Enumerable.Empty<ExtractedResult<string>>()
                : ExtractTop(searchTerm.Value, data, limit: 5);

        var leftCard = new Card(
            Layout.Vertical().Gap(4).Padding(2)
            | Text.H2("Fuzzy Search")
            | Text.Muted("Intelligent search with typo tolerance")
            | new TextInput(searchTerm)
                .Placeholder("Try: 'aple', 'bana', 'berry'...")
                .Variant(TextInputs.Search)
            | new Spacer()
            | Text.Small("This demo uses the FuzzySharp NuGet package for intelligent text matching.")
            | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [FuzzySharp](https://github.com/JakeBayer/FuzzySharp)")
        ).Width(Size.Fraction(0.45f)).Height(110);

        var rightCard = new Card(
            Layout.Vertical().Gap(4).Padding(2)
            | (results.Any() ? 
                Layout.Vertical()
                    | Text.H2("Results")
                    | Text.Muted("Search results with similarity scores")
                    | Layout.Vertical(
                        results.Select(r => (object)new Badge($"{r.Value} ({r.Score}%)")
                            .Secondary()
                            .Width(Size.Fit())).ToArray()
                    ) :
                Layout.Vertical().Gap(2)
                    | Text.H2("Info")
                    | Text.Muted("Try these examples:")
                    | Text.Muted("• 'aple' → finds 'Apple'")
                    | Text.Muted("• 'bana' → finds 'Banana'")
                    | Text.Muted("• 'berry' → finds all berry fruits")
                    | Text.Muted("• 'smoothie' → finds 'Tropical Pineapple Smoothie'"))
        ).Width(Size.Fraction(0.45f)).Height(110);

        return Layout.Horizontal().Gap(6).Align(Align.Center)
            | leftCard
            | rightCard;
    }
}