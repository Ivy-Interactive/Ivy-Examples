# Acme Internal Project - File Converter

Web application created using [Ivy](https://github.com/Ivy-Interactive/Ivy).

Ivy is a web framework for building interactive web applications using C# and .NET.

## Features

This application includes a **File Converter** that allows you to:

- Upload text files (.txt, .csv, .json, .md, .log)
- Apply various transformations with button clicks:
  - **Uppercase** - Convert all text to uppercase
  - **Lowercase** - Convert all text to lowercase  
  - **Add Line Numbers** - Add line numbers to each line
  - **Reverse Text** - Reverse the entire text character by character
  - **Format JSON** - Format JSON with proper indentation
  - **Convert CSV to Markdown** - Convert CSV files to markdown tables
- Real-time preview of converted content
- Shows file size and name information

## Running the App

To run the application in development mode:

```bash
dotnet watch
```

Then navigate to <http://localhost:5010> in your browser.

## Testing the File Converter

Sample test files are included in the project root:

- `sample.txt` - Simple text file for testing text conversions
- `sample.csv` - CSV file for testing CSV to Markdown conversion
- `sample.json` - JSON file for testing JSON formatting

Just upload these files and try different conversion options!

## Deploy

```
ivy deploy
```

## Project Structure

- `Program.cs` - Application entry point and server configuration
- `Apps/FileConverterApp.cs` - Main file converter application
- `Apps/` - Additional applications
- `Connections/` - SignalR connections for real-time features
