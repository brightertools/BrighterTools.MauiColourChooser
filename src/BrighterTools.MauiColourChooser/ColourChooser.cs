using System.Globalization;
using BrighterTools.ColourMath;
using Maths = BrighterTools.ColourMath.ColourMath;
namespace BrighterTools.MauiColourChooser;

public enum ColourChooserLayoutDensity { Comfortable, Compact }

public enum ColourChooserMode { Wheel, Square }

/// <summary>Reusable opaque RGB/HSV editor. The host owns confirmation, clipboard and history.</summary>
public sealed class ColourChooser : ContentView
{
    public static readonly BindableProperty SelectedColourProperty = BindableProperty.Create(nameof(SelectedColour), typeof(Color), typeof(ColourChooser), Colors.Red, BindingMode.TwoWay,
        propertyChanged: (b, _, value) => ((ColourChooser)b).ExternalColour((Color)value), coerceValue: (_, value) => ColourConvert.Opaque((Color?)value));
    public static readonly BindableProperty VisualModeProperty = BindableProperty.Create(nameof(VisualMode), typeof(ColourChooserMode), typeof(ColourChooser), ColourChooserMode.Wheel, BindingMode.TwoWay,
        propertyChanged: (b, _, value) => ((ColourChooser)b).visualMode.SelectedIndex = (int)(ColourChooserMode)value);
    public ColourChooserMode VisualMode { get => (ColourChooserMode)GetValue(VisualModeProperty); set => SetValue(VisualModeProperty, value); }
    public static readonly BindableProperty SaturationCurveProperty = BindableProperty.Create(nameof(SaturationCurve), typeof(WheelSaturationCurve), typeof(ColourChooser), WheelSaturationCurve.CompactWhites,
        propertyChanged: (b, _, value) => ((ColourChooser)b).wheel.SaturationCurve = (WheelSaturationCurve)value);
    public WheelSaturationCurve SaturationCurve { get => (WheelSaturationCurve)GetValue(SaturationCurveProperty); set => SetValue(SaturationCurveProperty, value); }
    private static readonly BindablePropertyKey IsInputValidPropertyKey = BindableProperty.CreateReadOnly(nameof(IsInputValid), typeof(bool), typeof(ColourChooser), true);
    public static readonly BindableProperty IsInputValidProperty = IsInputValidPropertyKey.BindableProperty;
    public Color SelectedColour { get => (Color)GetValue(SelectedColourProperty); set => SetValue(SelectedColourProperty, value); }
    public bool IsInputValid => (bool)GetValue(IsInputValidProperty);
    public event EventHandler<ColourChangedEventArgs>? ColourChanged;

    private readonly ColourWheel wheel = new();
    private readonly ColourSurface square = new(SurfaceKind.Square);
    private readonly ColourSurface hue = new(SurfaceKind.Hue);
    private readonly ColourSurface saturationRow = new(SurfaceKind.Saturation);
    private readonly Label saturationLabel = TextLabel("Saturation");
    private readonly ChoiceButtonGroup visualMode = new() { Title = "Visual selector", ItemsSource = new[] { "Wheel", "Square" }, SelectedIndex = 0 };
    private readonly ChoiceButtonGroup numericMode = new() { Title = "Colour values", ItemsSource = new[] { "RGB", "HSV" }, SelectedIndex = 0 };
    private readonly Slider brightness = new() { Minimum = 0, Maximum = 100, Value = 100 };
    private readonly Entry hex = new ChooserEntry() { MaxLength = 7, Placeholder = "#FF0000", Text = "FF0000" };
    private readonly Label error = TextLabel("");
    private readonly Label brightnessLabel = TextLabel("Brightness");
    private readonly Entry[] entries = new Entry[3];
    private readonly Slider[] sliders = new Slider[3];
    private readonly Label[] labels = new Label[3];
    private HsvColour hsv = new(0, 1, 1);
    private RgbColour rgb = new(255, 0, 0);
    private bool updating;
    // Pending wheel value resets for a newly loaded colour without modifying its authoritative RGB/HSV.
    private double wheelBrightness = 1;


