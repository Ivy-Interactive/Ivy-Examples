# Aspose.Words for .NET with Ivy Framework

This example demonstrates how to use **Aspose.Words for .NET** with the **Ivy Framework** to create, manipulate, and export Word documents programmatically in a modern web application.

## What is Ivy?

Ivy is a modern C# web framework for building interactive web applications. It's heavily inspired by React and allows you to create rich user interfaces using C# Views and Widgets with built-in state management.

## What is Aspose.Words?

Aspose.Words for .NET is a powerful document processing library that enables developers to create, edit, convert, and print Word documents without requiring Microsoft Word to be installed on the system.

## Features Demonstrated

This example showcases the following Aspose.Words capabilities:

- **Document Creation**: Generate new Word documents from scratch
- **Text Formatting**: Apply various text styles, fonts, and formatting
- **Table Creation**: Build structured tables with data and styling
- **Headers & Footers**: Add professional headers and footers to documents
- **Mail Merge**: Create template-based documents with placeholders
- **Document Export**: Save documents in DOCX format for download

## Document Templates

The application includes several pre-built templates:

1. **Simple Letter** - A basic business letter template with header, date, and formatted content
2. **Table Report** - A structured report with formatted tables and sales data
3. **Invoice** - A professional invoice template with company details and itemized billing
4. **Form Letter** - A mail merge style document with placeholder fields for personalization

## Getting Started

### Prerequisites

- .NET 9.0 or later
- Ivy Framework (automatically included via NuGet)
- Aspose.Words for .NET (automatically included via NuGet)

### Installation

1. Clone this repository
2. Navigate to the `aspose-words` directory
3. Restore dependencies:
   ```bash
   dotnet restore
   ```

### Running the Application

```bash
dotnet watch
```

The application will start and open in your default browser. You'll see the Aspose.Words demo interface where you can:

1. Select a document template from the available options
2. Generate a preview of the document
3. Download the generated Word document in DOCX format

### Project Structure

```
aspose-words/
├── Apps/
│   └── AsposeWordsApp.cs          # Main Ivy application
├── AsposeWords.csproj             # Project configuration
├── GlobalUsings.cs                # Global using statements
├── Program.cs                     # Application entry point
└── README.md                      # This file
```

## Code Highlights

### Document Generation

Each template demonstrates different Aspose.Words features:

```csharp
// Creating a new document
var doc = new Document();
var builder = new DocumentBuilder(doc);

// Adding formatted text
builder.Font.Size = 16;
builder.Font.Bold = true;
builder.Writeln("Document Title");

// Creating tables
var table = builder.StartTable();
builder.InsertCell();
builder.Write("Cell Content");
builder.EndRow();
builder.EndTable();
```

### Ivy Integration

The Ivy framework provides reactive UI components:

```csharp
// State management
var selectedTemplate = UseState<DocumentTemplate?>();
var documentContent = UseState<string>("");

// File download
var downloadUrl = this.UseDownload(
    async () => {
        var doc = selectedTemplate.Value.Generator();
        using var stream = new MemoryStream();
        doc.Save(stream, SaveFormat.Docx);
        return stream.ToArray();
    },
    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    "document.docx"
);
```

## Learn More

- [Aspose.Words Documentation](https://docs.aspose.com/words/net/)
- [Ivy Framework Documentation](https://github.com/Ivy-Interactive/Ivy-Framework)
- [Download Aspose.Words](https://www.nuget.org/packages/Aspose.Words/)

## License

This example is provided for demonstration purposes. Please refer to Aspose.Words licensing terms for production use.

## Deployment

To deploy this application:

```bash
ivy deploy
```

Or build for production:

```bash
dotnet publish -c Release