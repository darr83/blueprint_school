using System;
using System.Collections.Generic;
using System.Linq;
using BlueprintSchool.Simulation.Rooms;
using Godot;

namespace BlueprintSchool.School;

/// <summary>
/// The fitted furniture of each kind of room, drawn to suit where it stands: desks with drawer
/// pedestals for teachers and office staff, the two-level reception desk, the staff kitchenette
/// and a wash basin with its soap and towel dispensers.
/// Each has a front (the side people use) that faces into the room.
/// </summary>
public static partial class FurnitureArt
{
    private static readonly Color Steel = new("#C3CCD3");
    private static readonly Color Plinth = new("#55616B");
    private static readonly Color White = new("#F4F7F9");

    /// <summary>
    /// The side of an item people use: for desks and counters the side away from the staff chair
    /// (where visitors come), otherwise the side away from the nearest wall.
    /// </summary>
    public static Sides FrontOf(Room room, PlacedFurniture item)
    {
        var (x, y, w, l) = (item.X, item.Y, item.Width, item.Length);
        if (item.ItemId is "office_desk" or "teacher_desk" or "reception_desk")
        {
            var best = 2.2;
            var staffSide = Sides.None;
            foreach (var chair in room.Furniture.Where(f => f.ItemId == "staff_chair"))
            {
                var (cx, cy) = (chair.X + (chair.Width / 2), chair.Y + (chair.Length / 2));
                var gx = Math.Max(0, Math.Max(x - cx, cx - (x + w)));
                var gy = Math.Max(0, Math.Max(y - cy, cy - (y + l)));
                if (gx + gy < best)
                {
                    best = gx + gy;
                    staffSide = gx > gy ? (cx < x ? Sides.West : Sides.East) : (cy < y ? Sides.North : Sides.South);
                }
            }

            if (staffSide != Sides.None)
            {
                return IsoFace.Opposite(staffSide);
            }
        }

        var r = room.Rect;
        var gaps = new[] { (Sides.South, y - r.Y), (Sides.North, r.Bottom - (y + l)), (Sides.East, x - r.X), (Sides.West, r.Right - (x + w)) };

        // Long items stand with their length along the wall, so only the long sides count.
        var candidates = Math.Abs(w - l) < 0.2 ? gaps : w > l ? gaps.Take(2).ToArray() : gaps.Skip(2).ToArray();
        return candidates.MinBy(g => g.Item2).Item1;
    }

    /// <summary>How high things standing on an item sit: the staff worktop of a two-level counter is lower than its ledge.</summary>
    public static float TopHeight(string id, float height) => id switch
    {
        "reception_desk" => height - 9,
        _ => height,
    };

    /// <summary>A strip of a slice along one of its sides: <paramref name="from"/> tiles in from that side, <paramref name="depth"/> tiles deep.</summary>
    private static (double X, double Y, double W, double L) Strip(double sx, double sy, double sw, double sl, Sides side, double from, double depth) => side switch
    {
        Sides.South => (sx, sy + sl - from - depth, sw, depth),
        Sides.North => (sx, sy + from, sw, depth),
        Sides.East => (sx + sw - from - depth, sy, depth, sl),
        _ => (sx + from, sy, depth, sl),
    };

    private static double DepthAlong(double sw, double sl, Sides side) => side is Sides.North or Sides.South ? sl : sw;

    private static IEnumerable<IsoFace> FrontFaces(double x, double y, double w, double l, float height, Sides front, Sides joined, float lift = 0) =>
        IsoFace.Of(x, y, w, l, height, lift).Where(f => f.Side == front && (joined & front) == 0);

    private static IEnumerable<IsoFace> OpenFaces(double x, double y, double w, double l, float height, Sides joined, float lift = 0) =>
        IsoFace.Of(x, y, w, l, height, lift).Where(f => (joined & f.Side) == 0);

