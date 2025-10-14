$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
rm -r ${scriptDir}\Generated
dotnet run --project ..\..\Ivy-Framework\Ivy.Docs.Tools\Ivy.Docs.Tools.csproj -- convert ${scriptDir}\Modules\*.md ${scriptDir}\Generated