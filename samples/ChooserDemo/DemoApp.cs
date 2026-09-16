using System.ComponentModel;
using BrighterTools.MauiColourChooser;
namespace ChooserDemo;
public sealed class DemoColour : INotifyPropertyChanged
{
    private Color colour = Colors.CornflowerBlue;
    public Color Colour { get => colour; set { if (colour == value) return; colour = value; PropertyChanged?.Invoke(this, new(nameof(Colour))); } }
    public event PropertyChangedEventHandler? PropertyChanged;
}
public sealed class DemoApp : Application
{
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var firstModel = new DemoColour();
        var secondModel = new DemoColour { Colour = Colors.Orange };
        var first = new ColourChooser();
        var second = new ColourChooser();
        first.SetBinding(ColourChooser.SelectedColourProperty, new Binding(nameof(DemoColour.Colour), BindingMode.TwoWay, source: firstModel));
        second.SetBinding(ColourChooser.SelectedColourProperty, new Binding(nameof(DemoColour.Colour), BindingMode.TwoWay, source: secondModel));
        var swatch = new BoxView { HeightRequest = 36 };
        swatch.SetBinding(BoxView.ColorProperty, new Binding(nameof(DemoColour.Colour), source: firstModel));
        var reset = new Button { Text = "Set first colour to blue externally" };
        reset.Clicked += (_, _) => firstModel.Colour = Colors.Blue;
        var theme = new Picker { ItemsSource = new[] { "System", "Light", "Dark" }, SelectedIndex = 0, Title = "Appearance" };
        theme.SelectedIndexChanged += (_, _) => UserAppTheme = theme.SelectedIndex switch { 1 => AppTheme.Light, 2 => AppTheme.Dark, _ => AppTheme.Unspecified };
        var stack = new VerticalStackLayout { Padding = 16, Spacing = 12, Children = { theme, new Label { Text = "First chooser (two-way binding)" }, swatch, reset, first,
            new Label { Text = "Independent second chooser" }, second, new Label { Text = "Standalone wheel" }, new ColourWheel { SelectedColour = Colors.Lime } } };
        var page = new ContentPage { Title = "Colour chooser demo", Content = new ScrollView { Content = stack } };
        page.SetAppThemeColor(ContentPage.BackgroundColorProperty, Colors.White, Color.FromArgb("#202020"));
        bool tested = false;
        page.Loaded += async (_, _) =>
        {
            if (tested || !Environment.GetCommandLineArgs().Contains("--self-test")) return;
            tested = true;
            await Task.Delay(300);
            await SmokeChecks.RunAsync(first, second, firstModel, secondModel);
            Dispatcher.Dispatch(() => CloseWindow(page.Window!));
        };
        return new Window(page) { Width = 440, Height = 800, MinimumWidth = 340, MinimumHeight = 480 };
    }
}