    private static int Seed(double sx, double sy) => (int)Math.Abs((Math.Floor(sx * 3) * 7) + (Math.Floor(sy * 3) * 13));

    private static int SliceIndex(double sx, double sy, double sw, double sl, (double X, double Y, double W, double L) whole) =>
        whole.W >= whole.L ? (int)Math.Round((sx - whole.X) / sw) : (int)Math.Round((sy - whole.Y) / sl);

    private static int SliceCount(double sw, double sl, (double X, double Y, double W, double L) whole) =>
        whole.W >= whole.L ? (int)Math.Round(whole.W / sw) : (int)Math.Round(whole.L / sl);

    private static Vector2 Up(double gx, double gy, float lift) => IsoDraw.ToScreen(gx, gy) + new Vector2(0, -lift);

    private static void TopQuad(CanvasItem c, double x, double y, double w, double l, float lift, Color colour) =>
        c.DrawColoredPolygon(new[] { Up(x, y, lift), Up(x + w, y, lift), Up(x + w, y + l, lift), Up(x, y + l, lift) }, colour);

    /// <summary>Draws the pieces of one slice back to front.</summary>
    private static void InOrder(params (double X, double Y, Action Draw)[] parts)
    {
        foreach (var part in parts.OrderBy(p => IsoDraw.ToScreen(p.X, p.Y).Y))
        {
            part.Draw();
        }
    }

    private static (double X, double Y) Mid((double X, double Y, double W, double L) r) => (r.X + (r.W / 2), r.Y + (r.L / 2));

    /// <summary>Draws the room-specific fitted furniture. Returns false for ids it doesn't know.</summary>
    private static bool Fitted(CanvasItem c, string id, double sx, double sy, double sw, double sl, (double X, double Y, double W, double L) whole, float height, Color colour, Sides joined, Sides front)
    {
        if (front == Sides.None)
        {
            front = whole.W >= whole.L ? Sides.South : Sides.East;
        }

        switch (id)
        {
            case "office_desk":
            case "teacher_desk":
                Desk(c, sx, sy, sw, sl, whole, height, colour, joined, front);
                return true;
            case "staff_table":
                StaffTable(c, sx, sy, sw, sl, whole, height, colour, joined);
                return true;
            case "reception_desk":
                TwoLevelCounter(c, sx, sy, sw, sl, whole, height, colour, joined, front, 9, 0.5, new Color("#1E6B47"), "Reception", Pictogram.Info);
                return true;
            case "kitchenette":
                Kitchenette(c, sx, sy, sw, sl, whole, height, colour, joined, front);
                return true;
            case "hand_basin":
                WashStation(c, sx, sy, sw, sl, whole, height, joined, front);
                return true;
            default:
                return false;
        }
    }

    // ------------------------------------------------------------------ desks and tables

