using BrighterTools.ColourMath;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Maths = BrighterTools.ColourMath.ColourMath;
namespace BrighterTools.MauiColourChooser;

internal enum SurfaceKind { Wheel, Square, Hue, Saturation }
internal sealed class ColourSurface : SKCanvasView
{
    private readonly SurfaceKind kind;
    private HsvColour hsv = new(0, 1, 1);
    private SKBitmap? cache;
    private (int W, int H, double Component, WheelSaturationCurve Curve) cacheKey;
    private WheelSaturationCurve curve = WheelSaturationCurve.CompactWhites;
    public void SetCurve(WheelSaturationCurve value) { curve = value; InvalidateSurface(); }
    private double displayBrightness = 1;
    public void SetDisplayBrightness(double value) { displayBrightness = Maths.Unit(value); InvalidateSurface(); }
    private bool dragging;
    public event EventHandler<HsvColour>? Edited;
    public ColourSurface(SurfaceKind kind)
    {
        this.kind = kind;
        EnableTouchEvents = true;
        HeightRequest = kind is SurfaceKind.Hue or SurfaceKind.Saturation ? 32 : 210;
        SemanticProperties.SetDescription(this, kind == SurfaceKind.Wheel ? "Hue and saturation wheel. Its display brightness is controlled separately. Use the numeric controls for keyboard selection." : kind == SurfaceKind.Square ? "Saturation and brightness square" : kind == SurfaceKind.Saturation ? "Saturation gradient at the selected hue, shown from white to full colour. Selected brightness is controlled separately. Use the HSV saturation control for keyboard selection." : "Rainbow hue strip");
    }
    public void SetColour(HsvColour value) { hsv = value; InvalidateSurface(); }
    private SKRect SurfaceBounds(int w, int h)
    {
        float margin = 7 * (float)(w / Math.Max(1, Width));
        if (kind != SurfaceKind.Wheel) return new(margin, margin, w - margin, h - margin);
        float side = Math.Max(1, Math.Min(w, h) - 2 * margin);
        return SKRect.Create((w - side) / 2, (h - side) / 2, side, side);
    }
    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        var bounds = SurfaceBounds(e.Info.Width, e.Info.Height);
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        int w = Math.Clamp((int)bounds.Width, 2, 512), h = Math.Clamp((int)bounds.Height, 2, 512);
        // Wheel brightness is display state, independent of the loaded RGB. The saturation strip stays white-to-hue.
        double component = kind == SurfaceKind.Wheel ? displayBrightness : kind is SurfaceKind.Square or SurfaceKind.Saturation ? hsv.H : 0;
        if (cache == null || cacheKey != (w, h, component, curve))
        {
            cache?.Dispose();
            cache = new SKBitmap(w, h, SKColorType.Bgra8888, SKAlphaType.Premul);
            var pixels = new SKColor[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                double nx = x / (double)(w - 1), ny = y / (double)(h - 1);
                var colour = kind switch
                {
                    SurfaceKind.Wheel => Maths.WheelAt(nx * 2 - 1, ny * 2 - 1, new(0, 1, displayBrightness), curve),
                    SurfaceKind.Square => Maths.SquareAt(nx, ny, hsv.H),
                    SurfaceKind.Saturation => Maths.SaturationRowAt(nx, hsv with { V = 1 }),
                    _ => new HsvColour(nx * 360, 1, 1)
                };
                var rgb = Maths.ToRgb(colour);
                bool outside = kind == SurfaceKind.Wheel && Math.Pow(nx * 2 - 1, 2) + Math.Pow(ny * 2 - 1, 2) > 1;
                pixels[y * w + x] = outside ? SKColors.Transparent : new(rgb.R, rgb.G, rgb.B);
            }
            cache.Pixels = pixels;
            cacheKey = (w, h, component, curve);
        }
        using var image = SKImage.FromBitmap(cache);
        canvas.DrawImage(image, bounds, new SKSamplingOptions(SKFilterMode.Linear));
        var marker = kind == SurfaceKind.Wheel ? Maths.WheelMarker(hsv, curve) : (0d, 0d);
        float mx = kind == SurfaceKind.Wheel ? bounds.MidX + (float)marker.Item1 * bounds.Width / 2 :
            bounds.Left + (float)(kind == SurfaceKind.Hue ? hsv.H / 360 : hsv.S) * bounds.Width;
        float my = kind == SurfaceKind.Wheel ? bounds.MidY + (float)marker.Item2 * bounds.Height / 2 :
            kind is SurfaceKind.Hue or SurfaceKind.Saturation ? bounds.MidY : bounds.Top + (float)(1 - hsv.V) * bounds.Height;
        float density = (float)(e.Info.Width / Math.Max(1, Width));
        using var paint = new SKPaint { Style = SKPaintStyle.Stroke, IsAntialias = true };
        paint.Color = SKColors.Black; paint.StrokeWidth = 3 * density;
        canvas.DrawCircle(mx, my, 5 * density, paint);
        paint.Color = SKColors.White; paint.StrokeWidth = 1.5f * density;
        canvas.DrawCircle(mx, my, 5 * density, paint);
    }
    protected override void OnTouch(SKTouchEventArgs e)
    {
        base.OnTouch(e);
        var bounds = SurfaceBounds((int)CanvasSize.Width, (int)CanvasSize.Height);
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        double x = (e.Location.X - bounds.Left) / bounds.Width, y = (e.Location.Y - bounds.Top) / bounds.Height;
        if (e.ActionType == SKTouchAction.Pressed)
        {
            dragging = kind == SurfaceKind.Wheel ? Math.Pow(x * 2 - 1, 2) + Math.Pow(y * 2 - 1, 2) <= 1 : bounds.Contains(e.Location);
        }
        if (dragging && e.ActionType is SKTouchAction.Pressed or SKTouchAction.Moved or SKTouchAction.Released)
        {
            hsv = kind switch
            {
                SurfaceKind.Wheel => Maths.WheelAt(x * 2 - 1, y * 2 - 1, hsv, curve),
                SurfaceKind.Square => Maths.SquareAt(x, y, hsv.H),
                SurfaceKind.Saturation => Maths.SaturationRowAt(x, hsv),
                _ => hsv with { H = Maths.Hue(Maths.Unit(x) * 360) }
            };
            Edited?.Invoke(this, hsv); InvalidateSurface(); e.Handled = true;
        }
        if (e.ActionType is SKTouchAction.Released or SKTouchAction.Cancelled) dragging = false;
    }
    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        if (args.NewHandler == null) { cache?.Dispose(); cache = null; dragging = false; }
        base.OnHandlerChanging(args);
    }
}