    private readonly Grid modes = new() { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 8 };
    private readonly Grid visuals = new();
    private readonly Grid body = new() { ColumnSpacing = 8, RowSpacing = 4 };
    private readonly VerticalStackLayout visualColumn = new() { Spacing = 4 };
    private readonly VerticalStackLayout editorColumn = new() { Spacing = 4 };
    private readonly VerticalStackLayout editorPanel = new() { Spacing = 4 };
    private readonly Button slidersToggle = new() { Padding = new Thickness(6, 0), MinimumHeightRequest = 24, FontSize = 12, HorizontalOptions = LayoutOptions.Start };
    /// <summary>Whether brightness and numeric sliders are shown. Collapsing preserves draft text and colour.</summary>
    public static readonly BindableProperty AreSlidersExpandedProperty = BindableProperty.Create(
        nameof(AreSlidersExpanded), typeof(bool), typeof(ColourChooser), false, BindingMode.TwoWay,
        propertyChanged: (b, _, _) => ((ColourChooser)b).UpdateSliderExpansion());
    public bool AreSlidersExpanded
    {
        get => (bool)GetValue(AreSlidersExpandedProperty);
        set => SetValue(AreSlidersExpandedProperty, value);
    }
    private void UpdateSliderExpansion()
    {
        bool beside = sideBySide == true;
        editorColumn.IsVisible = beside || AreSlidersExpanded;
        slidersToggle.IsVisible = !beside;
        slidersToggle.Text = AreSlidersExpanded ? "▾ Sliders" : "▸ Sliders";
        SemanticProperties.SetDescription(slidersToggle, AreSlidersExpanded ? "Sliders expanded. Collapse sliders" : "Sliders collapsed. Expand sliders");
        ToolTipProperties.SetText(slidersToggle, AreSlidersExpanded ? "Hide brightness and RGB/HSV controls" : "Show brightness and RGB/HSV controls");
    }
    private readonly VerticalStackLayout layout = new() { Spacing = 8 };
    private readonly Grid hexRow = new() { ColumnSpacing = 4 };
    private readonly Label hexLabel = TextLabel("RGB hex");
    private readonly Grid[] numericRows = new Grid[3];
    private bool layoutReady;
    private bool? sideBySide;
    public static readonly BindableProperty LayoutDensityProperty = BindableProperty.Create(
        nameof(LayoutDensity), typeof(ColourChooserLayoutDensity), typeof(ColourChooser), ColourChooserLayoutDensity.Comfortable,
        propertyChanged: (b, _, _) => ((ColourChooser)b).ApplyDensity());
    public ColourChooserLayoutDensity LayoutDensity
    {
        get => (ColourChooserLayoutDensity)GetValue(LayoutDensityProperty);
        set => SetValue(LayoutDensityProperty, value);
    }
    /// <summary>Optional host-owned icon font; no licensed assets are included in this library.</summary>
    public void SetSelectorIcons(string fontFamily, string wheelGlyph, string squareGlyph, string rgbGlyph, string hsvGlyph)
    {
        visualMode.SetIcons(fontFamily, wheelGlyph, squareGlyph);
        numericMode.SetIcons(fontFamily, rgbGlyph, hsvGlyph);
    }
    private void ApplyDensity()
    {
        if (!layoutReady) return;
        bool compact = LayoutDensity == ColourChooserLayoutDensity.Compact;
        visualMode.Title = compact ? null : "Visual selector";
        numericMode.Title = compact ? null : "Colour values";
        visualMode.SetCompact(compact);
        numericMode.SetCompact(compact);
        ((ChooserEntry)hex).SetCompact(compact);
        layout.Spacing = compact ? 4 : 8;
        visualColumn.Spacing = editorColumn.Spacing = compact ? 4 : 8;
        brightness.Margin = new Thickness(0, compact ? -4 : 0, 0, 0);
        wheel.HeightRequest = compact ? 160 : 210;
        wheel.Content.HeightRequest = compact ? 160 : 210;
        square.HeightRequest = compact ? 160 : 210;
        saturationRow.HeightRequest = hue.HeightRequest = compact ? 28 : 32;
        foreach (var label in new[] { saturationLabel, brightnessLabel, hexLabel })
        {
            if (compact) label.FontSize = 12;
            else label.ClearValue(Label.FontSizeProperty);
        }
        for (int i = 0; i < 3; i++)
        {
            ((ChooserEntry)entries[i]).SetCompact(compact);
            entries[i].WidthRequest = compact ? 64 : 76;
            numericRows[i].ColumnSpacing = compact ? 4 : 6;
            numericRows[i].ColumnDefinitions[0].Width = compact ? 16 : 26;
            numericRows[i].ColumnDefinitions[2].Width = compact ? 64 : 76;
            if (compact) labels[i].FontSize = 13;
            else labels[i].ClearValue(Label.FontSizeProperty);
        }
        hexRow.RowDefinitions.Clear(); hexRow.ColumnDefinitions.Clear();
        hexRow.ColumnDefinitions.Add(new(compact ? new GridLength(46) : GridLength.Star));
        if (compact) hexRow.ColumnDefinitions.Add(new(GridLength.Star));
        hexRow.RowDefinitions.Add(new(GridLength.Auto));
        if (!compact) hexRow.RowDefinitions.Add(new(GridLength.Auto));
        Grid.SetRow(hex, compact ? 0 : 1); Grid.SetColumn(hex, compact ? 1 : 0);
        sideBySide = null;
        ArrangeColumns();
        UpdateEditorVisibility();
    }
    private void ArrangeColumns()
    {
        if (!layoutReady) return;
        bool beside = LayoutDensity == ColourChooserLayoutDensity.Compact && Width >= 340;
        if (sideBySide == beside) return;
        sideBySide = beside;
        body.ColumnDefinitions.Clear(); body.RowDefinitions.Clear();
        body.ColumnDefinitions.Add(new(beside ? new GridLength(160) : GridLength.Star));
        if (beside) body.ColumnDefinitions.Add(new(GridLength.Star));
        body.RowDefinitions.Add(new(GridLength.Auto));
        if (!beside) body.RowDefinitions.Add(new(GridLength.Auto));
        Grid.SetRow(editorPanel, beside ? 0 : 1);
        Grid.SetColumn(editorPanel, beside ? 1 : 0);
        UpdateSliderExpansion();
    }

