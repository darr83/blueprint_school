using System;
using System.Linq;
using Godot;

namespace BlueprintSchool.School;

/// <summary>
/// Detailed drawings for the plain "box" furniture: desks on legs, cupboards with doors and
/// handles, lab benches and workbenches, shelves of books, a fume cupboard, gym mats and so on.
/// Long items are drawn as one-tile slices (so each slice sorts on its own); the slices share
/// faces with their neighbours, which are left out so the item reads as one piece.
/// </summary>
public static partial class FurnitureArt
{
    private enum Kind
    {
        Plain,
        Table,
        Counter,
        Cupboard,
        Shelves,
        Fridge,
        Basin,
        Couch,
        Mat,
    }

    private static Kind KindOf(string id) => id switch
    {
        "office_desk" or "staff_table" or "teacher_desk" or "pupil_desk" or "single_desk" or "computer_desk" or "art_table"
            or "library_table" or "keyboard_desk" => Kind.Table,
        "reception_desk" or "lab_bench" or "workbench" or "cooking_station" => Kind.Counter,
        "storage_cupboard" or "kitchenette" => Kind.Cupboard,
        "fume_cupboard" => Kind.Fridge,
        "bookshelf" => Kind.Shelves,
        "hand_basin" => Kind.Basin,
        "medical_couch" => Kind.Couch,
        "gym_mat" => Kind.Mat,
        _ => Kind.Plain,
    };

    /// <summary>
    /// Draws one slice (sx, sy, sw, sl) of an item whose whole footprint is <paramref name="whole"/>.
    /// <paramref name="joined"/> lists the slice's sides that touch the next slice.
    /// </summary>
    public static void Slice(CanvasItem c, string id, double sx, double sy, double sw, double sl, (double X, double Y, double W, double L) whole, float height, Color colour, Sides joined, Sides front = Sides.None)
    {
        if (Fitted(c, id, sx, sy, sw, sl, whole, height, colour, joined, front))
        {
            return;
        }

        var face = colour.Darkened(0.18f);
        var rim = colour.Lightened(0.3f);
        switch (KindOf(id))
        {
            case Kind.Table:
                Table(c, id, sx, sy, sw, sl, whole, height, colour, joined);
                return;

            case Kind.Couch:
                Couch(c, sx, sy, sw, sl, whole, height, colour, joined);
                return;

            case Kind.Mat:
                IsoDraw.Prism(c, sx, sy, sw, sl, 2, colour, colour.Darkened(0.2f), joined, rim: colour.Darkened(0.25f));
                foreach (var (a, b) in Inset(sx, sy, sw, sl, whole, 0.12))
                {
                    c.DrawLine(a + new Vector2(0, -2), b + new Vector2(0, -2), colour.Lightened(0.3f), 1f, true);
                }

                return;

            case Kind.Counter:
                // A solid counter with a dark plinth and a slightly overhanging worktop.
                IsoDraw.Prism(c, sx, sy, sw, sl, height - 3, colour.Darkened(0.05f), face, joined, skirting: colour.Darkened(0.5f), skirtingHeight: 3);
                IsoDraw.Prism(c, sx - Edge(joined, Sides.West), sy - Edge(joined, Sides.North), sw + Edge(joined, Sides.West) + Edge(joined, Sides.East), sl + Edge(joined, Sides.North) + Edge(joined, Sides.South), 3, colour.Lightened(0.22f), colour.Lightened(0.05f), joined, height - 3, rim: colour.Lightened(0.4f));
                return;
        }

        var kind = KindOf(id);
        IsoDraw.Prism(c, sx, sy, sw, sl, height, colour, face, joined, skirting: kind is Kind.Cupboard or Kind.Basin or Kind.Fridge ? colour.Darkened(0.45f) : null, skirtingHeight: 2, rim: rim);
        var line = colour.Darkened(0.4f);
        foreach (var (a, b, _, _) in IsoDraw.VisibleFaces(sx, sy, sw, sl))
        {
            Vector2 At(float u, float v) => a.Lerp(b, u) + new Vector2(0, -v);
            switch (kind)
            {
                case Kind.Cupboard:
                case Kind.Basin:
                    // A pair of doors with small handles.
                    c.DrawLine(At(0.5f, 3), At(0.5f, height - 3), line, 1f, true);
                    c.DrawLine(At(0.42f, height * 0.62f), At(0.42f, height * 0.78f), line, 1.4f, true);
                    c.DrawLine(At(0.58f, height * 0.62f), At(0.58f, height * 0.78f), line, 1.4f, true);
                    break;

                case Kind.Shelves:
                    // Shelves of books with coloured spines.
                    for (var i = 1; i < 5; i++)
                    {
                        var v = height * i / 5f;
                        c.DrawLine(At(0.03f, v), At(0.97f, v), line, 1f, true);
                        for (var k = 0; k < 11; k++)
                        {
                            var seed = i + k + (int)(sx * 3) + (int)(sy * 5);
                            var u = 0.06f + (k * 0.08f);
                            var tall = (height / 5f) - 2 - (seed % 3);
                            var spine = BoxColours[seed % BoxColours.Length];
                            c.DrawColoredPolygon(new[] { At(u, v + 1), At(u + 0.06f, v + 1), At(u + 0.06f, v + tall), At(u, v + tall) }, spine);
                        }
                    }

                    break;

                case Kind.Fridge:
                    // A fume cupboard: a glass sash showing the shelf inside, with the fan's green light.
                    c.DrawColoredPolygon(new[] { At(0.1f, 6), At(0.9f, 6), At(0.9f, height - 6), At(0.1f, height - 6) }, new Color("#A9D3E8", 0.75f));
                    for (var i = 1; i < 4; i++)
                    {
                        c.DrawLine(At(0.12f, height * i / 4f), At(0.88f, height * i / 4f), new Color("#E9F5FB"), 1f, true);
                    }

                    c.DrawLine(At(0.7f, height - 3), At(0.85f, height - 3), new Color("#3EE07A"), 1.5f);
                    break;
            }
        }

        if (kind == Kind.Basin)
        {
            // The basin set into the top, with a mixer tap.
            var top = new Vector2(0, -height);
            var (cx, cy) = (sx + (sw / 2), sy + (sl / 2));
            c.DrawColoredPolygon(new[] { IsoDraw.ToScreen(cx - 0.3, cy - 0.3) + top, IsoDraw.ToScreen(cx + 0.3, cy - 0.3) + top, IsoDraw.ToScreen(cx + 0.3, cy + 0.3) + top, IsoDraw.ToScreen(cx - 0.3, cy + 0.3) + top }, new Color("#C9D6DF"));
            var tap = IsoDraw.ToScreen(cx, cy) + top;
            c.DrawLine(tap, tap + new Vector2(0, -6), new Color("#9AA6B0"), 2f);
            c.DrawCircle(tap + new Vector2(0, -6), 1.6f, new Color("#B9C3CB"));
        }
    }

