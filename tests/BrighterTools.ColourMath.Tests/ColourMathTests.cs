using BrighterTools.ColourMath;
using Maths = BrighterTools.ColourMath.ColourMath;
using Xunit;
namespace BrighterTools.ColourMath.Tests;

public class ColourMathTests
{
    [Theory]
    [InlineData(0, "FF0000")][InlineData(60, "FFFF00")][InlineData(120, "00FF00")]
    [InlineData(180, "00FFFF")][InlineData(240, "0000FF")][InlineData(300, "FF00FF")]
    [InlineData(360, "FF0000")][InlineData(-60, "FF00FF")][InlineData(720, "FF0000")]
    public void Hue_sectors_and_wrap_are_exact(double hue, string hex) => Assert.Equal(hex, Maths.ToRgb(new(hue, 1, 1)).Hex);

    [Fact] public void Rgb_roundtrips_across_the_colour_cube()
    {
        for (int r = 0; r <= 255; r += 17)
        for (int g = 0; g <= 255; g += 17)
        for (int b = 0; b <= 255; b += 17)
        {
            var rgb = new RgbColour((byte)r, (byte)g, (byte)b);
            Assert.Equal(rgb, Maths.ToRgb(Maths.ToHsv(rgb)));
        }
    }
    [Fact] public void Hue_and_saturation_survive_black_and_hue_survives_grey()
    {
        var black = Maths.ToHsv(new(0, 0, 0), new(217, .65, .5));
        Assert.Equal(new HsvColour(217, .65, 0), black);
        var grey = Maths.ToHsv(new(128, 128, 128), black);
        Assert.Equal(217, grey.H); Assert.Equal(0, grey.S);
        Assert.Equal("808080", Maths.ToRgb(grey).Hex);
    }
    [Fact] public void Rounding_is_to_the_nearest_opaque_byte()
    {
        Assert.Equal("808080", Maths.ToRgb(new(0, 0, .5)).Hex);
        Assert.Equal("000000", Maths.ToRgb(new(double.NaN, double.NaN, double.NaN)).Hex);
    }
    [Theory][InlineData("#aB12fF")][InlineData("AB12FF")][InlineData(" ab12ff ")]
    public void Hex_accepts_six_digits_and_normalizes(string text)
    { Assert.True(Maths.TryHex(text, out var rgb)); Assert.Equal("AB12FF", rgb.Hex); }
    [Theory][InlineData("")][InlineData("#123")][InlineData("##123456")][InlineData("12345678")][InlineData("12GG34")][InlineData("  12345")]
    public void Invalid_hex_is_not_a_colour(string text) => Assert.False(Maths.TryHex(text, out _));
    [Theory][InlineData("-1")][InlineData("256")][InlineData("12.5")][InlineData("NaN")][InlineData("")]
    public void Rgb_input_requires_an_in_range_integer(string text) => Assert.False(Maths.TryNumber(text, 255, true, out _));

