using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace BlueprintSchool.School;

/// <summary>How someone wears their hair (or what covers it).</summary>
public enum HairStyle
{
    Short,
    Crop,
    Curly,
    Long,
    Ponytail,
    Bun,
    Bald,
    SidePart,
    Quiff,
    Afro,
    Wavy,
    Bob,
    Braids,
    Pigtails,
    Balding,
    Receding,
    Perm,
    Hijab,
    Turban,
}

/// <summary>The cut of what someone wears on top.</summary>
public enum TopKind
{
    Tee,
    Stripes,
    Shirt,
    Polo,
    Tunic,
    Scrubs,
    Hoodie,
    Jumper,
    Cardigan,
    Dungarees,
    Dress,
}

/// <summary>What someone wears on their legs.</summary>
public enum LegKind
{
    Trousers,
    Jeans,
    Shorts,
    Skirt,
}

public enum Mouth
{
    Smile,
    Grin,
    Flat,
}

/// <summary>
/// What a person looks like and is wearing. Everyone in the game (patients, visitors and every
/// staff role) is drawn from one of these by <see cref="Figures"/>, so new roles only need a new
/// look in <see cref="Looks"/>: a uniform colour, its trim, and the kit that sets the job apart.
/// </summary>
public sealed record PersonLook(Color Top, Color Trousers, Color Skin, Color Hair)
{
    public TopKind Kind { get; init; } = TopKind.Tee;

    /// <summary>Trim on the top: tunic piping, the stripes, a dress's print or the blouse under a cardigan.</summary>
    public Color? Trim { get; init; }

    /// <summary>A round motif printed on a T-shirt.</summary>
    public Color? Motif { get; init; }

    public LegKind Legs { get; init; } = LegKind.Trousers;

    /// <summary>Tights or socks under a skirt or dress (bare legs if null).</summary>
    public Color? Tights { get; init; }

    public Color? ShoeColour { get; init; }

    public bool LongSleeves { get; init; }

    public HairStyle Style { get; init; } = HairStyle.Short;

    /// <summary>The colour of hair bobbles on pigtails and braids.</summary>
    public Color? Bobble { get; init; }

    public bool Female { get; init; }

    /// <summary>Height against an adult (1): toddlers about 0.5, children 0.6 to 0.8, teenagers 0.9.</summary>
    public float Scale { get; init; } = 1f;

    /// <summary>Head size against an adult's; children's heads are large for their bodies.</summary>
    public float HeadScale { get; init; } = 1f;

    /// <summary>Slim (0.9) to heavy (1.25).</summary>
    public float Build { get; init; } = 1f;

    public bool Belly { get; init; }

    /// <summary>The slight stoop of old age.</summary>
    public bool Stoop { get; init; }

    public bool Beard { get; init; }

    public bool Stubble { get; init; }

    public bool Moustache { get; init; }

    public bool Glasses { get; init; }

    public Color? GlassesFrame { get; init; }

    public bool Freckles { get; init; }

    public bool Blush { get; init; }

    public bool Wrinkles { get; init; }

    public Color? Lips { get; init; }

    public Mouth Mouth { get; init; } = Mouth.Smile;

    public Color? Jacket { get; init; }

    public bool Stick { get; init; }

    /// <summary>A walking frame.</summary>
    public bool Frame { get; init; }

    public bool Bump { get; init; }

    public bool Teddy { get; init; }

    // Staff kit.
    public bool Stethoscope { get; init; }

    public bool Lanyard { get; init; }

    /// <summary>The lanyard's colour, which says who someone is: green for teachers, purple for support staff, gold for leadership.</summary>
    public Color? LanyardColour { get; init; }

    public bool Pocket { get; init; }

    public bool FobWatch { get; init; }

    public Color? Cap { get; init; }

    public bool Mask { get; init; }

    public Color? Tie { get; init; }

    public Color? Apron { get; init; }

    public Color? Epaulettes { get; init; }

    public Color? Badge { get; init; }

    public Color? Gloves { get; init; }

    public bool HiVis { get; init; }

    public bool Radio { get; init; }

    /// <summary>The hair style actually drawn.</summary>
    public HairStyle Hairdo => Style;
}

public static partial class Figures
{
    /// <summary>A CanvasItem with a fade applied, and round-ended limbs.</summary>
    private readonly struct Pen
    {
        private readonly CanvasItem _c;
        private readonly float _alpha;

        public Pen(CanvasItem c, float alpha)
        {
            _c = c;
            _alpha = alpha;
        }

        public Color Fade(Color colour) => new(colour, colour.A * _alpha);

        public void Poly(Vector2[] points, Color fill, Color? edge = null, float width = 0.9f)
        {
            _c.DrawColoredPolygon(points, Fade(fill));
            if (edge is { } e)
            {
                _c.DrawPolyline(points.Append(points[0]).ToArray(), Fade(e), width, true);
            }
        }

        public void Line(Vector2 a, Vector2 b, Color colour, float width) => _c.DrawLine(a, b, Fade(colour), width, true);

        public void Open(Color colour, float width, params Vector2[] points) => _c.DrawPolyline(points, Fade(colour), width, true);

        /// <summary>A limb: thick segments with rounded joints and ends.</summary>
        public void Limb(Color colour, float width, params Vector2[] points)
        {
            var faded = Fade(colour);
            for (var i = 0; i < points.Length; i++)
            {
                _c.DrawCircle(points[i], width / 2, faded);
                if (i > 0)
                {
                    _c.DrawLine(points[i - 1], points[i], faded, width);
                }
            }
        }

        public void Dot(Vector2 at, float radius, Color colour) => _c.DrawCircle(at, radius, Fade(colour));

        public void Oval(Vector2 at, float rx, float ry, Color colour, Color? edge = null) => Poly(Ellipse(at, rx, ry, 20), colour, edge);

        public void Arc(Vector2 at, float radius, float from, float to, Color colour, float width) => _c.DrawArc(at, radius, from, to, 10, Fade(colour), width, true);

        public void Rect(Vector2 at, Vector2 size, Color colour) => _c.DrawRect(new Rect2(at, size), Fade(colour));
    }

    /// <summary>The measurements of one person's body, from their age, sex and build.</summary>
    private readonly struct Rig
    {
        public readonly float S, Lw, ShY, HipY, ShW, HipW, WaistW, WaistY, Ys, Rx, Ry, LegW, ArmW, Gap;
        public readonly Vector2 Head;
        public readonly bool Kid;

        public Rig(PersonLook look)
        {
            S = look.Scale;
            Kid = S < 0.8f;
            Lw = Mathf.Pow(S, 0.75f);
            var grown = look.Female && !Kid;
            ShY = -41 * S;
            HipY = -19 * S;
            ShW = 9 * S * (Kid ? 0.95f : look.Female ? 0.9f : 1.05f) * (0.6f + (0.4f * look.Build));
            HipW = 8.4f * S * (grown ? 1.04f : 1f) * look.Build;
            WaistW = (grown ? HipW * 0.84f : HipW * 0.98f) * (look.Belly ? 1.2f : 1f);
            WaistY = ShY + ((HipY - ShY) * 0.62f);
            Ys = ShY - (3.2f * S);
            Rx = Ry = 7.5f * look.HeadScale;
            var stoop = look.Stoop ? 1.6f : 0f;
            Head = new Vector2(stoop * 0.6f, ShY - (2.2f * S) - (Ry * 0.86f) + stoop);
            LegW = 5.5f * Lw * (0.75f + (0.25f * look.Build));
            ArmW = 4.6f * Lw;
            Gap = 3.6f * S * (0.8f + (0.2f * look.Build));
        }
    }

    public static readonly Color Shoes = new("#2B2F33");
    public static readonly Color Outline = new("#1D2B36");
    private static readonly Color LanyardGreen = new("#1E6B47");