    /// <summary>The four corners of a footprint, pulled in by <paramref name="inset"/>.</summary>
    private static (double X, double Y)[] Corners((double X, double Y, double W, double L) r, double inset) => new[]
    {
        (r.X + inset, r.Y + inset), (r.X + r.W - inset, r.Y + inset), (r.X + r.W - inset, r.Y + r.L - inset), (r.X + inset, r.Y + r.L - inset),
    };

    private static bool Inside(double px, double py, double sx, double sy, double sw, double sl) =>
        px >= sx - 1e-6 && px < sx + sw + 1e-6 && py >= sy - 1e-6 && py < sy + sl + 1e-6;

    /// <summary>
    /// True for the slice nearest the viewer, which is drawn last: anything standing on top of the
    /// whole item is drawn by it, so the other slices don't paint over it.
    /// </summary>
    private static bool IsFrontSlice(double sx, double sy, double sw, double sl, (double X, double Y, double W, double L) whole)
    {
        var front = Corners(whole, 0.01).MaxBy(p => IsoDraw.ToScreen(p.X, p.Y).Y);
        return Inside(front.X, front.Y, sx, sy, sw, sl);
    }

    /// <summary>The outline of the whole item pulled in a little, cut to this slice: for borders and stitching.</summary>
    private static (Vector2 A, Vector2 B)[] Inset(double sx, double sy, double sw, double sl, (double X, double Y, double W, double L) whole, double inset)
    {
        var (x0, y0, x1, y1) = (whole.X + inset, whole.Y + inset, whole.X + whole.W - inset, whole.Y + whole.L - inset);
        var (a0, b0, a1, b1) = (Math.Max(x0, sx), Math.Max(y0, sy), Math.Min(x1, sx + sw), Math.Min(y1, sy + sl));
        var lines = new System.Collections.Generic.List<(Vector2, Vector2)>();
        if (a0 < a1)
        {
            if (y0 >= sy && y0 <= sy + sl) lines.Add((IsoDraw.ToScreen(a0, y0), IsoDraw.ToScreen(a1, y0)));
            if (y1 >= sy && y1 <= sy + sl) lines.Add((IsoDraw.ToScreen(a0, y1), IsoDraw.ToScreen(a1, y1)));
        }

        if (b0 < b1)
        {
            if (x0 >= sx && x0 <= sx + sw) lines.Add((IsoDraw.ToScreen(x0, b0), IsoDraw.ToScreen(x0, b1)));
            if (x1 >= sx && x1 <= sx + sw) lines.Add((IsoDraw.ToScreen(x1, b0), IsoDraw.ToScreen(x1, b1)));
        }

        return lines.ToArray();
    }

