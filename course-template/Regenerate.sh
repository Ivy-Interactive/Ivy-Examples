#!/bin/bash

# Get the directory where this script is located
scriptDir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Remove the Generated directory
rm -rf "${scriptDir}/Generated"

# Run the dotnet command to regenerate files
dotnet run --project "../../Ivy-Framework/Ivy.Docs.Tools/Ivy.Docs.Tools.csproj" -- convert "${scriptDir}/Modules"/*.md "${scriptDir}/Generated" 