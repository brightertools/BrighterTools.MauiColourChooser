# Requires PowerShell 7 and the SDK in global.json.
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$toolchain = Get-Content (Join-Path $PSScriptRoot 'toolchain.json') | ConvertFrom-Json
Push-Location $root
try {
    if ((dotnet --version) -ne $toolchain.sdk) { throw "Install .NET SDK $($toolchain.sdk) first." }
    if (!$IsWindows -and !$IsMacOS) { throw 'The UI targets require Windows or macOS. Maths tests need only the SDK.' }
    $workloads = if ($IsWindows) { @('maui-windows', 'maccatalyst') } else { @('maui-maccatalyst') }
    dotnet workload install @workloads --version $toolchain.workloadSet
    if ($LASTEXITCODE -ne 0) { throw 'Workload installation failed.' }
    dotnet restore ./BrighterTools.MauiColourChooser.slnx --configfile ./NuGet.config
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }
} finally { Pop-Location }