    /// <summary>A defibrillator on top of the resus trolley: grey case, yellow handle, a small screen with a trace, and the pads.</summary>
    private static void Defibrillator(CanvasItem c, double cx, double cy, float height)
    {
        IsoDraw.Prism(c, cx - 0.3, cy - 0.25, 0.6, 0.5, 6, new Color("#56616B"), new Color("#3A434B"), lift: height, rim: new Color("#7D8A95"));
        var top = IsoDraw.ToScreen(cx, cy) + new Vector2(0, -height - 6);
        c.DrawLine(top + new Vector2(-6, -3), top + new Vector2(6, -3), new Color("#F2C230"), 2.2f, true);
        c.DrawLine(top + new Vector2(-6, -3), top + new Vector2(-6, 0), new Color("#F2C230"), 2f);
        c.DrawLine(top + new Vector2(6, -3), top + new Vector2(6, 0), new Color("#F2C230"), 2f);

        // The screen, turned to face the viewer.
        var screen = new Rect2(top + new Vector2(-6, -12), new Vector2(10, 7));
        Figures.RoundedRect(c, screen.Grow(1.2f), 1.5f, new Color("#3A434B"));
        c.DrawRect(screen, new Color("#0A1218"));
        c.DrawPolyline(new[] { screen.Position + new Vector2(0.5f, 4.5f), screen.Position + new Vector2(3.5f, 4.5f), screen.Position + new Vector2(4.5f, 1), screen.Position + new Vector2(5.5f, 6), screen.Position + new Vector2(6.5f, 4.5f), screen.Position + new Vector2(9.5f, 4.5f) }, new Color("#3EE07A"), 0.8f, true);
        c.DrawCircle(top + new Vector2(7.5f, -8), 1.5f, new Color("#E53935"));

        // The pads packet and a sharps bin on the trolley top.
        var pads = IsoDraw.ToScreen(cx + 0.38, cy + 0.3) + new Vector2(0, -height);
        c.DrawColoredPolygon(new[] { pads + new Vector2(-3, 0), pads + new Vector2(3, 0), pads + new Vector2(3, -2), pads + new Vector2(-3, -2) }, new Color("#F7F7F2"));
        c.DrawLine(pads + new Vector2(-2, -1), pads + new Vector2(2, -1), new Color("#E53935"), 0.8f);
        var bin = IsoDraw.ToScreen(cx - 0.4, cy + 0.35) + new Vector2(0, -height);
        c.DrawRect(new Rect2(bin + new Vector2(-2.5f, -6), new Vector2(5, 6)), new Color("#F2C230"));
        c.DrawRect(new Rect2(bin + new Vector2(-2.5f, -7), new Vector2(5, 1.6f)), new Color("#E53935"));
    }

    private static readonly Color[] BoxColours =
    {
        new("#E57373"), new("#64B5F6"), new("#FFF176"), new("#81C784"), new("#FFFFFF"), new("#BA68C8"), new("#FFB74D"),
    };

    private static double Edge(Sides joined, Sides side) => (joined & side) != 0 ? 0 : 0.06;

