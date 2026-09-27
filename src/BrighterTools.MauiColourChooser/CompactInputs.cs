namespace BrighterTools.MauiColourChooser;

// Native adjustments are local to chooser inputs, never global handler mappings.
internal sealed class ChooserEntry : Entry
{
    private readonly CompactInputStyle style = new();
    private bool compact;
    private readonly bool borderless;
    public ChooserEntry(bool borderless = false)
    {
        this.borderless = borderless;
        HandlerChanged += (_, _) => ApplyStyle();
    }
    private void ApplyStyle()
    {
        style.Apply(this, compact);
        if (!borderless) return;
#if WINDOWS
        if (Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBox textBox)
        {
            textBox.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
            textBox.CornerRadius = new Microsoft.UI.Xaml.CornerRadius(0);
            foreach (var state in new[] { "", "PointerOver", "Focused", "Disabled" })
                textBox.Resources["TextControlBackground" + state] =
                    new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }
#elif MACCATALYST
        if (Handler?.PlatformView is UIKit.UITextField textField)
            textField.BorderStyle = UIKit.UITextBorderStyle.None;
#endif
    }
    public void SetCompact(bool value)
    {
        compact = value;
        if (value) { FontSize = 13; MinimumHeightRequest = 32; }
        else { ClearValue(FontSizeProperty); ClearValue(MinimumHeightRequestProperty); }
        ApplyStyle();
    }
}
internal sealed class ChooserPicker : Picker
{
    private readonly CompactInputStyle style = new();
    private bool compact;
    public ChooserPicker() => HandlerChanged += (_, _) => style.Apply(this, compact);
    public void SetCompact(bool value)
    {
        compact = value;
        if (value) { FontSize = 13; MinimumHeightRequest = 32; }
        else { ClearValue(FontSizeProperty); ClearValue(MinimumHeightRequestProperty); }
        style.Apply(this, value);
    }
}
internal sealed class CompactInputStyle
{
#if WINDOWS
    private Microsoft.UI.Xaml.Controls.Control? current;
    private double minimum;
    private Microsoft.UI.Xaml.Thickness padding;
#endif
    public void Apply(VisualElement view, bool compact)
    {
#if WINDOWS
        if (view.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Control control) return;
        if (current != control) { current = control; minimum = control.MinHeight; padding = control.Padding; }
        control.MinHeight = compact ? 32 : minimum;
        control.Padding = compact ? new Microsoft.UI.Xaml.Thickness(6, 2, 6, 2) : padding;
#endif
    }
}
