param([string]$Version)
$ErrorActionPreference = 'Stop'
if (!$IsWindows) { throw 'Build release packages on Windows so both Windows and Mac Catalyst targets are included.' }
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($Version)) {
    [xml]$props = Get-Content (Join-Path $root 'Directory.Build.props')
    $Version = [string]$props.Project.PropertyGroup.Version
}
if ($Version -notmatch '^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$') {
    throw 'Version must be a three-part version with an optional prerelease suffix.'
}
$output = Join-Path $root "artifacts/nuget/$Version"
Push-Location $root
try {
    foreach ($project in @('BrighterTools.ColourMath', 'BrighterTools.MauiColourChooser')) {
        dotnet pack (Join-Path $root "src/$project/$project.csproj") -c Release "-p:Version=$Version" -p:EnableCodeSigning=false -o $output
        if ($LASTEXITCODE -ne 0) { throw "Packing $project failed." }
    }
    & (Join-Path $PSScriptRoot 'verify-packages.ps1') -Directory $output -Version $Version
    Write-Host "Local packages: $output"
    if ($env:GITHUB_OUTPUT) {
        "version=$Version" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
        "directory=$output" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    }
} finally { Pop-Location }
