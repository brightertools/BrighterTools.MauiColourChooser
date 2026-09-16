using System.Globalization;
namespace BrighterTools.ColourMath;

public readonly record struct RgbColour(byte R, byte G, byte B)
{
    public string Hex => $"{R:X2}{G:X2}{B:X2}";
}
public enum WheelSaturationCurve { Linear, ExpandedWhites, Balanced, CompactWhites }

public readonly record struct HsvColour(double H, double S, double V);

public static class ColourMath
{
    public static double Hue(double value) => double.IsFinite(value) ? ((value % 360) + 360) % 360 : 0;
    public static double Unit(double value) => double.IsFinite(value) ? Math.Clamp(value, 0, 1) : 0;
    public static byte Channel(double value) => (byte)Math.Round(Unit(value) * 255, MidpointRounding.AwayFromZero);
    public static RgbColour ToRgb(HsvColour colour)
    {
        double h = Hue(colour.H) / 60, s = Unit(colour.S), v = Unit(colour.V);
        double c = v * s, x = c * (1 - Math.Abs(h % 2 - 1)), m = v - c;
        var (r, g, b) = (int)h switch
        {
            0 => (c, x, 0d), 1 => (x, c, 0d), 2 => (0d, c, x),
            3 => (0d, x, c), 4 => (x, 0d, c), _ => (c, 0d, x)
        };
        return new(Channel(r + m), Channel(g + m), Channel(b + m));
    }
    // Hue remains meaningful through greys; saturation also survives black.
    public static HsvColour ToHsv(RgbColour colour, HsvColour previous = default)
    {
        double r = colour.R / 255d, g = colour.G / 255d, b = colour.B / 255d;
        double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b)), d = max - min;
        double h = d == 0 ? previous.H : max == r ? 60 * ((g - b) / d) : max == g ? 60 * ((b - r) / d + 2) : 60 * ((r - g) / d + 4);
        return new(Hue(h), max == 0 ? Unit(previous.S) : d / max, max);
    }
    public static bool TryHex(string? text, out RgbColour colour)
    {
        colour = default;
        text = text?.Trim();
        if (text?.StartsWith('#') == true) text = text[1..];
        if (text?.Length != 6 || !text.All(char.IsAsciiHexDigit) ||
            !uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var n)) return false;
        colour = new((byte)(n >> 16), (byte)(n >> 8), (byte)n);
        return true;
    }
    public static bool TryNumber(string? text, double maximum, bool integer, out double value) =>
        double.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
            CultureInfo.CurrentCulture, out value) && double.IsFinite(value) && value >= 0 && value <= maximum && (!integer || value == Math.Truncate(value));

    // ExpandedWhites reserves a small neutral centre and uses a quadratic ramp.
    // At 100% brightness the centre is pure white; at lower brightness it is neutral grey.
    public const double NeutralCentreRadius = .06;
    public const double BalancedCentreRadius = .03;
    public static double SaturationAtRadius(double radius, WheelSaturationCurve curve) => curve switch
    {
        WheelSaturationCurve.Linear => Unit(radius),
        WheelSaturationCurve.CompactWhites => SaturationAtRadius(ExpandInnerRadius(radius), WheelSaturationCurve.Balanced),
        WheelSaturationCurve.Balanced => Math.Pow(Unit((radius - BalancedCentreRadius) / (1 - BalancedCentreRadius)), 1.5),
        _ => Math.Pow(Unit((radius - NeutralCentreRadius) / (1 - NeutralCentreRadius)), 2)
    };
    public static double RadiusAtSaturation(double saturation, WheelSaturationCurve curve) => Unit(saturation) == 0 ? 0 : curve switch
    {
        WheelSaturationCurve.Linear => Unit(saturation),
        WheelSaturationCurve.CompactWhites => ContractInnerRadius(RadiusAtSaturation(saturation, WheelSaturationCurve.Balanced)),
        WheelSaturationCurve.Balanced => BalancedCentreRadius + (1 - BalancedCentreRadius) * Math.Pow(Unit(saturation), 1 / 1.5),
        _ => NeutralCentreRadius + (1 - NeutralCentreRadius) * Math.Sqrt(Unit(saturation))
    };

    // Redistribute only the inner half of the wheel. The warp is monotonic and
    // joins the unchanged outer region with matching slope, avoiding a visible ring.
    private static double ExpandInnerRadius(double radius)
    {
        radius = Unit(radius);
        if (radius >= .5) return radius;
        double remaining = 1 - radius / .5;
        return radius * (1 + 2 * remaining * remaining);
    }
    private static double ContractInnerRadius(double radius)
    {
        if (radius >= .5) return radius;
        double low = 0, high = .5;
        for (int i = 0; i < 40; i++)
        {
            double mid = (low + high) / 2;
            if (ExpandInnerRadius(mid) < radius) low = mid; else high = mid;
        }
        return (low + high) / 2;
    }

    // A horizontal row of the square: saturation varies, hue and brightness stay fixed.
    public static HsvColour SaturationRowAt(double x, HsvColour colour) =>
        new(Hue(colour.H), Unit(x), Unit(colour.V));

    // Coordinates normalized to the wheel radius; red is right, hue increases clockwise.
    public static HsvColour WheelAt(double x, double y, HsvColour previous, WheelSaturationCurve curve = WheelSaturationCurve.CompactWhites)
    {
        double radius = Math.Sqrt(x * x + y * y);
        double saturation = SaturationAtRadius(radius, curve);
        return new(saturation == 0 ? previous.H : Hue(Math.Atan2(y, x) * 180 / Math.PI), saturation, previous.V);
    }
    public static (double X, double Y) WheelMarker(HsvColour colour, WheelSaturationCurve curve = WheelSaturationCurve.CompactWhites) =>
        (Math.Cos(Hue(colour.H) * Math.PI / 180) * RadiusAtSaturation(colour.S, curve), Math.Sin(Hue(colour.H) * Math.PI / 180) * RadiusAtSaturation(colour.S, curve));
    public static HsvColour SquareAt(double x, double y, double hue) => new(Hue(hue), Unit(x), 1 - Unit(y));
}
