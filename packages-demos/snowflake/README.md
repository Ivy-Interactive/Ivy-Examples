# SnowflakeExample 

Web application created using [Ivy](https://github.com/Ivy-Interactive/Ivy). 

Ivy is a web framework for building interactive web applications using C# and .NET.

## Configuration

The app reads settings from `appsettings.json` (overridable via environment variables):
- `Snowflake:Account` – Your Snowflake account identifier
- `Snowflake:User` – Your Snowflake username
- `Snowflake:Password` – Your Snowflake password
- `Snowflake:Warehouse` – Snowflake warehouse name (default: `COMPUTE_WH`)
- `Snowflake:Database` – Database name (default: `SNOWFLAKE_SAMPLE_DATA`)
- `Snowflake:Schema` – Schema name (default: `TPCH_SF1`)

## Setting Up Credentials

Before running the application, you need to configure your Snowflake credentials. For better security, especially in production, use environment variables instead of storing credentials in a file.

**Windows PowerShell:**
```powershell
$env:Snowflake__Account = "your_account_here"
$env:Snowflake__User = "your_username_here"
$env:Snowflake__Password = "your_password_here"
```

**Windows Command Prompt:**
```cmd
set Snowflake__Account=your_account_here
set Snowflake__User=your_username_here
set Snowflake__Password=your_password_here
```

**Linux/macOS:**
```bash
export Snowflake__Account="your_account_here"
export Snowflake__User="your_username_here"
export Snowflake__Password="your_password_here"
```

> **Note:** The double underscore `__` in the environment variable corresponds to the colon `:` in configuration (i.e., `Snowflake__Account` → `Snowflake:Account`)

> **Important:** Never publish credentials in public repositories or share them with unauthorized parties.

## Run

```
dotnet watch
```

## Deploy

```
ivy deploy
```