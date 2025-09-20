# DiffEngine × Ivy (example)

An Ivy demo that shows how to launch and kill external diff tools using the [DiffEngine](https://github.com/VerifyTests/DiffEngine) NuGet package.

## What it shows
- **Text diff**: writes both sides to temp and opens your installed diff tool.
- **File diff**: copies two file paths to temp (so originals aren’t touched) and opens the diff tool.
- **Kill Last Diff**: closes the last launched diff session (WinMerge / KDiff3 / Meld / VS Code, etc. depending on your machine).

## Prerequisites
- .NET 8 SDK
- A diff tool installed (e.g. [WinMerge](https://winmerge.org/), VS Code, Meld, KDiff3). DiffEngine will detect what you have.

## Run
```bash
dotnet restore
dotnet run -c Release