    /// <summary>
    /// A standing (or walking) person whose feet are at <paramref name="foot"/>. With
    /// <paramref name="away"/> they are seen from behind, walking away from the viewer.
    /// </summary>
    public static void Standing(CanvasItem c, Vector2 foot, PersonLook look, double time, bool walking, int seed, float alpha = 1f, bool away = false)
    {
        var pen = new Pen(c, alpha);
        var r = new Rig(look);
        var s = r.S;
        c.DrawColoredPolygon(IsoDraw.FloorEllipse(foot, (10 * s) + 1), new Color(0, 0, 0, 0.2f * alpha));
        var phase = (time * 9) + seed;
        var swing = walking ? (float)Math.Sin(phase) * 3.2f * s : 0f;
        var bob = walking ? -(float)Math.Abs(Math.Cos(phase)) * 1.1f * s : 0f;
        Vector2 P(float x, float y) => foot + new Vector2(x, y);

        if (look.Frame)
        {
            WalkingFrame(pen, foot, r, behind: true);
        }

        // Legs (bare under a skirt or dress, bare shins in shorts) and shoes.
        var dress = look.Kind == TopKind.Dress;
        var skirt = look.Legs == LegKind.Skirt;
        var bare = dress || skirt;
        var hem = dress ? r.HipY * 0.42f : skirt ? r.HipY * 0.5f : r.HipY;
        var shoes = look.ShoeColour ?? Shoes;
        var footY = -2.2f * s;
        foreach (var side in new[] { -1f, 1f })
        {
            var s2 = side * swing * (away ? -1 : 1);
            var x = side * r.Gap;
            var toe = x + (side < 0 ? s2 : -s2);
            var cloth = side > 0 ? look.Trousers.Darkened(0.1f) : look.Trousers;
            var skin = look.Tights ?? look.Skin.Darkened(side > 0 ? 0.1f : 0.04f);
            var hip = P(x, r.HipY + 1 + bob);
            var ankle = P(toe, footY - 1);
            if (look.Legs == LegKind.Shorts && !dress)
            {
                var knee = hip.Lerp(ankle, 0.5f);
                pen.Limb(look.Skin.Darkened(side > 0 ? 0.1f : 0.03f), r.LegW * 0.72f, knee, ankle);
                pen.Limb(cloth, r.LegW, hip, knee);
            }
            else
            {
                pen.Limb(bare ? skin : cloth, bare ? r.LegW * 0.7f : r.LegW, hip, ankle);
                if (look.Legs == LegKind.Jeans && !bare)
                {
                    pen.Line(ankle + new Vector2(-r.LegW * 0.5f, -1.5f), ankle + new Vector2(r.LegW * 0.5f, -1.5f), look.Trousers.Lightened(0.25f), 1f);
                }
            }

            pen.Oval(P(toe, footY + (away ? -0.3f : 0.6f)), 3.3f * r.Lw, 2.3f * r.Lw, shoes);
        }

        if (skirt && !dress)
        {
            pen.Poly(new[] { P(-r.HipW, r.HipY - 1 + bob), P(r.HipW, r.HipY - 1 + bob), P(r.HipW * 1.25f, hem + bob), P(-r.HipW * 1.25f, hem + bob) }, look.Trousers);
        }

        Body(pen, c, foot, look, r, -swing, bob, away, alpha, propped: look.Frame || look.Stick);

        if (look.Stick && !away)
        {
            // A walking stick in the right hand, planted a little ahead.
            var hand = P(r.ShW + (3 * s) - (swing * 0.4f), r.HipY - (4 * s) + bob);
            pen.Line(hand + new Vector2(-2, -1), hand + new Vector2(2, -1), new Color("#5A3B22"), 2f);
            pen.Line(hand, P(r.ShW + (3 * s) + 1.5f, 1), new Color("#6B4A2E"), 1.6f);
        }

        if (look.Frame)
        {
            WalkingFrame(pen, foot, r, behind: false);
        }
    }

    /// <summary>A walking frame: the far legs behind the person, the near legs and the hand rail in front.</summary>
    private static void WalkingFrame(Pen pen, Vector2 foot, Rig r, bool behind)
    {
        var metal = new Color("#AEB9C2");
        foreach (var side in new[] { -1f, 1f })
        {
            if (behind)
            {
                pen.Line(foot + new Vector2(side * 9, r.HipY - 5), foot + new Vector2(side * 8.5f, -3), metal.Darkened(0.15f), 1.4f);
            }
            else
            {
                pen.Line(foot + new Vector2(side * 12, r.HipY - 3), foot + new Vector2(side * 12.5f, 2), metal, 1.6f);
            }
        }

        if (!behind)
        {
            pen.Line(foot + new Vector2(-12, r.HipY * 0.55f), foot + new Vector2(12, r.HipY * 0.55f), metal, 1.4f);
        }
    }

    /// <summary>A person sitting on a chair whose seat centre is at <paramref name="foot"/>.</summary>
    /// <remarks>
    /// <paramref name="away"/> draws them from behind (their legs are hidden by the chair and their body);
    /// <paramref name="lean"/> turns their knees a little to the left (-1) or right (+1) of the screen.
    /// </remarks>
    public static void Seated(CanvasItem c, Vector2 foot, PersonLook look, float alpha = 1f, bool away = false, float lean = 0)
    {
        var pen = new Pen(c, alpha);
        var r = new Rig(look);
        var s = r.S;

        // Their hips rest on the seat whatever their height; a child's feet dangle.
        var drop = -12 - r.HipY;
        if (away)
        {
            Body(pen, c, foot, look, r, 0, drop, away: true, alpha);
            return;
        }

        Vector2 P(float x, float y) => foot + new Vector2(x, y);
        var k = lean * 2.5f;
        var dress = look.Kind == TopKind.Dress;
        var bare = dress || look.Legs == LegKind.Skirt;
        var shorts = look.Legs == LegKind.Shorts && !dress;
        var shoes = look.ShoeColour ?? Shoes;
        var floor = -10 + (12 * s);
        foreach (var side in new[] { -1f, 1f })
        {
            var cloth = side > 0 ? look.Trousers.Darkened(0.08f) : look.Trousers;
            var skin = look.Tights ?? look.Skin.Darkened(side > 0 ? 0.1f : 0.04f);
            var hip = P(side * r.Gap, -15);
            var knee = P((side * (r.Gap + 1)) + k, -10);
            var ankle = P((side * (r.Gap + 1)) + k, floor - 1);

            // Thighs forward (towards the viewer), shins down to the floor.
            pen.Limb(bare ? skin : cloth, r.LegW, hip, knee);
            pen.Limb(bare || shorts ? (shorts ? look.Skin.Darkened(side > 0 ? 0.1f : 0.03f) : skin) : cloth, bare || shorts ? r.LegW * 0.72f : r.LegW * 0.92f, knee, ankle);
            pen.Oval(ankle + new Vector2(0, 2.2f), 3.3f * r.Lw, 2.3f * r.Lw, shoes);
        }

        if (bare)
        {
            var cloth = dress ? look.Top : look.Trousers;
            pen.Poly(new[] { P(-r.HipW - 0.5f, -16), P(r.HipW + 0.5f, -16), P(r.HipW + 1.5f + k, -8), P(-r.HipW - 1.5f + k, -8) }, cloth);
        }

        Body(pen, c, foot, look, r, 0, drop, away: false, alpha, seated: true);
    }

