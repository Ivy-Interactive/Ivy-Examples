# YamlDotNet

<img width="1919" height="910" alt="image" src="https://github.com/user-attachments/assets/090fbc31-1b9e-478e-86b7-4f2a8c80911b" />

<img width="1608" height="915" alt="image" src="https://github.com/user-attachments/assets/5a19e4e4-45b7-4995-98a5-f4fdfcd6752f" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fyamldotnet%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open this YamlDotNet demo in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **YamlDotNet tooling** ready to go
- **Zero local setup** required

## Created Using Ivy

Web application created with [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** unifies UI and backend into one C# codebase, letting you build internal tools and dashboards quickly with LLM-assisted workflows.

## Interactive Example Using YamlDotNet

This demo showcases YAML serialization and deserialization powered by [YamlDotNet](https://github.com/aaubry/YamlDotNet) within an Ivy application.

**What this application demonstrates:**

- **YAML Serialization**: Convert C# Person objects to YAML format with real-time preview and syntax highlighting.
- **YAML Deserialization**: Parse YAML input into C# Person objects with automatic type conversion.
- **Field validation**: Only fields present in the source code or YAML are included in the output (no empty values).
- **Interactive editing**: Edit C# code or YAML in real-time and see instant conversion results.
- **Error handling**: Clear error messages displayed in code input fields when validation fails or parsing errors occur.

**Technical highlights:**

- Uses `SerializerBuilder` with `CamelCaseNamingConvention` for YAML serialization.
- Uses `DeserializerBuilder` with `UnderscoredNamingConvention` for YAML deserialization.
- Demonstrates parsing C# object initialization code and converting to YAML.
- Shows how to format deserialized objects back to C# code representation.
- Includes validation logic to ensure only existing fields are serialized/deserialized.
- Presents a split-pane layout with code editors and output display.
- Error messages displayed through CodeInput components for consistent UX.

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd packages-demos/yamldotnet
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
   cd packages-demos/yamldotnet
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
   This publishes the YamlDotNet demo with one command.

## Learn More

- YamlDotNet documentation: [github.com/aaubry/YamlDotNet](https://github.com/aaubry/YamlDotNet)
- Ivy documentation: [docs.ivy.app](https://docs.ivy.app)
