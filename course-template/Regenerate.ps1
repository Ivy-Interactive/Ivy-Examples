$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
Remove-Item -Path "${scriptDir}\Generated" -Recurse -Force -ErrorAction SilentlyContinue
dotnet run --project "${scriptDir}\Helpers\Generator\Generator.csproj" -- convert "${scriptDir}\Modules\*.md" "${scriptDir}\Generated" "${scriptDir}\course-template.csproj"