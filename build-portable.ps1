$ErrorActionPreference = "Stop"
$project = Join-Path $PSScriptRoot "src\PCTemperatureMonitor\PCTemperatureMonitor.csproj"
$out = Join-Path $PSScriptRoot "publish\win-x64"
dotnet publish $project -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $out
Write-Host "Portable build: $out"
