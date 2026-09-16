# BrighterTools.MauiColourChooser

[![CI](https://github.com/brightertools/BrighterTools.MauiColourChooser/actions/workflows/ci.yml/badge.svg)](https://github.com/brightertools/BrighterTools.MauiColourChooser/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/brightertools/BrighterTools.MauiColourChooser/blob/main/LICENSE)

Reusable .NET MAUI colour selection for **Windows and Mac Catalyst**, licensed under standard MIT. Choose opaque, 8-bit RGB colours with a wheel, saturation/value square, RGB or HSV sliders, numeric fields and hexadecimal input.

The host application owns confirmation buttons, clipboard operations, history, persistence and window layout. The library does not capture the desktop or request screen-recording permission.

## Packages

| Package | Purpose | Target |
| --- | --- | --- |
| `BrighterTools.MauiColourChooser` | `ColourChooser`, standalone `ColourWheel` and `ChoiceButtonGroup` | .NET 10 MAUI, Windows / Mac Catalyst |
| `BrighterTools.ColourMath` | RGB/HSV conversion, hex validation and selector geometry | .NET 10, no UI dependencies |

Current source/package version: **0.9.1**, prepared for the first public release. NuGet publication is a separate maintainer action; these instructions do not imply that this version has already been published. The chooser package automatically depends on the matching maths package.

## Quick start

After publication, install `BrighterTools.MauiColourChooser`. Before publication, build the local packages and add their folder as a NuGet source; see [publishing.md](https://github.com/brightertools/BrighterTools.MauiColourChooser/blob/main/publishing.md).

```sh
dotnet add YourMauiApp.csproj package BrighterTools.MauiColourChooser --version 0.9.1
```

Register SkiaSharp in your application's startup:

```csharp
using SkiaSharp.Views.Maui.Controls.Hosting;

builder.UseMauiApp<App>().UseSkiaSharp();
```

Add the control to a page whose binding context exposes a MAUI `Color DraftColour` property with change notifications:

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:colour="clr-namespace:BrighterTools.MauiColourChooser;assembly=BrighterTools.MauiColourChooser">
    <colour:ColourChooser SelectedColour="{Binding DraftColour, Mode=TwoWay}"
                          LayoutDensity="Compact" />
</ContentPage>
```

See [usage.md](https://github.com/brightertools/BrighterTools.MauiColourChooser/blob/main/usage.md) for complete binding, validation, brightness and standalone-wheel examples.

## Behaviour

- Wheel/Square and RGB/HSV mode switches; six-digit uppercase hex with optional input `#`.
- Two-way colour binding, a colour-changed event and read-only input validity.
- Invalid or incomplete text keeps the last valid colour and shows inline feedback.
- Hue is retained through grey/black selections.
- Theme-aware native inputs and keyboard alternatives to pointer dragging.
- Comfortable layout by default; Compact layout uses two columns at 340 layout units and stacks below that width.
- Expandable numeric sliders, independent control instances and optional host-provided icon fonts.
- No alpha, palettes, named colours, LAB editor or mobile targets in this version.

## Build from source

Install the SDK pinned in `global.json` and PowerShell 7. On Windows, run:

```powershell
pwsh ./eng/bootstrap.ps1
dotnet test tests/BrighterTools.ColourMath.Tests/BrighterTools.ColourMath.Tests.csproj
dotnet build samples/ChooserDemo/ChooserDemo.csproj -f net10.0-windows10.0.19041.0
pwsh ./eng/pack.ps1
```

The bootstrap installs workload set **10.0.303.1** for SDK **10.0.303**. Packages use MAUI Controls **10.0.101** and SkiaSharp **4.152.0**. Workload installation may require administrator privileges.

On macOS, use the same bootstrap and build the demo with `-f net10.0-maccatalyst`; install the Xcode version required by that Apple workload. Source builds on a Mac target Mac Catalyst. Full two-platform NuGet packaging runs on Windows, where both library targets can be built.

The demo requires Windows 10 1809+ or macOS 15.2+. Windows builds/runtime checks and Mac managed compilation have been exercised. Real Mac input, Retina rendering and accessibility remain release acceptance checks; see [testing](https://github.com/brightertools/BrighterTools.MauiColourChooser/blob/main/docs/testing.md).

## Documentation and support

- [Usage and API guide](https://github.com/brightertools/BrighterTools.MauiColourChooser/blob/main/usage.md)
- [NuGet packaging and trusted publishing](https://github.com/brightertools/BrighterTools.MauiColourChooser/blob/main/publishing.md)
- [Release notes](https://github.com/brightertools/BrighterTools.MauiColourChooser/blob/main/RELEASE_NOTES.md)
- [Contributing](https://github.com/brightertools/BrighterTools.MauiColourChooser/blob/main/CONTRIBUTING.md)
- [Report a bug or request a feature](https://github.com/brightertools/BrighterTools.MauiColourChooser/issues)
- [Security reports](https://github.com/brightertools/BrighterTools.MauiColourChooser/blob/main/SECURITY.md)

## License

Copyright (c) 2026 Brighter Tools Limited. [MIT](https://github.com/brightertools/BrighterTools.MauiColourChooser/blob/main/LICENSE), including the standard attribution requirement. Dependencies retain their own licenses; see [third-party notices](https://github.com/brightertools/BrighterTools.MauiColourChooser/blob/main/THIRD_PARTY_NOTICES.md).

No Font Awesome fonts, desktop-picker branding or proprietary assets are included.