    /// <summary>Everything above the legs: torso and clothes, arms, neck, head and the kit they carry.</summary>
    private static void Body(Pen pen, CanvasItem c, Vector2 foot, PersonLook look, Rig r, float armSwing, float drop, bool away, float alpha, bool seated = false, bool propped = false)
    {
        Vector2 P(float x, float y) => foot + new Vector2(x, y + drop);
        var s = r.S;
        var top = look.Top;
        var outer = look.Jacket ?? top;
        var skin = look.Skin;
        var trim = look.Trim;
        var head = P(r.Head.X, r.Head.Y);
        var scarf = look.Style == HairStyle.Hijab;

        if (!away && !scarf && look.Cap is null)
        {
            HairBehind(pen, head, r.Rx, r.Ry, look, foot.Y + drop + r.ShY);
        }

        if (look.Kind == TopKind.Hoodie && look.Jacket is null && !away)
        {
            pen.Oval(P(r.Head.X, r.ShY - (1.5f * s)), r.ShW * 0.8f, 4.2f * s, top.Darkened(0.12f));
        }

        // Torso. A tunic or coat comes down over the hips; a dress flares to the knee.
        var dress = look.Kind == TopKind.Dress && look.Jacket is null;
        var longer = look.Kind == TopKind.Tunic || look.Jacket is not null;
        var hemY = dress ? (seated ? r.HipY + 3 : r.HipY * 0.42f) : r.HipY + (longer ? 3.2f * s : 0.5f);
        var hemW = dress ? r.HipW * (seated ? 1.1f : 1.32f) : r.HipW * (longer ? 1.05f : 1f);
        var torso = new[]
        {
            P(-r.ShW * 0.7f, r.Ys), P(r.ShW * 0.7f, r.Ys), P(r.ShW, r.ShY + 0.5f), P(r.WaistW, r.WaistY),
            P(hemW, hemY), P(-hemW, hemY), P(-r.WaistW, r.WaistY), P(-r.ShW, r.ShY + 0.5f),
        };
        pen.Poly(torso, outer, outer.Darkened(0.32f));

        // The side away from the light is a shade darker.
        pen.Poly(new[] { P(r.ShW * 0.3f, r.Ys), P(r.ShW * 0.7f, r.Ys), P(r.ShW, r.ShY + 0.5f), P(r.WaistW, r.WaistY), P(hemW, hemY), P(r.ShW * 0.3f, hemY) }, new Color(outer.Darkened(0.5f), 0.13f));

        // Half the torso's width at a given height, for prints that run edge to edge.
        float HalfAt(float y) => y < r.WaistY
            ? Mathf.Lerp(r.ShW, r.WaistW, Mathf.Clamp((y - r.ShY) / (r.WaistY - r.ShY), 0, 1))
            : Mathf.Lerp(r.WaistW, hemW, Mathf.Clamp((y - r.WaistY) / Math.Max(0.01f, hemY - r.WaistY), 0, 1));

        if (look.Jacket is null && look.Kind == TopKind.Stripes && trim is { } stripe)
        {
            for (var y = r.ShY + 1.6f; y < hemY - 1; y += 3.4f * s)
            {
                var half = HalfAt(y) - 0.4f;
                pen.Line(P(-half, y), P(half, y), stripe, 1.5f * s);
            }
        }

        if (dress && trim is { } print)
        {
            for (var i = 0; i < 12; i++)
            {
                var y = Mathf.Lerp(r.ShY + 1, hemY - 1.5f, i / 11f);
                var half = HalfAt(y) - 1.5f;
                pen.Dot(P(Mathf.Lerp(-half, half, ((i * 5) % 12 + ((i % 3) * 0.6f)) / 12.2f), y), 0.9f, print);
            }
        }

        if (look.Bump)
        {
            // Heavily pregnant: a round bump under the top, catching the light on the left.
            var centre = P(0.6f, r.WaistY + 1.5f);
            pen.Oval(centre, r.HipW * 1.2f, (r.HipY - r.ShY) * 0.36f, top, top.Darkened(0.3f));
            if (!away)
            {
                pen.Arc(centre + new Vector2(2.4f, 0.5f), r.HipW * 0.75f, -0.2f, Mathf.Pi * 0.5f, top.Darkened(0.14f), 2.2f);
                pen.Arc(centre + new Vector2(-1.5f, -0.5f), r.HipW * 0.7f, Mathf.Pi * 0.8f, Mathf.Pi * 1.1f, top.Lightened(0.22f), 1.3f);
            }
        }
        else if (look.Belly && !away)
        {
            pen.Arc(P(0, r.WaistY - 1), r.HipW, 0.5f, Mathf.Pi - 0.5f, new Color(top.Darkened(0.4f), 0.35f), 1f);
        }

        void NeckV(float width, float depth) => pen.Poly(new[] { P(-width, r.Ys), P(width, r.Ys), P(0, r.Ys + depth) }, skin);

        if (away)
        {
            // From behind: the neckline, and any trim that runs right round.
            pen.Line(P(-r.ShW * 0.4f, r.Ys), P(r.ShW * 0.4f, r.Ys), outer.Darkened(0.3f), 1.2f);
            if (look.Kind == TopKind.Hoodie && look.Jacket is null)
            {
                pen.Poly(new[] { P(-r.ShW * 0.75f, r.Ys - 1), P(r.ShW * 0.75f, r.Ys - 1), P(r.ShW * 0.45f, r.ShY + (7 * s)), P(-r.ShW * 0.45f, r.ShY + (7 * s)) }, top.Darkened(0.1f), top.Darkened(0.3f));
            }
            else if (look.Kind == TopKind.Dungarees && look.Jacket is null)
            {
                pen.Line(P(-r.ShW * 0.5f, r.Ys), P(r.ShW * 0.3f, r.WaistY), look.Trousers, 1.8f * s);
                pen.Line(P(r.ShW * 0.5f, r.Ys), P(-r.ShW * 0.3f, r.WaistY), look.Trousers, 1.8f * s);
                pen.Poly(new[] { P(-r.WaistW, r.WaistY), P(r.WaistW, r.WaistY), P(hemW, hemY), P(-hemW, hemY) }, look.Trousers);
            }
        }
        else if (look.Jacket is not null)
        {
            // Open coat or blazer: the top underneath shows down the middle, with the lapels either side.
            pen.Poly(new[] { P(-r.ShW * 0.36f, r.Ys), P(r.ShW * 0.36f, r.Ys), P(r.ShW * 0.3f, hemY), P(-r.ShW * 0.3f, hemY) }, top);
            if (look.Tie is { } jacketTie)
            {
                pen.Poly(new[] { P(-1.2f, r.Ys + (2.6f * s)), P(1.2f, r.Ys + (2.6f * s)), P(1.9f, r.WaistY + 2), P(0, r.WaistY + 4.5f), P(-1.9f, r.WaistY + 2) }, jacketTie);
                pen.Line(P(-1.6f, r.Ys + (6 * s)), P(1.6f, r.Ys + (5 * s)), jacketTie.Lightened(0.45f), 0.8f);
            }

            pen.Line(P(-r.ShW * 0.36f, r.Ys), P(-r.ShW * 0.3f, hemY), outer.Darkened(0.35f), 0.9f);
            pen.Line(P(r.ShW * 0.36f, r.Ys), P(r.ShW * 0.3f, hemY), outer.Darkened(0.35f), 0.9f);
            pen.Poly(new[] { P(-r.ShW * 0.36f, r.Ys), P(-r.ShW * 0.68f, r.Ys), P(-r.ShW * 0.32f, r.Ys + (8 * s)) }, outer.Lightened(0.14f));
            pen.Poly(new[] { P(r.ShW * 0.36f, r.Ys), P(r.ShW * 0.68f, r.Ys), P(r.ShW * 0.32f, r.Ys + (8 * s)) }, outer.Darkened(0.06f));
            NeckV(r.ShW * 0.22f, 3 * s);
        }
        else
        {
            switch (look.Kind)
            {
                case TopKind.Shirt:
                case TopKind.Polo:
                    // A collar and a button placket, and sometimes a tie.
                    NeckV(r.ShW * 0.16f, 3 * s);
                    pen.Poly(new[] { P(-r.ShW * 0.58f, r.Ys - 0.8f), P(-0.6f, r.Ys + 0.4f), P(-r.ShW * 0.24f, r.Ys + (4 * s)) }, top.Lightened(0.18f), top.Darkened(0.2f), 0.6f);
                    pen.Poly(new[] { P(r.ShW * 0.58f, r.Ys - 0.8f), P(0.6f, r.Ys + 0.4f), P(r.ShW * 0.24f, r.Ys + (4 * s)) }, top.Lightened(0.06f), top.Darkened(0.2f), 0.6f);
                    pen.Line(P(0, r.Ys + (3 * s)), P(0, look.Kind == TopKind.Polo ? r.Ys + (8 * s) : hemY - 1), top.Darkened(0.22f), 0.8f);
                    if (look.Tie is { } tie)
                    {
                        pen.Poly(new[] { P(-1.2f, r.Ys + (2.6f * s)), P(1.2f, r.Ys + (2.6f * s)), P(1.9f, r.WaistY + 2), P(0, r.WaistY + 4.5f), P(-1.9f, r.WaistY + 2) }, tie);
                    }

                    break;

                case TopKind.Tunic:
                    // A clinical tunic: V-neck, fastened down one side, piped at the neck, the fastening and the hem.
                    NeckV(r.ShW * 0.3f, 4.4f * s);
                    if (trim is { } piping)
                    {
                        pen.Open(piping, 1.5f, P(-r.ShW * 0.34f, r.Ys), P(0, r.Ys + (5 * s)), P(r.ShW * 0.34f, r.Ys));
                        pen.Open(new Color(piping, 0.9f), 1.1f, P(0, r.Ys + (5 * s)), P(r.ShW * 0.42f, r.WaistY), P(r.ShW * 0.42f, hemY));
                        pen.Line(P(-hemW, hemY - 0.6f), P(hemW, hemY - 0.6f), piping, 1.4f);
                    }

                    break;

                case TopKind.Scrubs:
                    NeckV(r.ShW * 0.36f, 5.4f * s);
                    pen.Open(top.Darkened(0.25f), 0.9f, P(-r.ShW * 0.36f, r.Ys), P(0, r.Ys + (5.4f * s)), P(r.ShW * 0.36f, r.Ys));
                    pen.Line(P(-hemW, hemY - 0.4f), P(hemW, hemY - 0.4f), top.Darkened(0.25f), 0.9f);
                    break;

                case TopKind.Hoodie:
                    NeckV(r.ShW * 0.3f, 3.4f * s);
                    pen.Line(P(-1.6f, r.Ys + (3 * s)), P(-2, r.Ys + (9 * s)), new Color("#F1F1F1"), 0.8f);
                    pen.Line(P(1.6f, r.Ys + (3 * s)), P(2, r.Ys + (9 * s)), new Color("#F1F1F1"), 0.8f);
                    pen.Poly(new[] { P(-r.HipW * 0.6f, r.WaistY), P(r.HipW * 0.6f, r.WaistY), P(r.HipW * 0.75f, hemY - 2), P(-r.HipW * 0.75f, hemY - 2) }, top.Darkened(0.1f));
                    break;

                case TopKind.Jumper:
                    pen.Open(top.Darkened(0.25f), 1.6f, P(-r.ShW * 0.34f, r.Ys + 0.6f), P(0, r.Ys + (2.4f * s)), P(r.ShW * 0.34f, r.Ys + 0.6f));
                    pen.Line(P(-hemW, hemY - 1.2f), P(hemW, hemY - 1.2f), top.Darkened(0.2f), 1.8f);
                    break;

                case TopKind.Cardigan:
                    pen.Poly(new[] { P(-r.ShW * 0.4f, r.Ys), P(r.ShW * 0.4f, r.Ys), P(1.2f, r.WaistY - 2), P(-1.2f, r.WaistY - 2) }, trim ?? new Color("#F1EDE4"));
                    NeckV(r.ShW * 0.2f, 2.6f * s);
                    pen.Line(P(0, r.WaistY - 2), P(0, hemY), top.Darkened(0.3f), 0.8f);
                    for (var i = 0; i < 3; i++)
                    {
                        pen.Dot(P(0, r.WaistY - 0.5f + (i * 3.2f * s)), 0.7f, top.Lightened(0.4f));
                    }

                    break;

                case TopKind.Dungarees:
                    NeckV(r.ShW * 0.3f, 3 * s);
                    pen.Poly(new[] { P(-r.ShW * 0.55f, r.ShY + (3 * s)), P(r.ShW * 0.55f, r.ShY + (3 * s)), P(r.HipW, hemY), P(-r.HipW, hemY) }, look.Trousers);
                    foreach (var side in new[] { -1f, 1f })
                    {
                        pen.Line(P(side * r.ShW * 0.5f, r.ShY + (3 * s)), P(side * r.ShW * 0.55f, r.Ys), look.Trousers, 1.8f * s);
                        pen.Dot(P(side * r.ShW * 0.5f, r.ShY + (3.4f * s)), 0.9f, new Color("#F2C230"));
                    }

                    break;

                case TopKind.Dress:
                    NeckV(r.ShW * 0.34f, 4 * s);
                    pen.Line(P(-r.WaistW, r.WaistY), P(r.WaistW, r.WaistY), top.Darkened(0.22f), 1.3f);
                    break;

                default:
                    // A T-shirt's round neck.
                    pen.Poly(HalfOval(P(0, r.Ys), r.ShW * 0.34f, 2.6f * s), skin);
                    if (look.Motif is { } motif)
                    {
                        pen.Dot(P(0, r.ShY + (7 * s)), 3 * s, motif);
                    }

                    break;
            }
        }

        if (look.Apron is { } apron && !away)
        {
            pen.Poly(new[] { P(-r.ShW * 0.55f, r.ShY + 2), P(r.ShW * 0.55f, r.ShY + 2), P(r.HipW * 0.95f, hemY + (seated ? 1 : 6 * s)), P(-r.HipW * 0.95f, hemY + (seated ? 1 : 6 * s)) }, apron, apron.Darkened(0.25f), 0.8f);
            pen.Line(P(-r.ShW * 0.5f, r.ShY + 2), P(-r.ShW * 0.3f, r.Ys), apron, 1.2f);
            pen.Line(P(r.ShW * 0.5f, r.ShY + 2), P(r.ShW * 0.3f, r.Ys), apron, 1.2f);
        }
        else if (look.Apron is { } ties && away)
        {
            // Only the apron's neck loop and waist ties show from behind.
            pen.Line(P(-r.ShW * 0.3f, r.Ys + 0.5f), P(r.ShW * 0.3f, r.Ys + 0.5f), ties, 1.2f);
            pen.Line(P(-r.WaistW, r.WaistY), P(r.WaistW, r.WaistY), ties, 1.2f);
        }

        if (look.Epaulettes is { } tabs)
        {
            foreach (var side in new[] { -1f, 1f })
            {
                pen.Line(P(side * r.ShW * 0.55f, r.ShY - (2.2f * s)), P(side * r.ShW * 0.98f, r.ShY - (0.6f * s)), tabs, 2.2f);
            }
        }

        if (look.HiVis)
        {
            foreach (var y in new[] { r.WaistY - 1, r.WaistY + 4 })
            {
                pen.Line(P(-HalfAt(y) + 0.3f, y), P(HalfAt(y) - 0.3f, y), new Color("#E8EEF0"), 1.6f);
            }
        }

        if (!away)
        {
            if (look.Pocket)
            {
                // Breast pocket with two pens.
                pen.Rect(P(-r.ShW * 0.78f, r.ShY + (4.5f * s)), new Vector2(r.ShW * 0.5f, 4 * s), top.Darkened(0.12f));
                pen.Line(P(-r.ShW * 0.66f, r.ShY + (4.5f * s)), P(-r.ShW * 0.66f, r.ShY + (2.4f * s)), new Color("#1E4F9E"), 1f);
                pen.Line(P(-r.ShW * 0.48f, r.ShY + (4.5f * s)), P(-r.ShW * 0.48f, r.ShY + (3 * s)), new Color("#D03A3A"), 1f);
            }

            if (look.FobWatch)
            {
                var watch = P(r.ShW * 0.56f, r.ShY + 5.8f);
                pen.Line(P(r.ShW * 0.56f, r.ShY + 1.6f), watch, new Color("#B9C3CB"), 0.8f);
                pen.Dot(watch, 1.8f, new Color("#F2F5F7"));
                pen.Arc(watch, 1.8f, 0, Mathf.Tau, new Color("#7D8A95"), 0.6f);
            }

            if (look.Badge is { } badge)
            {
                pen.Rect(P(r.ShW * 0.3f, r.ShY + (3 * s)), new Vector2(r.ShW * 0.46f, 3.2f * s), badge);
            }
        }

        if (look.Radio)
        {
            // A two-way radio clipped to the belt.
            var black = new Color("#1C1F22");
            var at = P((away ? -1 : 1) * r.HipW * 0.6f, r.HipY - 5);
            pen.Rect(at + new Vector2(-1.7f, 0), new Vector2(3.4f, 5.5f), black);
            pen.Line(at + new Vector2(0.9f, 0), at + new Vector2(0.9f, -3.5f), black, 0.9f);
        }

        // Arms: short sleeves and bare forearms (bare below the elbows) unless in long sleeves or a coat.
        var longSleeves = look.LongSleeves || look.Jacket is not null || look.Kind is TopKind.Hoodie or TopKind.Jumper or TopKind.Cardigan;
        var teddy = look.Teddy && !away && !seated;
        foreach (var side in new[] { -1f, 1f })
        {
            var shoulder = P(side * (r.ShW - 0.6f), r.ShY + 1.6f);
            var elbow = P(side * (r.ShW + (1.3f * s)), r.ShY + ((r.HipY - r.ShY) * 0.5f) + (armSwing * side * 0.3f));
            var hand = P((side * (r.ShW + (1.4f * s))) + (armSwing * side * 0.6f), r.HipY - (2.5f * s));
            if (seated)
            {
                // Hands resting in the lap.
                hand = P(side * r.ShW * 0.75f, r.HipY - (1.5f * s));
            }
            else if (!away && ((look.Stick && side > 0) || look.Frame))
            {
                hand = P(side * (r.ShW + (3 * s)), r.HipY - (4 * s));
            }
            else if (teddy && side < 0)
            {
                hand = P(-r.ShW - 1.5f, r.HipY + 1);
            }

            var sleeve = side > 0 ? outer.Darkened(0.1f) : outer;
            var arm = side > 0 ? skin.Darkened(0.06f) : skin;
            if (longSleeves)
            {
                pen.Limb(sleeve, r.ArmW, shoulder, elbow, hand);
            }
            else
            {
                pen.Limb(arm, r.ArmW * 0.88f, elbow, hand);
                pen.Limb(sleeve, r.ArmW + 0.6f, shoulder, shoulder.Lerp(elbow, 0.8f));
                if (trim is { } cuff && look.Kind == TopKind.Tunic)
                {
                    var end = shoulder.Lerp(elbow, 0.8f);
                    pen.Line(end + new Vector2(-(r.ArmW + 0.6f) / 2, 0), end + new Vector2((r.ArmW + 0.6f) / 2, 0), cuff, 1.1f);
                }
            }

            pen.Dot(hand, r.ArmW * 0.56f, look.Gloves ?? arm);
        }

        if (teddy)
        {
            var bear = new Color("#B98952");
            var at = P(-r.ShW - 2.5f, r.HipY + 5);
            pen.Dot(at, 2.6f, bear);
            pen.Dot(at + new Vector2(0, -3.6f), 2.1f, bear);
            pen.Dot(at + new Vector2(-1.7f, -5.3f), 0.9f, bear);
            pen.Dot(at + new Vector2(1.7f, -5.3f), 0.9f, bear);
        }

        // Neck, then the lanyard and stethoscope round it.
        var neckTop = head.Y + (r.Ry * 0.6f);
        pen.Rect(new Vector2(head.X - (r.Head.X * 0.5f) - (r.Rx * 0.33f), neckTop), new Vector2(r.Rx * 0.66f, P(0, r.Ys).Y - neckTop + 1.2f), skin.Darkened(0.08f));
        if (look.Lanyard)
        {
            var lanyard = look.LanyardColour ?? LanyardGreen;
            if (away)
            {
                pen.Line(P(-r.ShW * 0.28f, r.Ys - 0.5f), P(r.ShW * 0.28f, r.Ys - 0.5f), lanyard, 1.4f);
            }
            else
            {
                var card = P(0, r.ShY + (10.5f * s));
                pen.Open(lanyard, 1.4f, P(-r.ShW * 0.24f, r.Ys), card + new Vector2(r.ShW * 0.12f, 0), P(r.ShW * 0.3f, r.Ys));
                pen.Rect(card, new Vector2(4, 5), Colors.White);
                pen.Rect(card + new Vector2(0.6f, 0.7f), new Vector2(1.4f, 1.6f), new Color("#9AB4CC"));
                pen.Rect(card + new Vector2(0, 4), new Vector2(4, 1), lanyard);
            }
        }

        if (look.Stethoscope)
        {
            var tube = new Color("#2E3A44");
            if (away)
            {
                pen.Open(tube, 1.5f, P(-r.ShW * 0.55f, r.Ys + 1), P(-r.ShW * 0.2f, r.Ys - 1), P(r.ShW * 0.2f, r.Ys - 1), P(r.ShW * 0.55f, r.Ys + 1));
            }
            else
            {
                pen.Open(tube, 1.5f, P(-r.ShW * 0.5f, r.Ys + 0.5f), P(-r.ShW * 0.38f, r.ShY + 5), P(0, r.ShY + 8.5f), P(r.ShW * 0.38f, r.ShY + 5), P(r.ShW * 0.5f, r.Ys + 0.5f));
                pen.Dot(P(0, r.ShY + 9.5f), 1.9f, new Color("#B9C3CB"));
            }
        }

        if (away)
        {
            HeadFromBehind(pen, head, r.Rx, r.Ry, look, P(0, r.ShY).Y);
            return;
        }

        // The head: a headscarf frames a slightly smaller face.
        var face = scarf ? 0.84f : 1f;
        if (scarf)
        {
            pen.Poly(new[] { P(-r.ShW * 0.85f, r.ShY + 1), P(r.ShW * 0.85f, r.ShY + 1), P(r.ShW * 0.35f, r.ShY + (7 * s)), P(-r.ShW * 0.35f, r.ShY + (7 * s)) }, look.Hair, look.Hair.Darkened(0.25f));
            pen.Oval(head + new Vector2(0, -r.Ry * 0.05f), r.Rx * 1.22f, r.Ry * 1.2f, look.Hair, look.Hair.Darkened(0.25f));
            head += new Vector2(0, r.Ry * 0.08f);
        }
        else
        {
            pen.Dot(head + new Vector2(-r.Rx * 0.97f, r.Ry * 0.13f), 1.8f * r.Rx / 7.5f, skin);
            pen.Dot(head + new Vector2(r.Rx * 0.97f, r.Ry * 0.13f), 1.8f * r.Rx / 7.5f, skin.Darkened(0.08f));
        }

        var (rx, ry) = (r.Rx * face, r.Ry * face);
        pen.Oval(head, rx, ry, skin, skin.Darkened(0.28f));
        pen.Oval(head + new Vector2(rx * 0.33f, ry * 0.13f), rx * 0.73f, ry * 0.73f, new Color(skin.Darkened(0.06f), 0.5f));

        if (look.Cap is { } cap)
        {
            // A theatre cap pulled down over the hair, with its print.
            var dome = new List<Vector2>();
            for (var i = 0; i <= 12; i++)
            {
                var a = Mathf.Pi + (i * Mathf.Pi / 12);
                dome.Add(head + new Vector2(Mathf.Cos(a) * rx * 1.1f, (Mathf.Sin(a) * ry * 1.14f) - 0.3f));
            }

            dome.Add(head + new Vector2(rx * 1.06f, -ry * 0.18f));
            dome.Add(head + new Vector2(-rx * 1.06f, -ry * 0.18f));
            pen.Poly(dome.ToArray(), cap, cap.Darkened(0.3f));
            pen.Line(head + new Vector2(-rx * 1.04f, -ry * 0.22f), head + new Vector2(rx * 1.04f, -ry * 0.22f), cap.Darkened(0.25f), 1.3f);
            foreach (var (a, b) in new[] { (-0.45f, -0.65f), (0.3f, -0.8f), (0.6f, -0.45f), (-0.1f, -0.45f) })
            {
                pen.Dot(head + new Vector2(a * rx, b * ry), 0.8f, cap.Lightened(0.4f));
            }
        }
        else if (!scarf)
        {
            HairFront(pen, head, rx, ry, look);
        }

        Face(pen, head, rx, ry, look);

        if (look.Mask)
        {
            // A surgical mask pulled down under the chin.
            var blue = new Color("#8FC3E6");
            pen.Poly(new[] { head + new Vector2(-rx * 0.6f, ry * 0.8f), head + new Vector2(rx * 0.6f, ry * 0.8f), head + new Vector2(rx * 0.46f, ry * 1.35f), head + new Vector2(-rx * 0.46f, ry * 1.35f) }, blue, blue.Darkened(0.25f));
            pen.Line(head + new Vector2(-rx * 0.6f, ry * 0.85f), head + new Vector2(-rx * 0.95f, ry * 0.4f), Colors.White, 0.6f);
            pen.Line(head + new Vector2(rx * 0.6f, ry * 0.85f), head + new Vector2(rx * 0.95f, ry * 0.4f), Colors.White, 0.6f);
        }
    }