    /// <summary>
    /// An office desk: a top on panel ends, a modesty panel on the visitors' side and a pedestal of
    /// three drawers under the first end on the side where the member of staff sits.
    /// </summary>
    private static void Desk(CanvasItem c, double sx, double sy, double sw, double sl, (double X, double Y, double W, double L) whole, float height, Color colour, Sides joined, Sides front)
    {
        const float slab = 3f;
        var alongX = whole.W >= whole.L;
        var (first, last) = alongX ? ((joined & Sides.West) == 0, (joined & Sides.East) == 0) : ((joined & Sides.North) == 0, (joined & Sides.South) == 0);
        var panel = new Color("#C9D1D8");
        var parts = new List<(double, double, Action)>();

        // If the front is an end of the desk (a chair at its head), put the modesty panel along a long side.
        var frontIsLong = alongX ? front is Sides.North or Sides.South : front is Sides.East or Sides.West;
        var visitorSide = frontIsLong ? front : alongX ? Sides.North : Sides.West;
        var modesty = Strip(sx, sy, sw, sl, visitorSide, 0.18, 0.07);
        parts.Add((Mid(modesty).X, Mid(modesty).Y, () => IsoDraw.Prism(c, modesty.X, modesty.Y, modesty.W, modesty.L, (height - slab) * 0.62f, colour.Darkened(0.12f), colour.Darkened(0.24f), joined, (height - slab) * 0.38f)));

        void End(bool atStart)
        {
            var e = alongX ? (X: atStart ? sx + 0.05 : sx + sw - 0.15, Y: sy + 0.08, W: 0.1, L: sl - 0.16) : (X: sx + 0.08, Y: atStart ? sy + 0.05 : sy + sl - 0.15, W: sw - 0.16, L: 0.1);
            parts.Add((Mid(e).X, Mid(e).Y, () => IsoDraw.Prism(c, e.X, e.Y, e.W, e.L, height - slab, panel, panel.Darkened(0.14f))));
        }

        if (first)
        {
            End(true);

            // The drawer pedestal, tucked under the staff side.
            var staffSide = IsoFace.Opposite(visitorSide);
            var depth = DepthAlong(sw, sl, staffSide);
            var slot = alongX ? (sx + 0.2, sy, Math.Min(0.75, sw - 0.25), sl) : (sx, sy + 0.2, sw, Math.Min(0.75, sl - 0.25));
            var ped = Strip(slot.Item1, slot.Item2, slot.Item3, slot.Item4, staffSide, 0.1, depth * 0.5);
            parts.Add((Mid(ped).X, Mid(ped).Y, () =>
            {
                var h = height - slab - 1.5f;
                IsoDraw.Prism(c, ped.X, ped.Y, ped.W, ped.L, h, new Color("#E3E8EC"), new Color("#D2D9DF"), skirting: Plinth, skirtingHeight: 1.5f);
                foreach (var face in IsoFace.Of(ped.X, ped.Y, ped.W, ped.L, h))
                {
                    if (face.Side == staffSide)
                    {
                        for (var i = 0; i < 3; i++)
                        {
                            var v = 2.5f + (i * (h - 3) / 3);
                            face.Line(c, 0.08f, v, 0.92f, v, new Color("#9AA6B0"), 0.8f);
                            face.Line(c, 0.36f, v + ((h - 3) / 6), 0.64f, v + ((h - 3) / 6), new Color("#6E7A84"), 1.2f);
                        }
                    }
                }
            }));
        }

        if (last)
        {
            End(false);
        }

        InOrder(parts.ToArray());
        IsoDraw.Prism(c, sx, sy, sw, sl, slab, colour.Lightened(0.06f), colour.Darkened(0.22f), joined, height - slab, rim: colour.Lightened(0.32f));

        // A cable port in the top of each slice.
        var port = Up(sx + (sw / 2) + (alongX ? 0 : 0.0), sy + (sl / 2), height + 0.2f);
        var off = visitorSide switch { Sides.North => new Vector2(9, -4.5f), Sides.South => new Vector2(-9, 4.5f), Sides.West => new Vector2(-9, -4.5f), _ => new Vector2(9, 4.5f) };
        c.DrawColoredPolygon(IsoDraw.FloorEllipse(port + (off * (float)(DepthAlong(sw, sl, visitorSide) * 0.42)), 1.6f, 8), colour.Darkened(0.45f));
    }

