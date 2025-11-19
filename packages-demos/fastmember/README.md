# FastMember

<img width="1411" height="915" alt="image" src="https://github.com/user-attachments/assets/151b25cc-5ae3-4707-8364-65611c8742fd" />

<img width="1609" height="914" alt="image" src="https://github.com/user-attachments/assets/88d3c3ce-b79f-4fbe-af07-3ca1bac26f32" />

<img width="1608" height="906" alt="image" src="https://github.com/user-attachments/assets/4d634be2-4a0f-4af3-9c03-6abff686ec68" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Ffastmember%2Fdevconyainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## FastMember Performance Demo Application

This example demonstrates high-performance runtime member access capabilities using the [FastMember library](https://github.com/mgravell/fast-member) integrated with Ivy. FastMember is a .NET library for fast access to type fields and properties when member names are known only at runtime, using IL code generation for maximum performance.

**What This Application Does:**

This specific implementation creates a **FastMember Demo** application that allows users to:

- **View Sample Data**: Browse test product data in an interactive table format
- **Explore Demonstrations**: Select from multiple FastMember features to see example code and execution results:
  - **TypeAccessor**: Get and set property values by name for any type
  - **ObjectAccessor**: Work with specific object instances (static or DLR types)
  - **ObjectReader**: Efficiently read object sequences as IDataReader
  - **Dynamic Objects**: Work with ExpandoObject and DLR types
  - **Bulk Operations**: Process multiple objects efficiently
- **Run Performance Benchmarks**: Compare FastMember performance against:
  - Standard .NET Reflection (PropertyInfo)
  - PropertyDescriptor (System.ComponentModel)
  - Dynamic C# (dynamic keyword)
  - FastMember TypeAccessor and ObjectAccessor
- **View Results**: See execution results in formatted JSON with code examples

**Technical Implementation:**

- Uses FastMember's `TypeAccessor` and `ObjectAccessor` APIs for runtime member access
- Implements interactive demonstration selection with real-time code execution
- Creates performance benchmarks comparing multiple access methods
- Handles state management with Ivy's `UseState` and `UseEffect` hooks
- Displays code examples and execution results side-by-side
- Uses card-based layout for organized content presentation
- Supports all FastMember features including ObjectReader for IDataReader scenarios

**Key Features:**

- **Interactive Demonstrations**: Select from 5 different FastMember use cases with live code execution
- **Performance Comparison**: Benchmark FastMember against Reflection, PropertyDescriptor, and Dynamic C#
- **Code Examples**: View example code for each demonstration with syntax highlighting
- **Real-Time Results**: See execution results instantly in formatted JSON
- **Data Visualization**: Browse sample data in an interactive table
- **Clean UI**: Modern tab-based interface with card layouts for clear organization

**Performance Highlights:**

FastMember provides significantly faster performance than traditional Reflection:
- **Get Property**: FastMember is typically 2-5x faster than Reflection PropertyInfo
- **Set Property**: FastMember is typically 3-6x faster than Reflection PropertyInfo
- **Bulk Operations**: Ideal for high-performance scenarios requiring repeated member access

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd packages-demos/fastmember
   ```
3. **Restore dependencies**:
   ```bash
   dotnet restore
   ```
4. **Run the application**:
   ```bash
   dotnet watch
   ```
5. **Open your browser** to the URL shown in the terminal (typically `http://localhost:5010`)

## How to Deploy

Deploy this example to Ivy's hosting platform:

1. **Navigate to the example**:
   ```bash
   cd packages-demos/fastmember
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your FastMember demo application with a single command.

## Learn More

- FastMember library overview: [github.com/mgravell/fast-member](https://github.com/mgravell/fast-member)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
