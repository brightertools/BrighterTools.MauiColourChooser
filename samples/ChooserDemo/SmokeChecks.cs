using System.Text.Json;
using System.Reflection;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using BrighterTools.MauiColourChooser;
using Microsoft.Maui;
namespace ChooserDemo;
internal static class SmokeChecks
{
    private static SKColor[] RenderGradient(SKCanvasView view)
    {
        // Exercise the production Skia renderer, not a duplicate colour-maths formula.
        ((IView)view).Arrange(new Rect(0, 0, 240, 240));
        var info = new SKImageInfo(240, 240);
        using var surface = SKSurface.Create(info);
        view.GetType().GetMethod("OnPaintSurface", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(view, [new SKPaintSurfaceEventArgs(surface, info)]);
        var bitmap = (SKBitmap)view.GetType().GetField("cache", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
        return bitmap.Pixels;
    }
    public static async Task RunAsync(ColourChooser first, ColourChooser second, DemoColour firstModel, DemoColour secondModel)
    {
        var checks = new List<string>();
        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); checks.Add(message); }
        try
        {
            firstModel.Colour = Colors.Blue;
            Check(first.SelectedColour.ToHex() == Colors.Blue.ToHex(), "External binding updates the chooser");
            first.SelectedColour = Colors.Green;
            Check(firstModel.Colour.ToHex() == Colors.Green.ToHex(), "Chooser updates the two-way bound model");
            Check(second.SelectedColour.ToHex() == Colors.Orange.ToHex() && secondModel.Colour.ToHex() == Colors.Orange.ToHex(), "Instances remain independent");
            var hex = first.GetVisualTreeDescendants().OfType<Entry>().Single(e => SemanticProperties.GetDescription(e) == "Six digit RGB hexadecimal colour");
            var original = first.SelectedColour.ToHex();
            hex.Text = "#12";
            Check(!first.IsInputValid && first.SelectedColour.ToHex() == original, "Incomplete hex preserves the last valid colour");
            hex.Text = "#12abEF";
            Check(first.IsInputValid && first.SelectedColour.ToHex() == "#12ABEF", "Valid hex publishes a normalized colour");
            var red = first.GetVisualTreeDescendants().OfType<Entry>().Single(e => SemanticProperties.GetDescription(e) == "Red");
            red.Text = "999";
            Check(!first.IsInputValid, "Out-of-range RGB is invalid");
            first.ResetEditing(Colors.Red);
            Check(first.IsInputValid && hex.Text == "FF0000", "New editing session clears invalid input");
            var saturationBar = first.GetVisualTreeDescendants().OfType<VisualElement>().Single(e => SemanticProperties.GetDescription(e)?.StartsWith("Saturation gradient at") == true);
            Check(saturationBar.IsVisible, "Saturation row is visible beneath the wheel");
            first.VisualMode = ColourChooserMode.Square;
            Check(!saturationBar.IsVisible, "Saturation row is hidden in Square mode");
            Check(first.VisualMode == ColourChooserMode.Square && !first.GetVisualTreeDescendants().OfType<ColourWheel>().Single().IsVisible && first.SelectedColour.ToHex() == "#FF0000", "Programmatic Square selection preserves the colour and hides the wheel");
            hex.Text = "#";
            first.ResetEditing(Colors.Red);
            first.VisualMode = ColourChooserMode.Square;
            Check(first.IsInputValid && hex.Text == "FF0000", "Reloading the same recent colour clears invalid input in Square");
            first.SaturationCurve = BrighterTools.ColourMath.WheelSaturationCurve.Linear;
            first.SaturationCurve = BrighterTools.ColourMath.WheelSaturationCurve.CompactWhites;
            Check(first.SelectedColour.ToHex() == "#FF0000", "Changing the wheel curve preserves the selected colour");
            first.VisualMode = ColourChooserMode.Wheel;
            Check(saturationBar.IsVisible, "Returning to Wheel restores the saturation row");
            first.SelectedColour = new Color(0, 1, 0, .25f);
            Check(first.SelectedColour.Alpha == 1, "Public colour values are opaque");
            first.VisualMode = ColourChooserMode.Square;
            first.SelectedColour = Color.FromArgb("#030609");
            first.VisualMode = ColourChooserMode.Wheel;
            Check(first.SelectedColour.ToHex() == "#030609", "Returning to Wheel preserves a dark selected colour");
            var wheel = first.GetVisualTreeDescendants().OfType<ColourWheel>().Single();
            var wheelSurface = (SKCanvasView)wheel.Content;
            var wheelBrightnessSlider = first.GetVisualTreeDescendants().OfType<Slider>().Single(p => SemanticProperties.GetDescription(p) == "Wheel selection brightness from zero to one hundred percent");
            var brightWheel = RenderGradient(wheelSurface);
            var brightBar = RenderGradient((SKCanvasView)saturationBar);
            Check(wheelBrightnessSlider.Value == 100 && wheel.DisplayBrightness == 1 && first.SelectedColour.ToHex() == "#030609" && hex.Text == "030609",
                "Loading a dark colour resets wheel controls without changing the selected RGB or hex");
            wheelBrightnessSlider.Value = 40;
            var dimWheel = RenderGradient(wheelSurface);
            Check(wheel.DisplayBrightness == .4 && first.SelectedColour.ToHex() == "#224466" && firstModel.Colour.ToHex() == "#224466",
                "Brightness updates the selected colour without the binding echo resetting wheel controls");
            Check(!dimWheel.SequenceEqual(brightWheel) && dimWheel.Where(p => p.Alpha > 0).Max(p => Math.Max(p.Red, Math.Max(p.Green, p.Blue))) == 102,
                "The cached production wheel renderer darkens to the chosen brightness");
            wheelBrightnessSlider.Value = 0;
            var blackWheel = RenderGradient(wheelSurface);
            Check(first.SelectedColour.ToHex() == "#000000" && blackWheel.Where(p => p.Alpha > 0).All(p => p.Red == 0 && p.Green == 0 && p.Blue == 0),
                "Zero brightness draws a black wheel and selects black");
            var blackBar = RenderGradient((SKCanvasView)saturationBar);
            Check(brightBar.SequenceEqual(blackBar) && blackBar[0] == SKColors.White,
                "The saturation row retains its requested white-to-hue gradient");
            first.ResetEditing(Colors.Black);
            Check(wheelBrightnessSlider.Value == 100 && RenderGradient(wheelSurface).SequenceEqual(brightWheel) && first.SelectedColour.ToHex() == "#000000",
                "Reloading the same black colour restores a bright wheel without changing black");
            var numericForReset = first.GetVisualTreeDescendants().OfType<ChoiceButtonGroup>().Single(p => SemanticProperties.GetDescription(p) == "RGB or HSV colour values");
            numericForReset.SelectedIndex = 1;
            Check(first.GetVisualTreeDescendants().OfType<Entry>().Single(e => SemanticProperties.GetDescription(e) == "Brightness percent").Text == "0",
                "HSV Value continues to describe the actual loaded black, not the reset wheel");
            numericForReset.SelectedIndex = 0;
            wheel.Hsv = wheel.Hsv with { H = 0, S = 1 };
            Check(first.SelectedColour.ToHex() == "#FF0000", "The next wheel edit selects at its reset displayed brightness");
            wheelBrightnessSlider.Value = 50;
            first.VisualMode = ColourChooserMode.Square;
            first.VisualMode = ColourChooserMode.Wheel;
            Check(wheelBrightnessSlider.Value == 50 && wheel.DisplayBrightness == .5 && first.SelectedColour.ToHex() == "#800000",
                "Switching visual modes preserves the current brightness adjustment");
            firstModel.Colour = Color.FromArgb("#123456");
            Check(first.SelectedColour.ToHex() == "#123456" && wheelBrightnessSlider.Value == 100 && wheel.DisplayBrightness == 1,
                "A different external selection resets the wheel without changing exact RGB");
            hex.Text = "#";
            first.ResetEditing(Color.FromArgb("#123456"));
            Check(first.IsInputValid && hex.Text == "123456" && wheelBrightnessSlider.Value == 100,
                "Same-colour session reset clears invalid input and resets wheel controls");
            hex.Text = "203040";
            Check(first.SelectedColour.ToHex() == "#203040" && Math.Abs(wheel.DisplayBrightness - 64 / 255d) < .0001,
                "Deliberate numeric edits synchronize wheel brightness with the edited colour");
            var standalone = new ColourWheel { SelectedColour = Color.FromArgb("#123456") };
            int changes = 0; standalone.ColourChanged += (_, _) => changes++;
            standalone.DisplayBrightness = .2;
            Check(standalone.SelectedColour.ToHex() == "#123456" && changes == 0,
                "Standalone display brightness never changes or publishes the selected colour");
            Check(second.SelectedColour.ToHex() == "#FFA500" && second.GetVisualTreeDescendants().OfType<ColourWheel>().Single().DisplayBrightness == 1,
                "Brightness adjustment and reset remain independent between instances");
            wheel.Hsv = wheel.Hsv with { H = 0, V = .1 };
            var redBar = RenderGradient((SKCanvasView)saturationBar);
            Check(redBar[0] == SKColors.White && redBar[^1] == SKColors.Red,
                "Saturation row updates hue independently of wheel brightness");
            Check(first.LayoutDensity == ColourChooserLayoutDensity.Comfortable && second.LayoutDensity == ColourChooserLayoutDensity.Comfortable,
                "Standalone component defaults to Comfortable");
            hex.Text = "#";
            first.LayoutDensity = ColourChooserLayoutDensity.Compact;
            Check(!first.IsInputValid && hex.Text == "#", "Density changes preserve incomplete input");
            first.ResetEditing(Color.FromArgb("#123456"));
            var originalCompactColour = first.SelectedColour.ToHex();
            first.WidthRequest = 370; first.HorizontalOptions = LayoutOptions.Start;
            await Task.Delay(200);
            var body = (Grid)typeof(ColourChooser).GetField("body", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(first)!;
            Check(body.ColumnDefinitions.Count == 2, "Compact chooser uses two columns at laptop width");
            first.WidthRequest = 310;
            await Task.Delay(200);
            Check(body.ColumnDefinitions.Count == 1 && first.SelectedColour.ToHex() == originalCompactColour,
                "Narrow compact chooser stacks without changing colour");
            var numeric = first.GetVisualTreeDescendants().OfType<ChoiceButtonGroup>().Single(p => SemanticProperties.GetDescription(p) == "RGB or HSV colour values");
            var choices = numeric.GetVisualTreeDescendants().OfType<Button>().ToArray();
            var beforeChoice = first.SelectedColour.ToHex();
            ((IButtonController)choices[1]).SendClicked();
            Check(numeric.SelectedIndex == 1 && first.SelectedColour.ToHex() == beforeChoice, "HSV segment click changes representation without changing colour");
            var brightness = first.GetVisualTreeDescendants().OfType<Slider>().Single(p => SemanticProperties.GetDescription(p) == "Wheel selection brightness from zero to one hundred percent");
            Check(!brightness.IsVisible, "Compact HSV mode hides duplicate brightness control");
            ((IButtonController)choices[0]).SendClicked();
            Check(numeric.SelectedIndex == 0 && choices.All(b => b.ImageSource == null), "Standalone segments work with labelled text without a host icon font");
            Check(brightness.IsVisible && hex.MinimumHeightRequest == 32 && hex.FontSize == 13,
                "Compact RGB mode retains brightness and compact text fields");
            Check(second.LayoutDensity == ColourChooserLayoutDensity.Comfortable, "Compact styling does not affect independent instances");
            var editor = (VerticalStackLayout)typeof(ColourChooser).GetField("editorColumn", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(first)!;
            var toggle = first.GetVisualTreeDescendants().OfType<Button>().Single(b => b.Text == "▸ Sliders");
            Check(!first.AreSlidersExpanded && !editor.IsVisible && toggle.IsVisible, "Stacked sliders start collapsed with an accessible expander");
            first.WidthRequest = 370; first.HorizontalOptions = LayoutOptions.Start;
            await Task.Delay(200);
            Check(editor.IsVisible && !toggle.IsVisible && !first.AreSlidersExpanded, "Side-by-side sliders stay visible without an expander");
            first.WidthRequest = 310;
            await Task.Delay(200);
            Check(!editor.IsVisible && toggle.IsVisible, "Returning to stacked layout restores collapsed preference");
            first.AreSlidersExpanded = true;
            red.Text = "999";
            var beforeCollapse = first.SelectedColour.ToHex();
            ((IButtonController)toggle).SendClicked();
            Check(!first.AreSlidersExpanded && !editor.IsVisible && !first.IsInputValid && red.Text == "999" && first.SelectedColour.ToHex() == beforeCollapse,
                "Collapsing sliders preserves invalid text and the last valid colour");
            first.VisualMode = ColourChooserMode.Square;
            Check(!first.AreSlidersExpanded && !editor.IsVisible && !first.IsInputValid && red.Text == "999",
                "Square retains slider expansion and unfinished input");
            var root = (VerticalStackLayout)first.Content;
            Check(root.Children.IndexOf((IView)hex.Parent) < root.Children.IndexOf(body) && hex.IsVisible,
                "Hex remains above the visual selector outside collapsed sliders");
            hex.Text = "AABBCC";
            Check(first.IsInputValid && first.SelectedColour.ToHex() == "#AABBCC" && !first.AreSlidersExpanded,
                "Hex can resolve an invalid draft while sliders are collapsed");
            first.VisualMode = ColourChooserMode.Wheel;
            Check(!first.AreSlidersExpanded && first.SelectedColour.ToHex() == "#AABBCC",
                "Returning to Wheel preserves the collapsed state and selected colour");
            ((IButtonController)toggle).SendClicked();
            Check(first.AreSlidersExpanded && editor.IsVisible && red.Text == "170",
                "Expanding sliders restores synchronized numeric controls");
            Check(!second.AreSlidersExpanded, "Slider expansion is independent for each chooser");
            foreach (var theme in new[] { AppTheme.Dark, AppTheme.Light, AppTheme.Unspecified })
                Application.Current!.UserAppTheme = theme;
            Check(first.IsInputValid && second.SelectedColour.ToHex() == "#FFA500", "Theme changes preserve independent selections");
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "chooser-smoke.json"), JsonSerializer.Serialize(new { Passed = true, Checks = checks }, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception e)
        {
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "chooser-smoke.json"), JsonSerializer.Serialize(new { Passed = false, Checks = checks, Error = e.ToString() }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