    /// <summary>A staff room table: tubular steel legs, a pale top with a coloured edge, and somebody's mug or lunch.</summary>
    private static void StaffTable(CanvasItem c, double sx, double sy, double sw, double sl, (double X, double Y, double W, double L) whole, float height, Color colour, Sides joined)
    {
        foreach (var (lx, ly) in Corners(whole, 0.16))
        {
            if (Inside(lx, ly, sx, sy, sw, sl))
            {
                var p = IsoDraw.ToScreen(lx, ly);
                c.DrawLine(p, p + new Vector2(0, -height + 3), new Color("#8C979F"), 2.2f);
                c.DrawLine(p + new Vector2(0.7f, 0), p + new Vector2(0.7f, -height + 3), new Color("#C3CCD3"), 0.7f);
                c.DrawCircle(p, 1.3f, new Color("#3A434B"));
            }
        }

        IsoDraw.Prism(c, sx, sy, sw, sl, 3, colour.Lightened(0.28f), colour.Darkened(0.05f), joined, height - 3, rim: colour.Lightened(0.5f));
        var seed = Seed(sx, sy);
        var mid = Up(sx + (sw / 2), sy + (sl / 2), height);
        switch (seed % 4)
        {
            case 0:
                Mug(c, mid + new Vector2(-4, 1), new Color("#E53935"));
                Mug(c, mid + new Vector2(6, -3), new Color("#1E88E5"));
                break;
            case 1:
                // A lunch box and an apple.
                Figures.RoundedRect(c, new Rect2(mid + new Vector2(-6, -4), new Vector2(9, 5)), 1.2f, new Color("#4DB6AC"));
                c.DrawRect(new Rect2(mid + new Vector2(-6, -4), new Vector2(9, 1.4f)), new Color("#80CBC4"));
                c.DrawCircle(mid + new Vector2(7, -1.5f), 2f, new Color("#D32F2F"));
                break;
            case 2:
                // A newspaper and a mug.
                c.DrawColoredPolygon(new[] { mid + new Vector2(-9, 0), mid + new Vector2(-1, -4), mid + new Vector2(5, -1), mid + new Vector2(-3, 3) }, new Color("#F1F1EC"));
                c.DrawLine(mid + new Vector2(-6, 0), mid + new Vector2(0, -3), new Color("#6E7A84"), 0.7f, true);
                c.DrawLine(mid + new Vector2(-4, 1.2f), mid + new Vector2(2, -1.8f), new Color("#9AA6B0"), 0.7f, true);
                Mug(c, mid + new Vector2(8, 1), new Color("#F9A825"));
                break;
        }
    }

    private static void Mug(CanvasItem c, Vector2 at, Color colour)
    {
        c.DrawArc(at + new Vector2(2.2f, -2.2f), 1.5f, -1.5f, 1.5f, 6, colour, 0.9f, true);
        c.DrawRect(new Rect2(at + new Vector2(-2, -4.4f), new Vector2(4, 4.4f)), colour);
        c.DrawRect(new Rect2(at + new Vector2(-1.5f, -4.4f), new Vector2(3, 1f)), new Color("#4E342E"));
    }

    // ------------------------------------------------------------------ counters