    public ColourChooser()
    {
        SemanticProperties.SetDescription(visualMode, "Wheel or square colour selector");
        SemanticProperties.SetDescription(numericMode, "RGB or HSV colour values");
        SemanticProperties.SetDescription(hex, "Six digit RGB hexadecimal colour");
        SemanticProperties.SetDescription(brightness, "Wheel selection brightness from zero to one hundred percent");
        ToolTipProperties.SetText(brightness, "Brightness for the next wheel selection. Loading a colour resets this to 100% without changing that colour until you edit.");
        error.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#B3261E"), Color.FromArgb("#FFB4AB"));

        modes.Add(visualMode); modes.Add(numericMode, 1);

        visuals.Add(wheel); visuals.Add(square);
        visualColumn.Children.Add(visuals);
        visualColumn.Children.Add(saturationLabel); visualColumn.Children.Add(saturationRow); visualColumn.Children.Add(hue);
        editorColumn.Children.Add(brightnessLabel); editorColumn.Children.Add(brightness);
        editorPanel.Children.Add(slidersToggle); editorPanel.Children.Add(editorColumn);
        slidersToggle.Clicked += (_, _) => AreSlidersExpanded = !AreSlidersExpanded;
        UpdateSliderExpansion();
        body.Add(visualColumn); body.Add(editorPanel, 0, 1);
        layout.Children.Add(modes); layout.Children.Add(hexRow); layout.Children.Add(body);
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            labels[i] = TextLabel("");
            labels[i].VerticalOptions = LayoutOptions.Center;
            entries[i] = new ChooserEntry { WidthRequest = 76, Keyboard = Keyboard.Numeric };
            sliders[i] = new Slider { Minimum = 0, Maximum = 255 };
            var row = new Grid { ColumnDefinitions = { new(new GridLength(26)), new(GridLength.Star), new(new GridLength(76)) }, ColumnSpacing = 6 };
            row.Add(labels[i]); row.Add(sliders[i], 1); row.Add(entries[i], 2);
            numericRows[i] = row; editorColumn.Children.Add(row);
            entries[i].TextChanged += (_, _) => ReadNumbers(entries[index]);
            entries[i].Unfocused += (_, _) => { if (IsInputValid) Sync(); };
            sliders[i].ValueChanged += (_, args) =>
            {
                if (updating) return;
                if (numericMode.SelectedIndex == 1)
                    SetHsv(index switch { 0 => hsv with { H = args.NewValue }, 1 => hsv with { S = args.NewValue / 100 }, _ => hsv with { V = args.NewValue / 100 } });
                else
                {
                    byte channel = (byte)Math.Round(args.NewValue, MidpointRounding.AwayFromZero);
                    SetRgb(index switch { 0 => rgb with { R = channel }, 1 => rgb with { G = channel }, _ => rgb with { B = channel } });
                }
            };
        }
        hexLabel.VerticalOptions = LayoutOptions.Center;
        hexRow.Add(hexLabel); hexRow.Add(hex, 0, 1); layout.Children.Add(error);
        Content = layout;
        wheel.ColourChanged += (_, _) => { if (!updating) SetHsv(wheel.Hsv); };
        square.Edited += (_, value) => SetHsv(value);
        hue.Edited += (_, value) => SetHsv(value);
        saturationRow.Edited += (_, value) => SetHsv(value);
        brightness.ValueChanged += (_, args) => { if (!updating) SetHsv(hsv with { V = args.NewValue / 100 }); };
        visualMode.SelectedIndexChanged += (_, _) => { VisualMode = (ColourChooserMode)visualMode.SelectedIndex; UpdateEditorVisibility(); };
        numericMode.SelectedIndexChanged += (_, _) => Sync();
        hex.TextChanged += (_, _) =>
        {
            if (updating) return;
            if (!Maths.TryHex(hex.Text, out var value)) { Invalid("Enter six hexadecimal digits, with an optional #."); return; }
            SetRgb(value, hex);
        };
        hex.Unfocused += (_, _) => { if (IsInputValid) Sync(); };
        layoutReady = true; SizeChanged += (_, _) => ArrangeColumns(); ApplyDensity(); Sync();
    }
    private static Label TextLabel(string text)
    {
        var label = new Label { Text = text };
        label.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#202124"), Color.FromArgb("#F4F4F4"));
        return label;
    }
    /// <summary>Begin a new session: discard incomplete text and reset wheel brightness to 100%, preserving the exact loaded colour, even if unchanged.</summary>
    public void ResetEditing(Color colour) => SetRgb(ColourConvert.Rgb(ColourConvert.Opaque(colour)), resetWheel: true);
    private void ExternalColour(Color colour)
    {
        if (updating) return;
        rgb = ColourConvert.Rgb(colour); hsv = Maths.ToHsv(rgb, hsv); wheelBrightness = 1; Sync();
        ColourChanged?.Invoke(this, new(SelectedColour));
    }
    private void Invalid(string message) { SetValue(IsInputValidPropertyKey, false); error.Text = message; error.IsVisible = true; }
    private void ReadNumbers(Entry origin)
    {
        if (updating) return;
        bool isHsv = numericMode.SelectedIndex == 1;
        var values = new double[3];
        for (int i = 0; i < 3; i++)
        {
            double maximum = isHsv ? i == 0 ? 360 : 100 : 255;
            if (!Maths.TryNumber(entries[i].Text, maximum, !isHsv, out values[i]))
            { Invalid(isHsv ? "Hue: 0–360; saturation and brightness: 0–100%." : "RGB values must be whole numbers from 0 to 255."); return; }
        }
        if (isHsv) SetHsv(new(values[0], values[1] / 100, values[2] / 100), origin);
        else SetRgb(new((byte)values[0], (byte)values[1], (byte)values[2]), origin);
    }
    private void SetRgb(RgbColour value, Entry? origin = null, bool resetWheel = false)
    {
        rgb = value; hsv = Maths.ToHsv(rgb, hsv); wheelBrightness = resetWheel ? 1 : hsv.V; Publish(origin);
    }
    private void SetHsv(HsvColour value, Entry? origin = null)
    {
        hsv = new(Maths.Hue(value.H), Maths.Unit(value.S), Maths.Unit(value.V));
        rgb = Maths.ToRgb(hsv); wheelBrightness = hsv.V; Publish(origin);
    }
    private void Publish(Entry? origin)
    {
        updating = true; SelectedColour = ColourConvert.Maui(rgb); updating = false;
        Sync(origin);
        ColourChanged?.Invoke(this, new(SelectedColour));
    }
    private void UpdateEditorVisibility()
    {
        bool isHsv = numericMode.SelectedIndex == 1, isWheel = visualMode.SelectedIndex == 0;
        wheel.IsVisible = saturationRow.IsVisible = saturationLabel.IsVisible = isWheel;
        brightness.IsVisible = brightnessLabel.IsVisible = isWheel && !(LayoutDensity == ColourChooserLayoutDensity.Compact && isHsv);
        square.IsVisible = hue.IsVisible = !isWheel;
    }
    private void Sync(Entry? origin = null)
    {
        if (updating) return;
        updating = true;
        bool isHsv = numericMode.SelectedIndex == 1;
        UpdateEditorVisibility();
        // The displayed wheel defines the next visual choice. Numeric editors and Square still describe the loaded colour exactly.
        wheel.Hsv = hsv with { V = wheelBrightness };
        wheel.DisplayBrightness = wheelBrightness;
        square.SetColour(hsv); hue.SetColour(hsv);
        saturationRow.SetColour(hsv with { V = wheelBrightness });
        brightness.Value = wheelBrightness * 100;
        string[] names = isHsv ? ["H", "S", "V"] : ["R", "G", "B"];
        string[] descriptions = isHsv ? ["Hue in degrees", "Saturation percent", "Brightness percent"] : ["Red", "Green", "Blue"];
        double[] values = isHsv ? [hsv.H, hsv.S * 100, hsv.V * 100] : [rgb.R, rgb.G, rgb.B];
        for (int i = 0; i < 3; i++)
        {
            labels[i].Text = names[i];
            sliders[i].Maximum = isHsv ? i == 0 ? 360 : 100 : 255;
            sliders[i].Value = values[i];
            SemanticProperties.SetDescription(entries[i], descriptions[i]);
            SemanticProperties.SetDescription(sliders[i], descriptions[i]);
            if (entries[i] != origin) entries[i].Text = values[i].ToString(isHsv ? "0.##" : "0", CultureInfo.CurrentCulture);
        }
        if (hex != origin) hex.Text = rgb.Hex;
        error.Text = ""; error.IsVisible = false;
        SetValue(IsInputValidPropertyKey, true);
        updating = false;
    }
}
