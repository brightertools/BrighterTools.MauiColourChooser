# Building and validation

## Toolchain

Install .NET SDK 10.0.303, then `pwsh ./eng/bootstrap.ps1`. The script pins workload set 10.0.303.1. NuGet.config isolates this repository from any parent private/offline feeds.

Windows release packaging compiles both the Windows and Mac Catalyst library targets. Windows `-t:Compile` checks for Mac validate managed code only. Full Mac application builds require macOS, the workload-compatible Xcode installation and any necessary signing setup.

## Automated checks

```powershell
dotnet test tests/BrighterTools.ColourMath.Tests/BrighterTools.ColourMath.Tests.csproj -c Release
dotnet build samples/ChooserDemo/ChooserDemo.csproj -f net10.0-windows10.0.19041.0
pwsh ./eng/test-demo.ps1
dotnet build samples/ChooserDemo/ChooserDemo.csproj -f net10.0-maccatalyst -t:Compile -p:EnableCodeSigning=false
pwsh ./eng/pack.ps1
```

The demo self-test needs an interactive Windows desktop. It writes chooser-smoke.json alongside the executable; the wrapper checks freshness and Passed, since the application can exit successfully after recording a failed check. Do not equate process exit code with test success.

CI runs maths tests, builds the Windows demo, checks Mac managed compilation and verifies packages. Hosted CI does not establish pointer/keyboard behaviour or native Mac support.

## Desktop acceptance

- Use two independently bound instances, then deliberately bind two controls to one model.
- Load dark/grey/black colours; confirm exact RGB/hex survives the wheel's full-brightness reset.
- Edit brightness, wheel, square, hue strip, saturation strip and RGB/HSV/hex in turn; verify synchronization.
- Test incomplete/invalid entries, switching views, collapsing sliders and recovery via valid hex.
- Try 100%, 125%, 150%, 175%, 200% scaling, mixed monitors and moving/resizing between them.
- On Mac, test Retina/non-Retina and scaled modes with real pointer/keyboard input.
- Check System/Light/Dark, keyboard focus, accessibility names and enlarged text.
- Ensure no alpha or wide-gamut/HDR accuracy is implied; this editor represents opaque 8-bit RGB.
- Install both local packages into a clean MAUI consumer, register UseSkiaSharp, and render the control.

Record the OS, SDK/workloads, hardware and results in release notes or the release PR. Outstanding real-Mac checks must remain visible.

## Package contents

The release script verifies MIT/repository metadata, README/LICENSE, symbol packages and the Windows/Mac assemblies. No fonts or host branding should appear in either archive. SourceLink comes from the .NET SDK and repository metadata; package from a Git checkout with the correct origin.
