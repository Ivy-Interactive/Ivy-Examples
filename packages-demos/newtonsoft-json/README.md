# Newtonsoft.Json 

<img width="1364" height="636" alt="image" src="https://github.com/user-attachments/assets/1e57b532-4289-403c-bda3-48b7271f8c66" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fnewtonsoft-json%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** is a web framework for building interactive web applications using C# and .NET.

## Interactive Example Using Newtonsoft.Json

This example demonstrates JSON serialization and deserialization using the [Newtonsoft.Json library](https://github.com/JamesNK/Newtonsoft.Json) integrated with Ivy.

**What This Application Does:**

- **Source JSON editor**: Edit a sample JSON document (name, email, date, roles)
- **Deserialize to UI**: Load JSON into the form-based User Editor
- **Edit in UI**: Modify fields and roles in the User Editor
- **Serialize to JSON**: Write changes back to the JSON editor
- **Dynamic roles**: Adding a role in Source JSON makes it appear in the Roles selector after Deserialize
- **Validation**: Basic email validation with inline feedback

**Technical Implementation:**

- Uses `JsonConvert.SerializeObject` and `JsonConvert.DeserializeObject<UserData>`
- Two-way state sync between a JSON code editor and a typed `UserData` model
- Split-panel layout with two cards: "Source JSON" and "User Editor"
- Roles are managed as a multi-select; available options update from deserialized JSON (deduplicated, case-insensitive)
- Simple validation and error handling via Ivy services

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd newtonsoft-json
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

1. In the "Source JSON" card, edit the JSON (e.g., add a new role).
2. Click "Deserialize" to load values into the "User Editor".
3. In the "User Editor" card, adjust fields and roles.
4. Click "Serialize" to push changes back to the JSON editor.

## How to Deploy

Deploy this example to Ivy's hosting platform:

1. **Navigate to the example**:
   ```bash
   cd newtonsoft-json
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```

## Learn More

- Newtonsoft.Json: [github.com/JamesNK/Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)