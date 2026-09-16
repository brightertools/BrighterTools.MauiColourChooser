param(
    [Parameter(Mandatory)][string]$Directory,
    [Parameter(Mandatory)][string]$Version
)
$ErrorActionPreference = 'Stop'
$expected = @('BrighterTools.ColourMath', 'BrighterTools.MauiColourChooser')
$packages = @(Get-ChildItem -LiteralPath $Directory -Filter '*.nupkg' -File)
if ($packages.Count -ne 2) { throw 'Expected exactly two library packages in the output directory.' }
foreach ($id in $expected) {
    $path = Join-Path $Directory "$id.$Version.nupkg"
    $archive = [IO.Compression.ZipFile]::OpenRead($path)
    try {
        $nuspec = @($archive.Entries | Where-Object FullName -Like '*.nuspec')
        if ($nuspec.Count -ne 1) { throw "Missing or ambiguous nuspec in $path" }
        $reader = [IO.StreamReader]::new($nuspec[0].Open())
        try { [xml]$manifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
        $metadata = $manifest.package.metadata
        if ($metadata.id -ne $id -or $metadata.version -ne $Version) { throw "Unexpected package ID/version: $path" }
        if ($metadata.license.type -ne 'expression' -or $metadata.license.InnerText -ne 'MIT') { throw "MIT metadata missing: $path" }
        if ($metadata.repository.url -ne 'https://github.com/brightertools/BrighterTools.MauiColourChooser') { throw "Repository metadata missing: $path" }
        if ($metadata.readme -ne 'README.md') { throw "README metadata missing: $path" }
        foreach ($file in @('README.md','LICENSE','THIRD_PARTY_NOTICES.md')) {
            if (!$archive.GetEntry($file)) { throw "Missing $file in $path" }
        }
        $names = @($archive.Entries.FullName)
        if ($names | Where-Object { $_ -match '\.(otf|ttf|pfx|p12|key)$' }) { throw "Unexpected font or signing material in $path" }
        $targets = if ($id -eq 'BrighterTools.ColourMath') { @('net10.0/') } else { @('net10.0-windows','net10.0-maccatalyst') }
        foreach ($target in $targets) {
            if (!($names | Where-Object { $_.StartsWith("lib/$target") -and $_.EndsWith("/$id.dll") })) { throw "Missing $target assembly in $path" }
        }
        if ($id -eq 'BrighterTools.MauiColourChooser') {
            $dependencies = @($manifest.SelectNodes("//*[local-name()='dependency' and @id='BrighterTools.ColourMath']"))
            if (!$dependencies.Count -or ($dependencies | Where-Object version -NE $Version)) { throw 'Chooser must depend on the matching maths version.' }
        }
    } finally { $archive.Dispose() }
    $symbols = [IO.Compression.ZipFile]::OpenRead((Join-Path $Directory "$id.$Version.snupkg"))
    try {
        if (!($symbols.Entries | Where-Object FullName -Like '*.pdb')) { throw "Missing symbols for $id" }
    } finally { $symbols.Dispose() }
    Write-Host "Verified $id ${Version}: MIT, metadata, targets and symbols"
}