    [Fact] public void Wheel_marker_and_hit_test_agree_at_all_scales()
    {
        foreach (double scale in new[] { 1, 1.25, 1.5, 1.75, 2, 2.5 })
        foreach (double hue in new[] { 0, 60, 90, 180, 240, 359 })
        foreach (double saturation in new[] { .1, .5, 1 })
        {
            var colour = new HsvColour(hue, saturation, .7);
            var point = Maths.WheelMarker(colour);
            double radius = 100 * scale;
            var hit = Maths.WheelAt(point.X * radius / radius, point.Y * radius / radius, colour);
            Assert.Equal(colour.H, hit.H, 8); Assert.Equal(colour.S, hit.S, 8); Assert.Equal(colour.V, hit.V);
        }
        Assert.Equal(123, Maths.WheelAt(0, 0, new(123, 1, 1)).H);
        Assert.Equal(1, Maths.WheelAt(2, 0, new(0, .5, 1)).S);
    }
    [Fact] public void Expanded_whites_have_a_neutral_centre_and_more_low_saturation_space()
    {
        foreach (double radius in new[] { 0, .02, .06 })
        {
            var hsv = Maths.WheelAt(radius, 0, new(210, 1, 1), WheelSaturationCurve.ExpandedWhites);
            Assert.Equal("FFFFFF", Maths.ToRgb(hsv).Hex);
            Assert.Equal(210, hsv.H);
        }
        Assert.Equal("808080", Maths.ToRgb(Maths.WheelAt(.04, 0, new(210, 1, .5), WheelSaturationCurve.ExpandedWhites)).Hex);
        Assert.True(Maths.SaturationAtRadius(.5, WheelSaturationCurve.ExpandedWhites) < .25);
        Assert.Equal(1, Maths.SaturationAtRadius(1, WheelSaturationCurve.ExpandedWhites));
    }
    [Fact] public void All_curves_have_matching_marker_and_pointer_geometry()
    {
        foreach (var curve in Enum.GetValues<WheelSaturationCurve>())
        foreach (double saturation in new[] { 0, .0001, .01, .1, .5, 1 })
        {
            var hsv = new HsvColour(213, saturation, 1);
            var marker = Maths.WheelMarker(hsv, curve);
            var sampled = Maths.WheelAt(marker.X, marker.Y, hsv, curve);
            Assert.Equal(hsv.H, sampled.H, 8); Assert.Equal(hsv.S, sampled.S, 8);
        }
    }
    [Fact] public void Saturation_ramps_are_monotonic_and_keep_the_original_linear_mode()
    {
        double previous = 0;
        for (int i = 0; i <= 100; i++)
        {
            double radius = i / 100d;
            double expanded = Maths.SaturationAtRadius(radius, WheelSaturationCurve.ExpandedWhites);
            Assert.InRange(expanded, previous, 1); previous = expanded;
            Assert.InRange(Maths.SaturationAtRadius(radius, WheelSaturationCurve.Balanced), expanded, radius);
            Assert.Equal(radius, Maths.SaturationAtRadius(radius, WheelSaturationCurve.Linear));
        }
    }
    [Fact] public void Compact_whites_compress_only_the_inner_region_and_remain_monotonic()
    {
        double previous = 0;
        for (int i = 0; i <= 1000; i++)
        {
            double radius = i / 1000d;
            double compact = Maths.SaturationAtRadius(radius, WheelSaturationCurve.CompactWhites);
            double balanced = Maths.SaturationAtRadius(radius, WheelSaturationCurve.Balanced);
            Assert.InRange(compact, previous, 1); previous = compact;
            if (radius >= .5) Assert.Equal(balanced, compact);
            else Assert.True(compact >= balanced);
        }
        Assert.True(Maths.SaturationAtRadius(.1, WheelSaturationCurve.CompactWhites) > Maths.SaturationAtRadius(.1, WheelSaturationCurve.Balanced));
        double leftSlope = (Maths.SaturationAtRadius(.5, WheelSaturationCurve.CompactWhites) - Maths.SaturationAtRadius(.49999, WheelSaturationCurve.CompactWhites)) / .00001;
        double rightSlope = (Maths.SaturationAtRadius(.50001, WheelSaturationCurve.CompactWhites) - Maths.SaturationAtRadius(.5, WheelSaturationCurve.CompactWhites)) / .00001;
        Assert.InRange(Math.Abs(leftSlope - rightSlope), 0, .001);
    }
    [Fact] public void Saturation_bar_matches_the_square_row_and_preserves_hue_and_brightness()
    {
        foreach (double hue in new[] { 0d, 60, 120, 210, 359 })
        foreach (double brightness in new[] { 0d, .3, .5, 1 })
        foreach (double x in new[] { -1d, 0, .1, .5, 1, 2 })
        {
            var source = new HsvColour(hue, .4, brightness);
            var row = Maths.SaturationRowAt(x, source);
            Assert.Equal(hue, row.H); Assert.Equal(brightness, row.V);
            Assert.Equal(Maths.Unit(x), row.S);
            Assert.Equal(Maths.ToRgb(Maths.SquareAt(x, 1 - brightness, hue)), Maths.ToRgb(row));
        }
        Assert.Equal("FFFFFF", Maths.ToRgb(Maths.SaturationRowAt(0, new(210, .4, 1))).Hex);
        Assert.Equal("808080", Maths.ToRgb(Maths.SaturationRowAt(0, new(210, .4, .5))).Hex);
    }
    [Fact] public void Square_edges_are_white_hue_and_black()
    {
        Assert.Equal("FFFFFF", Maths.ToRgb(Maths.SquareAt(0, 0, 120)).Hex);
        Assert.Equal("00FF00", Maths.ToRgb(Maths.SquareAt(1, 0, 120)).Hex);
        Assert.Equal("000000", Maths.ToRgb(Maths.SquareAt(1, 1, 120)).Hex);
        Assert.Equal(new HsvColour(120, 0, 0), Maths.SquareAt(-1, 2, 120));
    }
}
