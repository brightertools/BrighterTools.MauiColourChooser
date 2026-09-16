using BrighterTools.ColourMath;
using Maths = BrighterTools.ColourMath.ColourMath;
namespace BrighterTools.MauiColourChooser;

public sealed class ColourChangedEventArgs(Color colour) : EventArgs
{
    public Color Colour { get; } = colour;
}
/// <summary>Opaque hue/saturation wheel. DisplayBrightness changes rendering without publishing a selection; Hsv controls selection values.</summary>
public sealed class ColourWheel : ContentView
{
    public static readonly BindableProperty SelectedColourProperty = BindableProperty.Create(nameof(SelectedColour), typeof(Color), typeof(ColourWheel), Colors.Red, BindingMode.TwoWay,
        propertyChanged: (b, _, value) => ((ColourWheel)b).FromColour((Color)value), coerceValue: (_, value) => ColourConvert.Opaque((Color?)value));
    public static readonly BindableProperty HsvProperty = BindableProperty.Create(nameof(Hsv), typeof(HsvColour), typeof(ColourWheel), new HsvColour(0, 1, 1), BindingMode.TwoWay,
        propertyChanged: (b, _, value) => ((ColourWheel)b).FromHsv((HsvColour)value));
    public static readonly BindableProperty SaturationCurveProperty = BindableProperty.Create(nameof(SaturationCurve), typeof(WheelSaturationCurve), typeof(ColourWheel), WheelSaturationCurve.CompactWhites,
        propertyChanged: (b, _, value) => ((ColourWheel)b).surface.SetCurve((WheelSaturationCurve)value));
    public WheelSaturationCurve SaturationCurve { get => (WheelSaturationCurve)GetValue(SaturationCurveProperty); set => SetValue(SaturationCurveProperty, value); }
    /// <summary>Rendered brightness (0-1). Defaults to 1; changing it does not change Hsv, SelectedColour or raise ColourChanged.</summary>
    public static readonly BindableProperty DisplayBrightnessProperty = BindableProperty.Create(nameof(DisplayBrightness), typeof(double), typeof(ColourWheel), 1d,
        propertyChanged: (b, _, value) => ((ColourWheel)b).surface.SetDisplayBrightness((double)value),
        coerceValue: (_, value) => double.IsFinite((double)value) ? Maths.Unit((double)value) : 1d);
    public double DisplayBrightness { get => (double)GetValue(DisplayBrightnessProperty); set => SetValue(DisplayBrightnessProperty, value); }
    private readonly ColourSurface surface = new(SurfaceKind.Wheel);
    private bool updating;
    public Color SelectedColour { get => (Color)GetValue(SelectedColourProperty); set => SetValue(SelectedColourProperty, value); }
    public HsvColour Hsv { get => (HsvColour)GetValue(HsvProperty); set => SetValue(HsvProperty, value); }
    public event EventHandler<ColourChangedEventArgs>? ColourChanged;
    public ColourWheel()
    {
        Content = surface;
        surface.Edited += (_, value) => Hsv = value;
    }
    private void FromColour(Color colour)
    {
        if (updating) return;
        updating = true;
        Hsv = Maths.ToHsv(ColourConvert.Rgb(colour), Hsv);
        surface.SetColour(Hsv);
        updating = false;
        ColourChanged?.Invoke(this, new(SelectedColour));
    }
    private void FromHsv(HsvColour hsv)
    {
        if (updating) return;
        updating = true;
        Hsv = new(Maths.Hue(hsv.H), Maths.Unit(hsv.S), Maths.Unit(hsv.V));
        SelectedColour = ColourConvert.Maui(Maths.ToRgb(Hsv));
        surface.SetColour(Hsv);
        updating = false;
        // Includes changes to remembered hue when RGB remains grey/black.
        ColourChanged?.Invoke(this, new(SelectedColour));
    }
}
internal static class ColourConvert
{
    public static RgbColour Rgb(Color c) => new(Maths.Channel(c.Red), Maths.Channel(c.Green), Maths.Channel(c.Blue));
    public static Color Maui(RgbColour c) => Color.FromRgb(c.R, c.G, c.B);
    public static Color Opaque(Color? c) => c == null ? Colors.Red : Maui(Rgb(c));
}
