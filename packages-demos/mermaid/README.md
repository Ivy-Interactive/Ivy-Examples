# MermaidDotNet

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** is a web framework for building interactive web applications using C# and .NET.

## Interactive Example Using MermaidDotNet

This example demonstrates Mermaid flowchart generation using the [MermaidDotNet library](https://github.com/samsmithnz/MermaidDotNet) integrated with Ivy.

**What This Application Does:**

- **Visual Diagram Builder**: Create flowcharts by adding nodes and links through an intuitive UI
- **Node Management**: Add, remove, and configure nodes with different shapes (Rectangle, Circle, Rhombus, Hexagon, etc.)
- **Link Management**: Create connections between nodes with various link types (Normal, Dotted, Thick) and arrow types
- **Live Preview**: See your Mermaid diagram update in real-time as you make changes
- **Direction Control**: Choose diagram direction (Left-Right, Top-Down, Bottom-Top, Right-Left)
- **Code Generation**: Automatically generates Mermaid syntax code that can be used in markdown or HTML

**Technical Implementation:**

- Uses `MermaidDotNet.Flowchart` to programmatically generate Mermaid diagrams
- Two-panel layout: "Diagram Builder" for editing and "Mermaid Output" for visualization
- Reactive state management with automatic diagram regeneration on changes
- Support for 11 different node shapes and 4 link types
- Built-in Mermaid rendering using Ivy's `Text.Mermaid()` component

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd mermaid
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

## How to Use

1. **Add Nodes**: 
   - Enter a unique Node ID and descriptive text
   - Select a shape from the dropdown
   - Click "Add Node"

2. **Add Links**: 
   - Select "From" and "To" nodes from dropdowns
   - Optionally add a label for the link
   - Choose link type (Normal, Dotted, Thick, Invisible)
   - Choose arrow type (Normal, Circle, Cross, Open)
   - Click "Add Link"

3. **Customize Direction**: 
   - Use the Direction dropdown to change diagram flow (LR, TD, BT, RL)

4. **View Results**: 
   - See the generated Mermaid code in the code editor
   - View the live diagram preview below

5. **Manage Elements**: 
   - Click the trash icon next to any node or link to remove it
   - Removing a node automatically removes all connected links

## Features

**Supported Node Shapes:**
- Rectangle (default)
- Rounded
- Stadium
- Circle
- Rhombus (diamond)
- Hexagon
- Parallelogram
- Cylinder
- Trapezoid
- TrapezoidAlt
- Subroutine

**Supported Link Types:**
- Normal (solid line)
- Dotted (dashed line)
- Thick (bold line)
- Invisible (no visible line)

**Supported Arrow Types:**
- Normal (standard arrow)
- Circle (circle endpoint)
- Cross (cross endpoint)
- Open (open arrow)

**Diagram Directions:**
- LR (Left to Right)
- TD (Top Down)
- BT (Bottom to Top)
- RL (Right to Left)

## How to Deploy

Deploy this example to Ivy's hosting platform:

1. **Navigate to the example**:
   ```bash
   cd mermaid
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```

## Learn More

- MermaidDotNet: [github.com/samsmithnz/MermaidDotNet](https://github.com/samsmithnz/MermaidDotNet)
- Mermaid.js: [mermaid.js.org](https://mermaid.js.org/)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