    /// <summary>
    /// A reception desk or nurses' station: a low worktop on the staff side and a raised ledge on the
    /// public side, a panelled front with a coloured band, and a name plate in the middle.
    /// </summary>
    private static void TwoLevelCounter(CanvasItem c, double sx, double sy, double sw, double sl, (double X, double Y, double W, double L) whole, float height, Color colour, Sides joined, Sides front, float drop, double ledgeDepth, Color band, string name, Pictogram icon)
    {
        var low = height - drop;
        var depth = DepthAlong(sw, sl, front);
        var staff = Strip(sx, sy, sw, sl, IsoFace.Opposite(front), 0, depth - ledgeDepth);
        var ledge = Strip(sx, sy, sw, sl, front, 0, ledgeDepth);
        var cream = new Color("#F3EEE4");
        var index = SliceIndex(sx, sy, sw, sl, whole);
        var count = SliceCount(sw, sl, whole);
        InOrder(
            (Mid(staff).X, Mid(staff).Y, () =>
            {
                // The staff side: an open knee space under a pale worktop, closed at the ends.
                IsoDraw.Prism(c, staff.X, staff.Y, staff.W, staff.L, low - 2.5f, colour.Darkened(0.1f), colour.Darkened(0.2f), joined | front, skirting: Plinth, skirtingHeight: 2);
                IsoDraw.Prism(c, staff.X, staff.Y, staff.W, staff.L, 2.5f, cream, cream.Darkened(0.14f), joined | front, low - 2.5f, rim: Colors.White);
            }),
            (Mid(ledge).X, Mid(ledge).Y, () =>
            {
                IsoDraw.Prism(c, ledge.X, ledge.Y, ledge.W, ledge.L, height - 2.5f, colour, colour.Darkened(0.1f), joined, skirting: Plinth, skirtingHeight: 2.5f);
                foreach (var face in FrontFaces(ledge.X, ledge.Y, ledge.W, ledge.L, height - 2.5f, front, joined))
                {
                    // Panelled front: a paler inset panel, the coloured band and a shadow line under the ledge.
                    face.Quad(c, 0, 4.5f, 1, height - 9.5f, colour.Lightened(0.14f));
                    face.Quad(c, 0, height - 9.5f, 1, height - 6.5f, band);
                    face.Line(c, 0, height - 3.2f, 1, height - 3.2f, colour.Darkened(0.35f), 1f);
                    face.Line(c, 1, 4.5f, 1, height - 9.5f, colour.Darkened(0.12f), 0.8f);
                    // The name plate sits in the middle of the whole desk; every slice it crosses draws it.
                    var centre = front switch
                    {
                        Sides.South => IsoDraw.ToScreen(whole.X + (whole.W / 2), ledge.Y + ledge.L),
                        Sides.North => IsoDraw.ToScreen(whole.X + (whole.W / 2), ledge.Y),
                        Sides.East => IsoDraw.ToScreen(ledge.X + ledge.W, whole.Y + (whole.L / 2)),
                        _ => IsoDraw.ToScreen(ledge.X, whole.Y + (whole.L / 2)),
                    };
                    var mid = (centre - face.A).Dot((face.B - face.A).Normalized());
                    var plate = Flat.Width(name, 7) + 18;
                    if (count >= 2 && mid + (plate / 2) > 0 && mid - (plate / 2) < face.Length)
                    {
                        face.Draw(c, (length, h) =>
                        {
                            Flat.Round(c, mid - (plate / 2), h - 20.5f, plate, 11f, 1.8f, face.Lit(band));
                            Signs.Draw(c, icon, mid - (plate / 2) + 6.5f, h - 15f, 7.5f, Colors.White, face.Lit(band));
                            Flat.Text(c, name, mid + 5.5f, h - 12.3f, 7, Colors.White);
                        }, shade: false);
                    }
                }

                var over = 0.05;
                var top = Strip(sx, sy, sw, sl, front, -over, ledgeDepth + over);
                IsoDraw.Prism(c, top.X, top.Y, top.W, top.L, 2.5f, Colors.White, new Color("#DDE3E8"), joined, height - 2.5f, rim: Colors.White);

                // Now and then something left on the ledge: a pen pot or a leaflet stand.
                var (mx, my) = Mid(ledge);
                var at = Up(mx, my, height);
                switch (Seed(sx, sy) % 5)
                {
                    case 1:
                        c.DrawRect(new Rect2(at + new Vector2(-1.6f, -4), new Vector2(3.2f, 4)), new Color("#37474F"));
                        c.DrawLine(at + new Vector2(-0.6f, -4), at + new Vector2(-1.4f, -7), new Color("#1E88E5"), 0.8f);
                        c.DrawLine(at + new Vector2(0.6f, -4), at + new Vector2(1.2f, -6.6f), new Color("#E53935"), 0.8f);
                        break;
                    case 3:
                        c.DrawRect(new Rect2(at + new Vector2(-3, -6), new Vector2(6, 6)), new Color(0.85f, 0.93f, 0.97f, 0.7f));
                        c.DrawRect(new Rect2(at + new Vector2(-2.2f, -5.4f), new Vector2(4.4f, 4f)), band.Lightened(0.2f));
                        c.DrawRect(new Rect2(at + new Vector2(-2.2f, -5.4f), new Vector2(4.4f, 1.2f)), Colors.White);
                        break;
                }
            }));
    }