    private static void Table(CanvasItem c, string id, double sx, double sy, double sw, double sl, (double X, double Y, double W, double L) whole, float height, Color colour, Sides joined)
    {
        const float slab = 4f;
        const double leg = 0.12;
        var legColour = new Color("#4A535B");

        // Legs at the corners of the whole table, drawn by whichever slice they stand in.
        foreach (var (lx, ly) in new[]
                 {
                     (whole.X + 0.08, whole.Y + 0.08), (whole.X + whole.W - 0.08 - leg, whole.Y + 0.08),
                     (whole.X + 0.08, whole.Y + whole.L - 0.08 - leg), (whole.X + whole.W - 0.08 - leg, whole.Y + whole.L - 0.08 - leg),
                 })
        {
            if (lx >= sx - 1e-6 && lx < sx + sw - 1e-6 && ly >= sy - 1e-6 && ly < sy + sl - 1e-6)
            {
                IsoDraw.Box(c, lx, ly, leg, leg, height - slab, legColour, shadow: false);
            }
        }

        IsoDraw.Prism(c, sx, sy, sw, sl, slab, colour, colour.Darkened(0.25f), joined, height - slab, rim: colour.Lightened(0.3f));
    }

    /// <summary>An examination couch: chrome legs, a padded blue top with a raised back at the head and a strip of couch roll.</summary>
    private static void Couch(CanvasItem c, double sx, double sy, double sw, double sl, (double X, double Y, double W, double L) whole, float height, Color colour, Sides joined)
    {
        const float pad = 5f;
        foreach (var (lx, ly) in Corners(whole, 0.12))
        {
            if (Inside(lx, ly, sx, sy, sw, sl))
            {
                IsoDraw.Box(c, lx - 0.05, ly - 0.05, 0.1, 0.1, height - pad, new Color("#AEB8C1"), shadow: false);
            }
        }

        IsoDraw.Prism(c, sx, sy, sw, sl, 2, new Color("#8C979F"), new Color("#6F7B85"), joined, height - pad - 2);
        IsoDraw.Prism(c, sx, sy, sw, sl, pad, colour.Lightened(0.08f), colour.Darkened(0.12f), joined, height - pad, rim: colour.Lightened(0.3f));

        // The head end is the north (or west) end; its back rest is raised.
        var alongX = whole.W >= whole.L;
        var (hx, hy, hw, hl) = alongX ? (whole.X, whole.Y, 0.9, whole.L) : (whole.X, whole.Y, whole.W, 0.9);
        var head = sx < hx + hw - 1e-6 && sy < hy + hl - 1e-6;

        // Paper couch roll down the middle.
        var across = alongX ? whole.L : whole.W;
        var (rx0, ry0, rx1, ry1) = alongX
            ? (Math.Max(sx, whole.X + 0.1), whole.Y + (across * 0.22), Math.Min(sx + sw, whole.X + whole.W - 0.1), whole.Y + (across * 0.78))
            : (whole.X + (across * 0.22), Math.Max(sy, whole.Y + 0.1), whole.X + (across * 0.78), Math.Min(sy + sl, whole.Y + whole.L - 0.1));
        var top = new Vector2(0, -height - 0.4f);
        c.DrawColoredPolygon(new[] { IsoDraw.ToScreen(rx0, ry0) + top, IsoDraw.ToScreen(rx1, ry0) + top, IsoDraw.ToScreen(rx1, ry1) + top, IsoDraw.ToScreen(rx0, ry1) + top }, new Color("#F4F6F2"));

        if (head)
        {
            var (bx, by, bw, bl) = (Math.Max(sx, hx), Math.Max(sy, hy), Math.Min(sx + sw, hx + hw) - Math.Max(sx, hx), Math.Min(sy + sl, hy + hl) - Math.Max(sy, hy));
            IsoDraw.Prism(c, bx, by, bw, bl, 6, colour.Lightened(0.12f), colour.Darkened(0.08f), joined & (alongX ? Sides.North | Sides.South : Sides.East | Sides.West), height, rim: colour.Lightened(0.35f));
            if (sx <= whole.X + 1e-6 && sy <= whole.Y + 1e-6)
            {
                // The roll itself, on its holder at the head end.
                var roll = alongX ? IsoDraw.ToScreen(whole.X - 0.05, whole.Y + (across / 2)) : IsoDraw.ToScreen(whole.X + (across / 2), whole.Y - 0.05);
                roll += new Vector2(0, -height - 3);
                c.DrawLine(roll + (alongX ? new Vector2(-8, 4) : new Vector2(8, 4)), roll + (alongX ? new Vector2(8, -4) : new Vector2(-8, -4)), new Color("#F4F6F2"), 4.5f, true);
                c.DrawLine(roll + (alongX ? new Vector2(-8, 4) : new Vector2(8, 4)), roll + (alongX ? new Vector2(8, -4) : new Vector2(-8, -4)), new Color("#D9DDD6"), 1.2f, true);
            }
        }
    }

}
