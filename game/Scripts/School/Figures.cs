using System;
using Godot;

namespace BlueprintSchool.School;

/// <summary>
/// Flat-shaded people in the game's isometric style, drawn front-on at a fixed size (about 1.8 m
/// tall for an adult). All positions are in world (screen) pixels. The people themselves are in People.cs.
/// </summary>
public static partial class Figures
{
    private static Vector2[] Ellipse(Vector2 centre, float rx, float ry, int n = 18)
    {
        var points = new Vector2[n];
        for (var i = 0; i < n; i++)
        {
            var a = i * Mathf.Tau / n;
            points[i] = centre + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
        }

        return points;
    }

    /// <summary>A filled rectangle with rounded corners.</summary>
    public static void RoundedRect(CanvasItem c, Rect2 r, float radius, Color colour)
    {
        radius = Math.Min(radius, Math.Min(r.Size.X, r.Size.Y) / 2);
        var points = new Vector2[20];
        var centres = new[]
        {
            r.Position + new Vector2(r.Size.X - radius, radius),
            r.End - new Vector2(radius, radius),
            r.Position + new Vector2(radius, r.Size.Y - radius),
            r.Position + new Vector2(radius, radius),
        };
        var i = 0;
        for (var corner = 0; corner < 4; corner++)
        {
            for (var k = 0; k < 5; k++)
            {
                var a = (-Mathf.Pi / 2) + (corner * Mathf.Pi / 2) + (k * Mathf.Pi / 8);
                points[i++] = centres[corner] + new Vector2(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius);
            }
        }

        c.DrawColoredPolygon(points, colour);
    }

    private static Color A(Color colour, float alpha) => new(colour, colour.A * alpha);
}