    private static Vector2[] HalfOval(Vector2 centre, float rx, float ry)
    {
        var points = new Vector2[9];
        for (var i = 0; i < points.Length; i++)
        {
            var a = i * Mathf.Pi / (points.Length - 1);
            points[i] = centre + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
        }

        return points;
    }

    /// <summary>Eyes, brows, mouth and everything that makes one face different from the next.</summary>
    private static void Face(Pen pen, Vector2 head, float rx, float ry, PersonLook look)
    {
        var e = rx / 7.5f;
        var skin = look.Skin;
        var eyeY = ry * 0.14f;
        var eyeX = rx * 0.36f;
        if (look.Blush)
        {
            foreach (var side in new[] { -1f, 1f })
            {
                pen.Dot(head + new Vector2(side * rx * 0.55f, ry * 0.42f), rx * 0.2f, new Color(0.94f, 0.47f, 0.47f, 0.35f));
            }
        }

        var brow = look.Hair.V > 0.75f && look.Hair.S < 0.15f ? new Color("#B9B9B6") : look.Hair.Darkened(0.25f);
        foreach (var side in new[] { -1f, 1f })
        {
            pen.Dot(head + new Vector2(side * eyeX, eyeY), 0.95f * e, new Color("#2A211C"));
            pen.Line(head + new Vector2(side * (eyeX + (1.2f * e)), eyeY - (2.5f * e)), head + new Vector2(side * (eyeX - (1.1f * e)), eyeY - (2.8f * e)), brow, 0.8f * e);
        }

        if (look.Wrinkles)
        {
            foreach (var side in new[] { -1f, 1f })
            {
                pen.Line(head + new Vector2(side * rx * 0.72f, eyeY - (0.6f * e)), head + new Vector2(side * rx * 0.86f, eyeY - (1.2f * e)), skin.Darkened(0.25f), 0.6f);
                pen.Line(head + new Vector2(side * rx * 0.3f, ry * 0.5f), head + new Vector2(side * rx * 0.44f, ry * 0.68f), skin.Darkened(0.22f), 0.6f);
            }

            pen.Line(head + new Vector2(-rx * 0.3f, -ry * 0.5f), head + new Vector2(rx * 0.3f, -ry * 0.5f), skin.Darkened(0.18f), 0.6f);
        }

        if (look.Freckles)
        {
            foreach (var (a, b) in new[] { (-0.5f, 0.3f), (-0.38f, 0.4f), (0.5f, 0.3f), (0.38f, 0.4f), (-0.6f, 0.42f), (0.6f, 0.42f) })
            {
                pen.Dot(head + new Vector2(a * rx, b * ry), 0.4f * e, skin.Darkened(0.3f));
            }
        }

        var mouthY = ry * 0.52f;
        var lip = skin.Darkened(0.42f);
        if (look.Beard)
        {
            // A full beard round the jaw, cut away under the nose.
            var beard = new List<Vector2>();
            for (var i = 0; i <= 10; i++)
            {
                var a = Mathf.Pi * i / 10;
                beard.Add(head + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry * 1.04f));
            }

            beard.Add(head + new Vector2(-rx * 0.92f, ry * 0.05f));
            beard.Add(head + new Vector2(-rx * 0.55f, ry * 0.38f));
            beard.Add(head + new Vector2(0, ry * 0.3f));
            beard.Add(head + new Vector2(rx * 0.55f, ry * 0.38f));
            beard.Add(head + new Vector2(rx * 0.92f, ry * 0.05f));
            pen.Poly(beard.ToArray(), look.Hair);
            pen.Line(head + new Vector2(-1.5f * e, mouthY), head + new Vector2(1.5f * e, mouthY), skin.Darkened(0.15f), 1f * e);
        }
        else
        {
            if (look.Stubble)
            {
                var shadow = new List<Vector2>();
                for (var i = 0; i <= 10; i++)
                {
                    var a = Mathf.Pi * i / 10;
                    shadow.Add(head + new Vector2(Mathf.Cos(a) * rx * 0.95f, Mathf.Sin(a) * ry * 0.98f));
                }

                shadow.Add(head + new Vector2(0, ry * 0.2f));
                pen.Poly(shadow.ToArray(), new Color(look.Hair, 0.22f));
            }

            if (look.Moustache)
            {
                pen.Line(head + new Vector2(-2.2f * e, mouthY - (0.9f * e)), head + new Vector2(2.2f * e, mouthY - (0.9f * e)), look.Hair, 1.5f * e);
            }

            switch (look.Mouth)
            {
                case Mouth.Grin:
                    pen.Poly(HalfOval(head + new Vector2(0, mouthY - (0.8f * e)), 2.1f * e, 2.1f * e), new Color("#8A3B3B"));
                    pen.Line(head + new Vector2(-1.3f * e, mouthY - (0.45f * e)), head + new Vector2(1.3f * e, mouthY - (0.45f * e)), Colors.White, 0.7f * e);
                    break;
                case Mouth.Flat:
                    pen.Line(head + new Vector2(-1.5f * e, mouthY), head + new Vector2(1.5f * e, mouthY), lip, 0.9f * e);
                    break;
                default:
                    pen.Arc(head + new Vector2(0, mouthY - (1.2f * e)), 2f * e, 0.45f, Mathf.Pi - 0.45f, lip, 0.9f * e);
                    break;
            }

            if (look.Lips is { } lips && look.Mouth != Mouth.Grin)
            {
                pen.Line(head + new Vector2(-1.3f * e, mouthY + 0.1f), head + new Vector2(1.3f * e, mouthY + 0.1f), new Color(lips, 0.8f), 1.1f * e);
            }
        }

