namespace BrighterTools.MauiColourChooser;

/// <summary>Connected, keyboard-focusable choices. Optional glyphs are supplied by the host.</summary>
public sealed class ChoiceButtonGroup : ContentView
{
    private readonly Grid row = new() { ColumnSpacing = 0 };
    private readonly List<Button> buttons = [];
    private IList<string> items = Array.Empty<string>();
    private string? iconFont;
    private string[] glyphs = [];
    public string? Title { get; set; }
    public IList<string> ItemsSource
    {
        get => items;
        set { items = value; Build(); }
    }
    public static readonly BindableProperty SelectedIndexProperty = BindableProperty.Create(
        nameof(SelectedIndex), typeof(int), typeof(ChoiceButtonGroup), 0, BindingMode.TwoWay,
        propertyChanged: (b, _, _) => { var group = (ChoiceButtonGroup)b; group.Refresh(); group.SelectedIndexChanged?.Invoke(group, EventArgs.Empty); });
    public int SelectedIndex { get => (int)GetValue(SelectedIndexProperty); set => SetValue(SelectedIndexProperty, value); }
    public event EventHandler? SelectedIndexChanged;
    public ChoiceButtonGroup()
    {
        var border = new Border { Content = row, StrokeThickness = 1, StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 4 } };
        border.Stroke = new SolidColorBrush(Color.FromArgb("#888888"));
        Content = border;
    }
    public void SetIcons(string? fontFamily, params string[] icons)
    {
        iconFont = fontFamily; glyphs = icons; Refresh();
    }
    public void SetCompact(bool compact)
    {
        foreach (var button in buttons) { button.FontSize = compact ? 12 : 14; button.MinimumHeightRequest = compact ? 32 : 36; }
    }
    private void Build()
    {
        row.Children.Clear(); row.ColumnDefinitions.Clear(); buttons.Clear();
        for (int i = 0; i < items.Count; i++)
        {
            int index = i;
            var button = new Button { Text = items[i], Style = new Style(typeof(Button)), CornerRadius = 0, Padding = new Thickness(6, 3),
                MinimumWidthRequest = 0, MinimumHeightRequest = 32, FontSize = 12, BorderWidth = 0, ContentLayout = new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Left, 5) };
            button.Clicked += (_, _) => SelectedIndex = index;
            ToolTipProperties.SetText(button, items[i]);
            row.ColumnDefinitions.Add(new(GridLength.Star)); row.Add(button, i); buttons.Add(button);
        }
        Refresh();
    }
    private void Refresh()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            bool selected = i == SelectedIndex;
            var button = buttons[i];
            button.SetAppThemeColor(Button.BackgroundColorProperty, Color.FromArgb(selected ? "#CCCCCC" : "#EEEEEE"), Color.FromArgb(selected ? "#555555" : "#292929"));
            button.SetAppThemeColor(Button.TextColorProperty, Color.FromArgb("#202124"), Color.FromArgb("#F4F4F4"));
            button.FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None;
            SemanticProperties.SetDescription(button, $"{items[i]}, {(selected ? "selected" : "not selected")}");
            if (!string.IsNullOrEmpty(iconFont) && i < glyphs.Length)
            {
                var source = new FontImageSource { FontFamily = iconFont, Glyph = glyphs[i], Size = 15 };
                source.SetAppThemeColor(FontImageSource.ColorProperty, Color.FromArgb("#202124"), Color.FromArgb("#F4F4F4"));
                button.ImageSource = source;
            }
            else button.ImageSource = null;
        }
    }
}
