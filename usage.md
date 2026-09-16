# Usage

## Install and initialize

The MAUI package requires a .NET 10 MAUI application targeting Windows and/or Mac Catalyst. It depends on `BrighterTools.ColourMath`, MAUI Controls 10.0.101 and SkiaSharp 4.152.0. Use a compatible MAUI workload.

Register SkiaSharp once in `MauiProgram`:

```csharp
using SkiaSharp.Views.Maui.Controls.Hosting;
builder.UseMauiApp<App>().UseSkiaSharp();
```

Before the first NuGet release, use a project reference or the local feed described in [publishing.md](publishing.md).

## Two-way binding

```csharp
using System.ComponentModel;
using Microsoft.Maui.Graphics;

public sealed class ColourEditorModel : INotifyPropertyChanged
{
    private Color draftColour = Colors.CornflowerBlue;
    public Color DraftColour
    {
        get => draftColour;
        set
        {
            if (draftColour == value) return;
            draftColour = value;
            PropertyChanged?.Invoke(this, new(nameof(DraftColour)));
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}
```

Set the page's binding context to an instance of this model. Add this namespace:
`xmlns:colour="clr-namespace:BrighterTools.MauiColourChooser;assembly=BrighterTools.MauiColourChooser"`.

```xml
<Grid RowDefinitions="*,Auto">
    <ScrollView>
        <colour:ColourChooser x:Name="Chooser"
                              LayoutDensity="Compact"
                              SelectedColour="{Binding DraftColour, Mode=TwoWay}" />
    </ScrollView>
    <Button Grid.Row="1" Text="Use colour"
            IsEnabled="{Binding IsInputValid, Source={x:Reference Chooser}}"
            Clicked="UseColourClicked" />
</Grid>
```

The host may use a ScrollView or resize its window; the component does neither. The host handles Use/Cancel and persistence. Commit only when `Chooser.IsInputValid` is true.

## Main API

| Member | Purpose |
| --- | --- |
| `SelectedColour` | Two-way bindable MAUI `Color`; coerced to opaque 8-bit RGB. Null becomes red. |
| `ColourChanged` | Event carrying `ColourChangedEventArgs.Colour`; reflects valid edits/updates, not a confirmed selection. |
| `IsInputValid` | Read-only bindable validity state. Invalid edits leave the last valid colour unchanged. |
| `VisualMode` | Two-way bindable `ColourChooserMode.Wheel` or `.Square`. |
| `LayoutDensity` | `Comfortable` (default) or `Compact`. |
| `AreSlidersExpanded` | Two-way stacked-editor preference; defaults false. Side-by-side editors always show sliders. |
| `SaturationCurve` | Bindable `WheelSaturationCurve`; defaults `CompactWhites`. |
| `ResetEditing(Color)` | Load a colour and clear unfinished input, including reloading the same colour. |
| `SetSelectorIcons(...)` | Supply optional host-owned icon font/glyphs. |

Use a new binding-context instance for independently editable colours. Sharing one model intentionally shares the selected colour.

## Input validation

RGB fields accept 0-255; hue accepts 0-360 degrees; saturation and value accept 0-100%. Hex accepts six hexadecimal digits and an optional leading `#`, displaying uppercase.

Incomplete/invalid text is kept for correction and shows inline feedback. It does not publish an invalid colour. Valid text or a visual selector resolves it. Changing layout/theme or collapsing sliders preserves unfinished input. Hosts should observe `IsInputValid` to disable confirmation or copying.

Call `ResetEditing` to explicitly start a new editing session:

```csharp
using BrighterTools.MauiColourChooser;

Chooser.ResetEditing(Color.FromArgb("#123456"));
Chooser.VisualMode = ColourChooserMode.Square;
```

The library does not maintain a cancellation snapshot. Keep that in the host if Cancel must restore a previous selection.

## Wheel brightness

Loading an external colour or calling `ResetEditing` resets the wheel's pending brightness to 100% **without changing the selected RGB/hex**. Loading `#123456` therefore keeps that exact selected colour while presenting a bright wheel for a new choice.

The next wheel, saturation-bar or Brightness edit applies that pending brightness. Numeric RGB/HSV/hex and Square edits synchronize brightness with the edited colour. Switching modes/theme/density preserves adjustments. The wheel darkens when Brightness is changed.

The wheel's saturation bar always displays white to the current fully saturated hue. Brightness determines the value of the next selection; the strip itself stays bright. Grey and black retain the previously chosen hue.

## Standalone wheel

```csharp
using BrighterTools.MauiColourChooser;
using BrighterTools.ColourMath;

var wheel = new ColourWheel
{
    Hsv = new HsvColour(210, 0.7, 0.5),
    DisplayBrightness = 0.5
};
wheel.ColourChanged += (_, e) => Console.WriteLine(e.Colour.ToHex());
```

`Hsv` controls selection values (hue in degrees, saturation/value 0-1). `DisplayBrightness` controls only rendering (0-1, default 1); it does not publish a colour. Set both values when the displayed wheel should match selection brightness. `SelectedColour` and `Hsv` are two-way bindable.

The full chooser manages this distinction automatically for its new-colour workflow.

## Gradient, layout and icons

`CompactWhites` compresses the near-white region. Other curves are `Linear`, `Balanced` and `ExpandedWhites`. Rendering, hit testing and marker placement use the same curve.

Compact layout switches to two columns at 340 layout units of available **control** width. Below that it stacks, with sliders collapsed by default and an accessible expander. Comfortable layout remains the default for standalone hosts. Expansion and resizing do not change the colour.

Native inputs follow the host's system/light/dark appearance. Controls have text labels and keyboard alternatives to canvas dragging. Test the final host with enlarged text and its window-sizing policy.

Optional icons require the host to register and license its own font:

```csharp
Chooser.SetSelectorIcons("HostIconFont", wheelGlyph, squareGlyph, rgbGlyph, hsvGlyph);
```

No font assets are included. `ChoiceButtonGroup` can also be used independently with `ItemsSource`, two-way `SelectedIndex` and `SetIcons(fontAlias, glyphs)`.

## Portable maths

```csharp
using BrighterTools.ColourMath;

if (ColourMath.TryHex("#336699", out var rgb))
{
    var hsv = ColourMath.ToHsv(rgb);
    var roundTrip = ColourMath.ToRgb(hsv);
    Console.WriteLine(roundTrip.Hex); // 336699
}
```

`BrighterTools.ColourMath` targets plain .NET 10 and can be consumed without MAUI. Pass the prior HSV value to `ToHsv(rgb, previous)` to retain hue through greys and saturation through black.