    // ------------------------------------------------------------------ cupboards

    /// <summary>A flat door on a face: the panel outline, a bar handle and (optionally) a little label.</summary>
    private static void Door(CanvasItem c, IsoFace face, float u0, float u1, float v0, float v1, Color line, Color handle, bool handleLeft, Color? label = null)
    {
        c.DrawPolyline(new[] { face.P(u0, v0), face.P(u1, v0), face.P(u1, v1), face.P(u0, v1), face.P(u0, v0) }, face.Lit(line), 0.8f, true);
        var hu = handleLeft ? u0 + ((u1 - u0) * 0.16f) : u1 - ((u1 - u0) * 0.16f);
        face.Line(c, hu, v0 + ((v1 - v0) * 0.52f), hu, v0 + ((v1 - v0) * 0.84f), handle, 1.5f);
        if (label is { } l)
        {
            var lu = handleLeft ? u1 - ((u1 - u0) * 0.52f) : u0 + ((u1 - u0) * 0.14f);
            face.Quad(c, lu, v1 - 5.5f, lu + ((u1 - u0) * 0.38f), v1 - 2.5f, Colors.White);
            face.Quad(c, lu, v1 - 5.5f, lu + ((u1 - u0) * 0.1f), v1 - 2.5f, l);
        }
    }

    private static readonly Color[] LabelColours = { new("#1E88E5"), new("#E53935"), new("#43A047"), new("#F9A825"), new("#8E24AA") };

    /// <summary>The staff room kitchenette: a sink, a fridge under the worktop and a microwave, with pale doors.</summary>
    private static void Kitchenette(CanvasItem c, double sx, double sy, double sw, double sl, (double X, double Y, double W, double L) whole, float height, Color colour, Sides joined, Sides front)
    {
        var body = height - 2.5f;
        var index = SliceIndex(sx, sy, sw, sl, whole);
        var count = SliceCount(sw, sl, whole);
        var door = new Color("#C7D9E2");
        IsoDraw.Prism(c, sx, sy, sw, sl, body, door, door.Darkened(0.08f), joined, skirting: Plinth, skirtingHeight: 2.5f);
        foreach (var face in FrontFaces(sx, sy, sw, sl, body, front, joined))
        {
            if (index == 1 && count > 2)
            {
                // The fridge: a white door with a long handle and a little badge.
                face.Quad(c, 0.04f, 3.5f, 0.96f, body - 1, White);
                face.Line(c, 0.1f, body - 4.5f, 0.9f, body - 4.5f, Steel.Darkened(0.2f), 1.4f);
                face.Quad(c, 0.72f, 6f, 0.88f, 8f, new Color("#64B5F6"));
            }
            else
            {
                Door(c, face, 0.05f, 0.95f, 4f, body - 5.5f, door.Darkened(0.3f), Steel.Darkened(0.25f), index % 2 == 0);
                face.Line(c, 0.05f, body - 4f, 0.95f, body - 4f, door.Darkened(0.3f), 0.8f);
                face.Line(c, 0.38f, body - 2.6f, 0.62f, body - 2.6f, Steel.Darkened(0.25f), 1.3f);
            }
        }

        var wood = new Color("#C9A77C");
        IsoDraw.Prism(c, sx, sy, sw, sl, 2.5f, wood, wood.Darkened(0.2f), joined, body, rim: wood.Lightened(0.3f));
        var back = Strip(sx, sy, sw, sl, IsoFace.Opposite(front), 0, 0.1);
        IsoDraw.Prism(c, back.X, back.Y, back.W, back.L, 4f, White, new Color("#DDE3E8"), joined, height);
        var (mx, my) = (sx + (sw / 2), sy + (sl / 2));
        if (index == 0)
        {
            // A steel sink with its drainer and a mixer tap.
            TopQuad(c, sx + (sw * 0.14), sy + (sl * 0.18), sw * 0.72, sl * 0.6, height + 0.3f, Steel);
            TopQuad(c, sx + (sw * 0.24), sy + (sl * 0.28), sw * 0.52, sl * 0.4, height + 0.3f, new Color("#7D8A95"));
            var (tx, ty) = Mid(Strip(sx, sy, sw, sl, IsoFace.Opposite(front), 0.16, 0.05));
            var tap = Up(tx, ty, height);
            c.DrawLine(tap, tap + new Vector2(0, -7), Steel.Darkened(0.2f), 1.8f);
            var spout = (Up(mx, my, height + 7) - (tap + new Vector2(0, -7))) * 0.55f;
            c.DrawLine(tap + new Vector2(0, -7), tap + new Vector2(0, -7) + spout, Steel.Darkened(0.2f), 1.6f, true);
        }
        else if (index == count - 1)
        {
            // The microwave.
            var (wx, wy, ww, wl) = (sx + (sw * 0.1), sy + (sl * 0.2), sw * 0.8, sl * 0.6);
            IsoDraw.Prism(c, wx, wy, ww, wl, 9, White, new Color("#DDE3E8"), lift: height, rim: Colors.White);
            foreach (var face in FrontFaces(wx, wy, ww, wl, 9, front, Sides.None, height))
            {
                face.Quad(c, 0.06f, 1.5f, 0.7f, 7.5f, new Color("#1B2228"));
                face.Quad(c, 0.1f, 2.2f, 0.66f, 6.8f, new Color("#3A4A58"));
                face.Quad(c, 0.76f, 5.5f, 0.94f, 7.5f, new Color("#0D1B14"));
                face.Line(c, 0.78f, 6.5f, 0.9f, 6.5f, new Color("#5CF08A"), 0.8f);
                face.Quad(c, 0.78f, 1.8f, 0.92f, 4.2f, Steel);
            }
        }
        else if (count <= 2 || index != 1)
        {
            Mug(c, Up(mx, my, height) + new Vector2(-3, 1), new Color("#FBFCFD"));
            Mug(c, Up(mx, my, height) + new Vector2(4, -1), new Color("#1E88E5"));
        }
    }

