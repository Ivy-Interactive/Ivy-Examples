# SmartFormat.NET Example

This is a simple demo of SmartFormat.NET running in an Ivy app. I built this to explore how the library handles pluralization and conditionals.

## What is SmartFormat.NET?

It's a string formatting library that goes beyond the standard C# string interpolation. Instead of writing if/else logic for plurals, you can handle it directly in the template string.

For example:
```
"You have {Count:plural:no items|one item|{} items}."
```

Works with Count = 0, 1, or 5 without extra code.

## Running it

```bash
cd smartformat-net/SmartFormatNetDemo
dotnet watch
```

Click the example buttons to see different formatting options. You can edit the templates and data to test your own patterns.

