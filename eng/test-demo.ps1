param([ValidateSet('Debug','Release')][string]$Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
if (!$IsWindows) { throw 'This wrapper requires an interactive Windows desktop.' }
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$exe = Join-Path $root "samples/ChooserDemo/bin/$Configuration/net10.0-windows10.0.19041.0/win-x64/ChooserDemo.exe"
$started = Get-Date
$process = Start-Process -FilePath $exe -ArgumentList '--self-test' -WindowStyle Hidden -PassThru
if (!$process.WaitForExit(45000)) { throw 'Demo self-test timed out.' }
$report = Get-Item (Join-Path (Split-Path $exe) 'chooser-smoke.json')
if ($report.LastWriteTime -lt $started) { throw 'Demo self-test did not produce a fresh report.' }
$result = Get-Content $report.FullName -Raw | ConvertFrom-Json
if (!$result.Passed) { throw "Demo self-test failed: $($result.Error)" }
Write-Host "$($result.Checks.Count) demo runtime/rendering checks passed."