    // ------------------------------------------------------------------ plaster room

    // ------------------------------------------------------------------ pharmacy

    // ------------------------------------------------------------------ wash basin

    /// <summary>
    /// A clinical wash station: a white panel up the wall with soap and paper towel dispensers, a
    /// ceramic basin with a lever tap, the boxed-in pipework below and a pedal bin beside it.
    /// </summary>
    private static void WashStation(CanvasItem c, double sx, double sy, double sw, double sl, (double X, double Y, double W, double L) whole, float height, Sides joined, Sides front)
    {
        var back = IsoFace.Opposite(front);
        var depth = DepthAlong(sw, sl, front);
        var panel = Strip(sx, sy, sw, sl, back, 0, 0.14);
        var unit = Strip(sx, sy, sw, sl, back, 0.14, depth - 0.3);
        var tall = height * 1.85f;
        var alongX = whole.W >= whole.L;
        var first = SliceIndex(sx, sy, sw, sl, whole) == 0;
        InOrder(
            (Mid(panel).X, Mid(panel).Y, () =>
            {
                IsoDraw.Prism(c, panel.X, panel.Y, panel.W, panel.L, tall, White, new Color("#E7ECF0"), joined, skirting: Plinth, skirtingHeight: 2.5f, rim: Colors.White);
                foreach (var face in FrontFaces(panel.X, panel.Y, panel.W, panel.L, tall, front, joined))
                {
                    if (first)
                    {
                        // Soap (teal) and hand gel dispensers.
                        face.Quad(c, 0.52f, tall - 12, 0.86f, tall - 4.5f, Colors.White);
                        face.Quad(c, 0.52f, tall - 12, 0.86f, tall - 9.5f, new Color("#2BB3A3"));
                        face.Quad(c, 0.64f, tall - 13.5f, 0.74f, tall - 12, Steel.Darkened(0.2f));
                    }
                    else
                    {
                        // The paper towel dispenser, a towel poking out.
                        face.Quad(c, 0.12f, tall - 13, 0.88f, tall - 3.5f, new Color("#CFD8DC"));
                        face.Quad(c, 0.12f, tall - 13, 0.88f, tall - 11.5f, new Color("#9AA6B0"));
                        face.Quad(c, 0.3f, tall - 15.5f, 0.7f, tall - 13, Colors.White);
                    }
                }
            }),
            (Mid(unit).X, Mid(unit).Y, () =>
            {
                IsoDraw.Prism(c, unit.X, unit.Y, unit.W, unit.L, height - 5, White, new Color("#E1E7EC"), joined, skirting: Plinth, skirtingHeight: 2.5f);
                foreach (var face in FrontFaces(unit.X, unit.Y, unit.W, unit.L, height - 5, front, joined))
                {
                    c.DrawPolyline(new[] { face.P(0.08f, 4), face.P(0.92f, 4), face.P(0.92f, height - 7), face.P(0.08f, height - 7), face.P(0.08f, 4) }, face.Lit(new Color("#B9C6CF")), 0.7f, true);
                }

                // The basin: white ceramic with a grey bowl, sitting proud of the unit.
                var bowl = Strip(sx, sy, sw, sl, back, 0.14, depth - 0.2);
                var (bx, by, bw, bl) = alongX ? (bowl.X + ((joined & Sides.West) == 0 ? 0.08 : 0), bowl.Y, bowl.W - ((joined & Sides.West) == 0 ? 0.08 : 0) - ((joined & Sides.East) == 0 ? 0.08 : 0), bowl.L)
                    : (bowl.X, bowl.Y + ((joined & Sides.North) == 0 ? 0.08 : 0), bowl.W, bowl.L - ((joined & Sides.North) == 0 ? 0.08 : 0) - ((joined & Sides.South) == 0 ? 0.08 : 0));
                IsoDraw.Prism(c, bx, by, bw, bl, 5, Colors.White, new Color("#E9EEF2"), joined, height - 5, rim: Colors.White);
                if (first)
                {
                    var (cx, cy) = alongX ? (whole.X + (whole.W / 2), by + (bl / 2)) : (bx + (bw / 2), whole.Y + (whole.L / 2));
                    var (rw, rl) = alongX ? (Math.Min(whole.W * 0.32, 0.42), bl * 0.3) : (bw * 0.3, Math.Min(whole.L * 0.32, 0.42));
                    TopQuad(c, cx - rw, cy - rl, 2 * rw, 2 * rl, height + 0.3f, new Color("#CBD6DE"));
                    TopQuad(c, cx - (rw * 0.6), cy - (rl * 0.6), 1.2 * rw, 1.2 * rl, height + 0.3f, new Color("#AEBBC5"));
                    var (tx, ty) = back switch { Sides.North => (cx, by + 0.08), Sides.South => (cx, by + bl - 0.08), Sides.West => (bx + 0.08, cy), _ => (bx + bw - 0.08, cy) };
                    var tap = Up(tx, ty, height);
                    c.DrawLine(tap, tap + new Vector2(0, -6), Steel.Darkened(0.2f), 1.8f);
                    var spout = (Up(cx, cy, height + 6) - (tap + new Vector2(0, -6))) * 0.7f;
                    c.DrawLine(tap + new Vector2(0, -6), tap + new Vector2(0, -6) + spout, Steel.Darkened(0.2f), 1.6f, true);
                    c.DrawLine(tap + new Vector2(0, -6), tap + new Vector2(3, -8.5f), Steel.Darkened(0.35f), 1.2f, true);
                }
            }));
    }
}