        if (look.Glasses)
        {
            var frame = look.GlassesFrame ?? new Color("#2B2F33");
            foreach (var side in new[] { -1f, 1f })
            {
                pen.Dot(head + new Vector2(side * eyeX, eyeY), 2.1f * e, new Color(1, 1, 1, 0.22f));
                pen.Dot(head + new Vector2(side * eyeX, eyeY), 0.95f * e, new Color("#2A211C"));
                pen.Arc(head + new Vector2(side * eyeX, eyeY), 2.1f * e, 0, Mathf.Tau, frame, 0.8f * e);
            }

            pen.Line(head + new Vector2(-eyeX + (2.1f * e), eyeY - 0.2f), head + new Vector2(eyeX - (2.1f * e), eyeY - 0.2f), frame, 0.8f * e);
        }
    }

    /// <summary>Hair over the top of the head down to a fringe line, given in head-sized units from right to left.</summary>
    private static Vector2[] HairCap(Vector2 head, float rx, float ry, float size, float down, (float X, float Y)[] fringe, float lift = 0)
    {
        var points = new List<Vector2>();
        for (var i = 0; i <= 16; i++)
        {
            var a = Mathf.Pi - down + (i / 16f * (Mathf.Pi + (2 * down)));
            points.Add(head + new Vector2(Mathf.Cos(a) * rx * size, (Mathf.Sin(a) * ry * size) + lift));
        }

        points.AddRange(fringe.Select(p => head + new Vector2(p.X * rx, p.Y * ry)));
        return points.ToArray();
    }

    /// <summary>Hair that hangs behind the head and shoulders, drawn before the body.</summary>
    private static void HairBehind(Pen pen, Vector2 head, float rx, float ry, PersonLook look, float shoulderY)
    {
        var hair = look.Hair;
        switch (look.Style)
        {
            case HairStyle.Long:
                pen.Poly(new[] { head + new Vector2(-rx * 1.1f, -ry * 0.2f), head + new Vector2(rx * 1.1f, -ry * 0.2f), new Vector2(head.X + (rx * 1.12f), shoulderY + 7), new Vector2(head.X - (rx * 1.12f), shoulderY + 7) }, hair);
                break;
            case HairStyle.Wavy:
                pen.Poly(new[] { head + new Vector2(-rx * 1.15f, -ry * 0.2f), head + new Vector2(rx * 1.15f, -ry * 0.2f), new Vector2(head.X + (rx * 1.3f), shoulderY + 8), new Vector2(head.X - (rx * 1.3f), shoulderY + 8) }, hair);
                for (var i = -2; i <= 2; i++)
                {
                    pen.Dot(new Vector2(head.X + (i * rx * 0.55f), shoulderY + 8), rx * 0.36f, hair);
                }

                break;
            case HairStyle.Afro:
                pen.Dot(head + new Vector2(0, -ry * 0.25f), rx * 1.55f, hair);
                break;
            case HairStyle.Bob:
                pen.Poly(new[] { head + new Vector2(-rx * 1.22f, -ry * 0.3f), head + new Vector2(rx * 1.22f, -ry * 0.3f), head + new Vector2(rx * 1.28f, ry * 0.95f), head + new Vector2(-rx * 1.28f, ry * 0.95f) }, hair);
                break;
            case HairStyle.Ponytail:
                pen.Poly(new[] { head + new Vector2(rx * 0.6f, -ry * 0.4f), head + new Vector2(rx * 1.5f, -ry * 0.1f), head + new Vector2(rx * 1.55f, ry * 1.5f), head + new Vector2(rx * 1.05f, ry * 1.3f) }, hair.Darkened(0.1f));
                break;
        }
    }

    /// <summary>Hair as seen from the front, for each style.</summary>
    private static void HairFront(Pen pen, Vector2 head, float rx, float ry, PersonLook look)
    {
        var hair = look.Hair;
        void Cap(float size, float down, params (float, float)[] fringe) => pen.Poly(HairCap(head, rx, ry, size, down, fringe), hair);
        void Shine() => pen.Arc(head + new Vector2(-rx * 0.15f, -ry * 0.25f), rx * 0.72f, Mathf.Pi * 1.15f, Mathf.Pi * 1.5f, new Color(hair.Lightened(0.35f), 0.8f), Math.Max(0.8f, rx * 0.13f));
        void BareScalp() => pen.Dot(head + new Vector2(-rx * 0.33f, -ry * 0.6f), rx * 0.18f, new Color(1, 1, 1, 0.3f));

        switch (look.Style)
        {
            case HairStyle.Crop:
                Cap(1.03f, 0, (0.82f, -0.5f), (-0.82f, -0.5f));
                break;
            case HairStyle.SidePart:
                Cap(1.08f, 0.18f, (0.92f, -0.15f), (0.4f, -0.55f), (-0.3f, -0.25f), (-0.97f, 0));
                Shine();
                break;
            case HairStyle.Quiff:
                pen.Oval(head + new Vector2(-rx * 0.1f, -ry), rx * 0.78f, ry * 0.42f, hair);
                Cap(1.07f, 0.08f, (0.82f, -0.42f), (-0.82f, -0.42f));
                Shine();
                break;
            case HairStyle.Curly:
            case HairStyle.Perm:
                Cap(1.08f, 0.2f, (0.85f, -0.3f), (-0.85f, -0.3f));
                for (var i = 0; i <= 8; i++)
                {
                    var a = (Mathf.Pi * 0.95f) + (i * Mathf.Pi * 1.1f / 8);
                    pen.Dot(head + new Vector2(Mathf.Cos(a) * rx * 1.02f, (Mathf.Sin(a) * ry * 1.02f) - 0.4f), rx * 0.34f, hair);
                }

                for (var i = 0; i < 4; i++)
                {
                    pen.Dot(head + new Vector2((-rx * 0.6f) + (i * rx * 0.4f), -ry * 0.5f), rx * 0.26f, hair);
                }

                break;
            case HairStyle.Afro:
                Cap(1.12f, 0.25f, (0.88f, -0.3f), (0, -0.45f), (-0.88f, -0.3f));
                break;
            case HairStyle.Long:
            case HairStyle.Wavy:
                Cap(1.09f, 0.4f, (1f, 0.35f), (0.6f, -0.28f), (0.08f, -0.62f), (-0.5f, -0.3f), (-1f, 0.35f));
                Shine();
                break;
            case HairStyle.Bob:
                Cap(1.11f, 0.2f, (0.95f, -0.1f), (0.75f, -0.27f), (-0.75f, -0.27f), (-0.95f, -0.1f));
                Shine();
                break;
            case HairStyle.Ponytail:
                Cap(1.06f, 0.1f, (0.8f, -0.32f), (0, -0.52f), (-0.8f, -0.32f));
                Shine();
                break;
            case HairStyle.Bun:
                pen.Dot(head + new Vector2(0, -ry * 1.15f), rx * 0.45f, hair);
                Cap(1.06f, 0.1f, (0.8f, -0.32f), (0, -0.52f), (-0.8f, -0.32f));
                Shine();
                break;
            case HairStyle.Braids:
                Cap(1.08f, 0.3f, (0.97f, 0.2f), (0.45f, -0.45f), (0, -0.55f), (-0.45f, -0.45f), (-0.97f, 0.2f));
                foreach (var side in new[] { -1f, 1f })
                {
                    for (var i = 0; i < 5; i++)
                    {
                        pen.Dot(head + new Vector2(side * rx * 1.05f, ry * (0.45f + (i * 0.36f))), rx * 0.24f, i == 4 ? look.Bobble ?? new Color("#E0559A") : hair);
                    }
                }

                break;
            case HairStyle.Pigtails:
                Cap(1.07f, 0.15f, (0.85f, -0.3f), (0.2f, -0.42f), (0, -0.6f), (-0.2f, -0.42f), (-0.85f, -0.3f));
                foreach (var side in new[] { -1f, 1f })
                {
                    pen.Dot(head + new Vector2(side * rx * 1.3f, ry * 0.15f), rx * 0.42f, hair);
                    pen.Dot(head + new Vector2(side * rx * 1.02f, -ry * 0.12f), rx * 0.2f, look.Bobble ?? new Color("#E0559A"));
                }

                break;
            case HairStyle.Bald:
                BareScalp();
                break;
            case HairStyle.Balding:
                // A fringe of hair round the sides only.
                pen.Arc(head + new Vector2(0, ry * 0.05f), rx, Mathf.Pi * 0.95f, Mathf.Pi * 1.2f, hair, rx * 0.32f);
                pen.Arc(head + new Vector2(0, ry * 0.05f), rx, Mathf.Pi * 1.8f, Mathf.Pi * 2.05f, hair, rx * 0.32f);
                BareScalp();
                break;
            case HairStyle.Receding:
                Cap(1.05f, 0.15f, (0.88f, -0.25f), (0.5f, -0.72f), (0, -0.6f), (-0.5f, -0.72f), (-0.88f, -0.25f));
                break;
            case HairStyle.Turban:
                pen.Poly(HairCap(head, rx, ry, 1.22f, 0.05f, new[] { (1f, -0.2f), (0f, -0.62f), (-1f, -0.2f) }, -ry * 0.12f), hair, hair.Darkened(0.3f));
                pen.Open(hair.Darkened(0.2f), 0.8f, head + new Vector2(-rx * 0.9f, -ry * 0.55f), head + new Vector2(0, -ry * 0.62f), head + new Vector2(rx * 0.5f, -ry * 1.15f));
                pen.Open(hair.Darkened(0.2f), 0.8f, head + new Vector2(rx * 0.9f, -ry * 0.55f), head + new Vector2(0, -ry * 0.62f), head + new Vector2(-rx * 0.5f, -ry * 1.15f));
                break;
            default:
                Cap(1.07f, 0.1f, (0.6f, -0.42f), (-0.2f, -0.3f), (-0.85f, -0.22f));
                Shine();
                break;
        }
    }

    /// <summary>The back of the head: hair over almost all of it (or a cap, scarf or turban), ears showing.</summary>
    private static void HeadFromBehind(Pen pen, Vector2 head, float rx, float ry, PersonLook look, float shoulderY)
    {
        var skin = look.Skin;
        var hair = look.Hair;
        var e = rx / 7.5f;
        if (look.Style == HairStyle.Hijab)
        {
            pen.Poly(new[] { new Vector2(head.X - (rx * 1.1f), shoulderY + 1), new Vector2(head.X + (rx * 1.1f), shoulderY + 1), head + new Vector2(rx * 0.9f, 0), head + new Vector2(-rx * 0.9f, 0) }, hair);
            pen.Oval(head + new Vector2(0, -ry * 0.05f), rx * 1.22f, ry * 1.2f, hair, hair.Darkened(0.25f));
            return;
        }

        pen.Dot(head + new Vector2(-rx * 0.97f, ry * 0.13f), 1.8f * e, skin);
        pen.Dot(head + new Vector2(rx * 0.97f, ry * 0.13f), 1.8f * e, skin);
        pen.Dot(head, rx, skin);
        if (look.Cap is { } cap)
        {
            pen.Oval(head + new Vector2(0, -ry * 0.1f), rx * 1.1f, ry * 1.04f, cap);
            pen.Line(head + new Vector2(-1, ry * 0.4f), head + new Vector2(-3, ry * 1.05f), cap.Darkened(0.2f), 1.2f);
            pen.Line(head + new Vector2(1, ry * 0.4f), head + new Vector2(3, ry * 1.05f), cap.Darkened(0.2f), 1.2f);
            return;
        }

        switch (look.Style)
        {
            case HairStyle.Bald:
                break;
            case HairStyle.Balding:
                pen.Arc(head + new Vector2(0, ry * 0.07f), rx * 0.98f, Mathf.Pi * -0.05f, Mathf.Pi * 1.05f, hair, 3f * e);
                break;
            case HairStyle.Long:
            case HairStyle.Wavy:
                pen.Poly(new[] { head + new Vector2(-rx * 1.1f, -ry * 0.15f), head + new Vector2(rx * 1.1f, -ry * 0.15f), head + new Vector2(rx * 1.05f, ry * 1.5f), head + new Vector2(-rx * 1.05f, ry * 1.5f) }, hair);
                pen.Dot(head, rx * 1.07f, hair);
                break;
            case HairStyle.Bob:
                pen.Poly(new[] { head + new Vector2(-rx * 1.2f, -ry * 0.2f), head + new Vector2(rx * 1.2f, -ry * 0.2f), head + new Vector2(rx * 1.25f, ry * 0.95f), head + new Vector2(-rx * 1.25f, ry * 0.95f) }, hair);
                pen.Dot(head, rx * 1.08f, hair);
                break;
            case HairStyle.Ponytail:
                pen.Dot(head, rx * 1.05f, hair);
                pen.Poly(new[] { head + new Vector2(-2 * e, ry * 0.65f), head + new Vector2(2 * e, ry * 0.65f), head + new Vector2(1.2f * e, ry * 1.75f), head + new Vector2(-1.2f * e, ry * 1.75f) }, hair);
                pen.Line(head + new Vector2(-1.8f * e, ry * 0.8f), head + new Vector2(1.8f * e, ry * 0.8f), look.Bobble ?? hair.Darkened(0.45f), 1.2f);
                break;
            case HairStyle.Bun:
                pen.Dot(head, rx * 1.05f, hair);
                pen.Dot(head + new Vector2(0, -ry * 0.4f), rx * 0.5f, hair.Darkened(0.12f));
                break;
            case HairStyle.Braids:
                pen.Dot(head, rx * 1.07f, hair);
                foreach (var side in new[] { -1f, 1f })
                {
                    for (var i = 0; i < 5; i++)
                    {
                        pen.Dot(head + new Vector2(side * rx * 0.55f, ry * (0.6f + (i * 0.34f))), rx * 0.24f, i == 4 ? look.Bobble ?? new Color("#E0559A") : hair);
                    }
                }

                break;
            case HairStyle.Pigtails:
                pen.Dot(head, rx * 1.06f, hair);
                foreach (var side in new[] { -1f, 1f })
                {
                    pen.Dot(head + new Vector2(side * rx * 1.3f, ry * 0.15f), rx * 0.42f, hair);
                    pen.Dot(head + new Vector2(side * rx * 1.02f, -ry * 0.12f), rx * 0.2f, look.Bobble ?? new Color("#E0559A"));
                }

                break;
            case HairStyle.Afro:
                pen.Dot(head + new Vector2(0, -ry * 0.25f), rx * 1.55f, hair);
                break;
            case HairStyle.Curly:
            case HairStyle.Perm:
                pen.Dot(head + new Vector2(0, -0.5f), rx * 1.1f, hair);
                for (var i = 0; i < 9; i++)
                {
                    var a = (Mathf.Pi * 0.9f) + (i * Mathf.Pi * 1.2f / 8);
                    pen.Dot(head + new Vector2(Mathf.Cos(a) * rx * 1.05f, Mathf.Sin(a) * ry * 1.05f), rx * 0.34f, hair);
                }

                break;
            case HairStyle.Turban:
                pen.Oval(head + new Vector2(0, -ry * 0.3f), rx * 1.22f, ry * 1.05f, hair, hair.Darkened(0.3f));
                break;
            default:
                // Short hair, ending above the neck.
                pen.Oval(head + new Vector2(0, -ry * 0.08f), rx * 1.05f, ry * 0.99f, hair);
                break;
        }

        if (look.Style is not (HairStyle.Bald or HairStyle.Balding or HairStyle.Turban))
        {
            pen.Arc(head + new Vector2(0, -1), rx * 0.8f, Mathf.Pi * 1.15f, Mathf.Pi * 1.45f, new Color(hair.Lightened(0.25f), 0.8f), 1f);
        }
    }

    /// <summary>The part of one disc that lies inside another: hair or a cap covering part of a head.</summary>
    private static Vector2[] Lens(Vector2 centre, float radius, Vector2 other, float otherRadius)
    {
        var d = centre.DistanceTo(other);
        if (d < 0.01f || d <= Math.Abs(radius - otherRadius))
        {
            return Ellipse(radius <= otherRadius ? centre : other, Math.Min(radius, otherRadius), Math.Min(radius, otherRadius), 20);
        }

        var theta = Mathf.Atan2(other.Y - centre.Y, other.X - centre.X);
        var alpha = Mathf.Acos(Mathf.Clamp(((d * d) + (radius * radius) - (otherRadius * otherRadius)) / (2 * d * radius), -1, 1));
        var beta = Mathf.Acos(Mathf.Clamp(((d * d) + (otherRadius * otherRadius) - (radius * radius)) / (2 * d * otherRadius), -1, 1));
        var points = new List<Vector2>();
        const int n = 12;
        for (var i = 0; i <= n; i++)
        {
            var a = theta - alpha + (2 * alpha * i / n);
            points.Add(centre + (new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius));
        }

        for (var i = 1; i < n; i++)
        {
            var a = theta + Mathf.Pi - beta + (2 * beta * i / n);
            points.Add(other + (new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * otherRadius));
        }

        return points.ToArray();
    }
}
