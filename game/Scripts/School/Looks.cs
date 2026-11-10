using System;
using BlueprintSchool.Simulation.Staff;
using Godot;

namespace BlueprintSchool.School;

/// <summary>
/// Uniforms and appearances. Every person is drawn the same way by <see cref="Figures"/> from a
/// <see cref="PersonLook"/>; this class decides what each one looks like.
/// <para>
/// School staff dress as they would in a UK secondary school: teachers in smart office clothes
/// (experienced teachers in a jacket), teaching assistants in a school polo shirt, site staff in
/// hi-vis and the receptionist in a blazer. Everyone wears a lanyard. When a new staff role is
/// added, give it a look here in the same way: something no other role wears.
/// </para>
/// <para>
/// Visitors (and, later, pupils) are made from their id, sex and age, so each keeps one face, hair style
/// and outfit, children are child-sized and the elderly look their age.
/// </para>
/// </summary>
public static class Looks
{
    public static readonly Color Red = new("#E53935");
    public static readonly Color Amber = new("#F9A825");
    public static readonly Color Green = new("#43A047");

    private static readonly Color[] SkinTones =
    {
        new("#F5D3BC"), new("#F1C7A5"), new("#E0AC86"), new("#C68C5F"), new("#9C6B43"), new("#6E4A2E"),
    };

    private static readonly Color HairBlack = new("#1E1A18");
    private static readonly Color HairDark = new("#2B1E16");
    private static readonly Color HairBrown = new("#4A3020");
    private static readonly Color HairChestnut = new("#6B3A22");
    private static readonly Color HairLight = new("#8A5A33");
    private static readonly Color HairBlonde = new("#C9A15A");
    private static readonly Color HairGinger = new("#B5562A");
    private static readonly Color HairGrey = new("#9A9A9A");
    private static readonly Color HairSilver = new("#B8B8B8");
    private static readonly Color HairWhite = new("#D9D9D6");

    private static readonly Color[] TopColours =
    {
        new("#3E5C76"), new("#7A3B47"), new("#4D6B3C"), new("#8A6E2F"), new("#55476E"), new("#2F4F4F"), new("#C2571A"), new("#B5525C"),
        new("#7FB6A4"), new("#D9A441"), new("#4A4A52"), new("#3F6F8F"), new("#9C8AD0"), new("#E88AB0"), new("#6E2F3A"), new("#F2F4F7"),
    };

    private static readonly Color[] TrouserColours =
    {
        new("#2F3E57"), new("#3B3B3B"), new("#5B5048"), new("#24324A"), new("#6B6B6B"), new("#22262B"), new("#8A7F72"),
    };

    private static readonly Color[] JeansColours = { new("#2F3E57"), new("#6C8CB5"), new("#3F5F8A"), new("#24324A") };

    private static readonly Color[] ShirtColours =
    {
        new("#BFD7EE"), new("#F2F4F7"), new("#D8CDE8"), new("#F3D9DC"), new("#CFE3D4"),
    };

    private static readonly Color[] CapColours =
    {
        new("#1D3A6E"), new("#7A2E4A"), new("#2F7D6B"), new("#5A3D7A"), new("#C2571A"), new("#3B7DD8"),
    };

    private static readonly Color[] CoatColours =
    {
        new("#2F3E57"), new("#5B4636"), new("#3F5240"), new("#6E2F3A"), new("#4A4A52"), new("#8A6E4B"),
    };

    private static readonly Color[] ScarfColours = { new("#2F7D6B"), new("#1D3A6E"), new("#7A2E4A"), new("#55476E"), new("#B5525C"), new("#3B3B3B") };

    private static readonly Color[] FrameColours = { new("#2B2F33"), new("#7A2E4A"), new("#8A6A45"), new("#1D3A6E") };

    private static readonly Color[] BrightColours = { new("#F2C230"), new("#E0559A"), new("#3B7DD8"), new("#D03A3A"), new("#43A047") };

    private static readonly Color[] ShoeColours = { new("#2B2F33"), new("#3A2A1E"), new("#E8ECEF"), new("#5A3B22"), new("#3B7DD8"), new("#B5525C") };

    /// <summary>A repeatable roll for one person: the same id and question always give the same answer.</summary>
    private static int Roll(int seed, int question, int sides)
    {
        unchecked
        {
            var h = (uint)((seed * 73856093) ^ (question * 19349663) ^ 0x5bd1e995);
            h ^= h >> 15;
            h *= 2246822519u;
            h ^= h >> 13;
            h *= 3266489917u;
            h ^= h >> 16;
            return (int)(h % (uint)sides);
        }
    }

    private static T Pick<T>(int seed, int question, params T[] options) => options[Roll(seed, question, options.Length)];

    /// <summary>Hair colour: it follows skin tone loosely, and greys then whitens with age.</summary>
    private static Color HairColour(int seed, int skin, int age)
    {
        if (age >= 75)
        {
            return Pick(seed, 20, HairWhite, HairSilver, HairWhite, HairGrey);
        }

        if (age >= 58 && Roll(seed, 21, 4) != 0)
        {
            return Pick(seed, 20, HairGrey, HairSilver, HairGrey);
        }

        return skin >= 3
            ? Pick(seed, 22, HairBlack, HairDark, HairBlack, HairBrown)
            : Pick(seed, 22, HairDark, HairBrown, HairChestnut, HairLight, HairBlonde, HairGinger, HairBlack, HairBrown);
    }

    private static StaffLook Person(int seed, bool female, int age)
    {
        var skin = Roll(seed, 1, SkinTones.Length);
        return new StaffLook(seed, female, age, skin, SkinTones[skin], HairColour(seed, skin, age));
    }

    private readonly record struct StaffLook(int Seed, bool Female, int Age, int SkinIndex, Color Skin, Color Hair);

    /// <summary>The face everyone is born with: glasses, facial hair, freckles and laughter lines, by age and sex.</summary>
    private static PersonLook WithFace(PersonLook look, int seed, bool female, int age, int skin, bool neat)
    {
        var glasses = age >= 70 ? Roll(seed, 30, 3) != 0 : age >= 50 ? Roll(seed, 30, 2) == 0 : age >= 18 ? Roll(seed, 30, 5) == 0 : Roll(seed, 30, 9) == 0;
        var man = !female && age >= 20;
        var hair = man ? Roll(seed, 31, 10) : -1;
        var turban = look.Style == HairStyle.Turban;
        return look with
        {
            Female = female,
            Glasses = glasses,
            GlassesFrame = FrameColours[Roll(seed, 32, FrameColours.Length)],
            Beard = turban || hair is 0 or 1,
            Stubble = !turban && hair is 2 or 3 && !neat,
            Moustache = !turban && hair == 4 && age >= 38,
            Freckles = skin <= 1 && age < 32 && Roll(seed, 33, 4) == 0,
            Blush = age < 12,
            Wrinkles = age >= 62,
            Lips = female && age >= 17 && Roll(seed, 34, 2) == 0 ? Pick(seed, 35, new Color("#B5525C"), new Color("#A8455A"), new Color("#C2574A")) : null,
            Mouth = age < 12 ? Pick(seed, 36, Mouth.Grin, Mouth.Smile, Mouth.Flat) : Pick(seed, 36, Mouth.Smile, Mouth.Smile, Mouth.Flat, Mouth.Smile),
        };
    }

    /// <summary>A staff member's hair: long hair is tied back or covered (hair off the collar).</summary>
    private static HairStyle StaffHair(int seed, bool female, int age) => female
        ? Roll(seed, 10, 14) == 0 ? HairStyle.Hijab
            : age >= 55 ? Pick(seed, 11, HairStyle.Bob, HairStyle.Bun, HairStyle.Short, HairStyle.Perm)
            : Pick(seed, 11, HairStyle.Bun, HairStyle.Ponytail, HairStyle.Bob, HairStyle.Braids, HairStyle.Short, HairStyle.SidePart, HairStyle.Ponytail, HairStyle.Curly)
        : Roll(seed, 10, 18) == 0 ? HairStyle.Turban
            : age >= 52 ? Pick(seed, 11, HairStyle.Balding, HairStyle.Receding, HairStyle.Bald, HairStyle.Short, HairStyle.SidePart)
            : Pick(seed, 11, HairStyle.Short, HairStyle.Crop, HairStyle.SidePart, HairStyle.Quiff, HairStyle.Curly, HairStyle.Short, HairStyle.Receding);

    public static PersonLook Staff(StaffMember member)
    {
        var seed = member.Id;
        var age = member.Age > 0 ? member.Age : 25 + Roll(seed, 2, 35);

        // Most school staff are women, teaching assistants and receptionists especially; site staff mostly men.
        var female = member.Role switch
        {
            StaffRole.TeachingAssistant or StaffRole.Receptionist => Roll(seed, 3, 10) < 8,
            StaffRole.SiteStaff => Roll(seed, 3, 10) < 2,
            _ => Roll(seed, 3, 10) < 6,
        };
        var who = Person(seed, female, age);
        var style = StaffHair(seed, female, age);
        var hair = style is HairStyle.Hijab or HairStyle.Turban ? ScarfColours[Roll(seed, 12, ScarfColours.Length)] : who.Hair;
        var grade = member.Grade;
        bool Is(string text) => grade.Contains(text, StringComparison.OrdinalIgnoreCase);

        var look = member.Role switch
        {
            // Teachers: smart office clothes; experienced teachers in a jacket, some men in a tie.
            StaffRole.Teacher when Is("Experienced") => new PersonLook(ShirtColours[Roll(seed, 14, ShirtColours.Length)], new Color("#2E3440"), who.Skin, hair)
            {
                Kind = TopKind.Shirt, Jacket = Pick(seed, 16, new Color("#2B3A55"), new Color("#4A4A52"), new Color("#5B3A2E")),
                Tie = female ? null : Pick(seed, 15, new Color("#8E2448"), new Color("#1D3A6E"), new Color("#2F7D6B")), Lanyard = true, ShoeColour = new Color("#3A2A1E"),
            },
            StaffRole.Teacher => new PersonLook(ShirtColours[Roll(seed, 14, ShirtColours.Length)], new Color("#2E3440"), who.Skin, hair)
            {
                Kind = Pick(seed, 17, TopKind.Shirt, TopKind.Jumper, TopKind.Cardigan), Lanyard = true, ShoeColour = new Color("#3A2A1E"),
            },

            // Teaching assistants: a school polo shirt with a lanyard.
            StaffRole.TeachingAssistant => new PersonLook(new Color("#1E6B47"), new Color("#22262B"), who.Skin, hair)
            {
                Kind = TopKind.Polo, Lanyard = true, ShoeColour = new Color("#1C1F22"),
            },

            // Site staff: a hi-vis polo and a radio on the belt.
            StaffRole.SiteStaff => new PersonLook(new Color("#E0B020"), new Color("#22262B"), who.Skin, hair)
            {
                Kind = TopKind.Polo, HiVis = true, Radio = true, Lanyard = true, ShoeColour = new Color("#1C1F22"),
            },

            // Reception: a lilac blazer over a blouse.
            _ => new PersonLook(new Color("#F3F0FA"), new Color("#2E2E38"), who.Skin, hair)
            {
                Kind = TopKind.Shirt, Jacket = new Color("#7C6AA8"), Lanyard = true,
            },
        };

        look = look with { Style = style, Bobble = new Color("#F2C230"), Build = 0.92f + (Roll(seed, 4, 5) * 0.06f) };
        return WithFace(look, seed, female, age, who.SkinIndex, neat: true);
    }

    /// <summary>The colour that stands for a staff member's uniform in lists and on route lines.</summary>
    public static Color UniformColour(StaffMember member) => Staff(member).Jacket ?? Staff(member).Top;

    /// <summary>A member of the public in their own clothes, from who they are.</summary>
    private static PersonLook Civilian(int seed, bool female, int age, bool coat, bool bump)
    {
        var who = Person(seed, female, age);
        var toddler = age < 5;
        var child = age < 12;
        var teen = age is >= 12 and < 18;
        var old = age >= 70;
        var top = TopColours[Roll(seed, 5, TopColours.Length)];
        var bright = BrightColours[Roll(seed, 6, BrightColours.Length)];

        HairStyle style;
        if (child)
        {
            style = female
                ? Pick(seed, 10, HairStyle.Pigtails, HairStyle.Ponytail, HairStyle.Bob, HairStyle.Braids, HairStyle.Curly, HairStyle.Long)
                : Pick(seed, 10, HairStyle.Short, HairStyle.Crop, HairStyle.Curly, HairStyle.SidePart, HairStyle.Short);
        }
        else if (old)
        {
            style = female
                ? Pick(seed, 10, HairStyle.Perm, HairStyle.Bun, HairStyle.Bob, HairStyle.Short, HairStyle.Perm)
                : Pick(seed, 10, HairStyle.Balding, HairStyle.Bald, HairStyle.Receding, HairStyle.Short, HairStyle.Balding);
        }
        else if (female)
        {
            style = !teen && Roll(seed, 9, 12) == 0
                ? HairStyle.Hijab
                : Pick(seed, 10, HairStyle.Long, HairStyle.Wavy, HairStyle.Bob, HairStyle.Ponytail, HairStyle.Bun, HairStyle.Braids, HairStyle.Curly, HairStyle.Short, HairStyle.SidePart, who.SkinIndex >= 3 ? HairStyle.Afro : HairStyle.Long);
        }
        else
        {
            style = age >= 25 && Roll(seed, 9, 20) == 0 ? HairStyle.Turban
                : age >= 48 ? Pick(seed, 10, HairStyle.Receding, HairStyle.Balding, HairStyle.Bald, HairStyle.Short, HairStyle.SidePart, HairStyle.Crop)
                : Pick(seed, 10, HairStyle.Short, HairStyle.Crop, HairStyle.SidePart, HairStyle.Quiff, HairStyle.Curly, HairStyle.Short, age >= 32 ? HairStyle.Receding : HairStyle.Quiff, who.SkinIndex >= 3 ? HairStyle.Afro : HairStyle.Short);
        }

        // Clothes suit their age: dungarees and bright T-shirts on small children, hoodies on
        // teenagers, cardigans on the elderly.
        TopKind kind;
        if (toddler)
        {
            kind = Pick(seed, 40, TopKind.Dungarees, TopKind.Stripes, TopKind.Tee, TopKind.Dungarees);
        }
        else if (child)
        {
            kind = female ? Pick(seed, 40, TopKind.Tee, TopKind.Dress, TopKind.Stripes, TopKind.Hoodie, TopKind.Jumper) : Pick(seed, 40, TopKind.Tee, TopKind.Stripes, TopKind.Hoodie, TopKind.Jumper, TopKind.Stripes);
        }
        else if (teen)
        {
            kind = Pick(seed, 40, TopKind.Hoodie, TopKind.Tee, TopKind.Jumper, TopKind.Stripes, TopKind.Hoodie);
        }
        else if (old)
        {
            kind = female ? Pick(seed, 40, TopKind.Cardigan, TopKind.Dress, TopKind.Jumper, TopKind.Cardigan) : Pick(seed, 40, TopKind.Cardigan, TopKind.Jumper, TopKind.Shirt, TopKind.Cardigan);
        }
        else if (female)
        {
            kind = style == HairStyle.Hijab ? Pick(seed, 40, TopKind.Jumper, TopKind.Cardigan, TopKind.Shirt)
                : Pick(seed, 40, TopKind.Tee, TopKind.Dress, TopKind.Jumper, TopKind.Cardigan, TopKind.Shirt, TopKind.Stripes, TopKind.Hoodie, TopKind.Dress);
        }
        else
        {
            kind = Pick(seed, 40, TopKind.Tee, TopKind.Shirt, TopKind.Polo, TopKind.Jumper, TopKind.Hoodie, TopKind.Stripes, TopKind.Tee, TopKind.Shirt);
        }

        if (bump && kind is TopKind.Hoodie or TopKind.Cardigan or TopKind.Shirt)
        {
            kind = TopKind.Jumper;
        }

        if (kind is TopKind.Shirt)
        {
            top = ShirtColours[Roll(seed, 41, ShirtColours.Length)];
        }

        var legs = kind == TopKind.Dress ? LegKind.Skirt
            : female && style != HairStyle.Hijab && Roll(seed, 42, 4) == 0 ? LegKind.Skirt
            : (child || (!female && age < 40)) && Roll(seed, 43, 4) == 0 ? LegKind.Shorts
            : !old && Roll(seed, 44, 2) == 0 ? LegKind.Jeans : LegKind.Trousers;
        var trousers = legs == LegKind.Jeans ? JeansColours[Roll(seed, 45, JeansColours.Length)]
            : toddler ? Pick(seed, 45, new Color("#4D7FB8"), new Color("#B5525C"), new Color("#4D6B3C"))
            : TrouserColours[Roll(seed, 45, TrouserColours.Length)];
        var light = top.Luminance > 0.6f;
        var build = child ? 1f : 0.9f + (Roll(seed, 46, 8) * 0.05f);

        var look = new PersonLook(top, trousers, who.Skin, style is HairStyle.Hijab or HairStyle.Turban ? ScarfColours[Roll(seed, 12, ScarfColours.Length)] : who.Hair)
        {
            Kind = kind,
            Trim = kind switch
            {
                TopKind.Stripes => light ? bright : new Color("#F2F4F7"),
                TopKind.Dress => Roll(seed, 47, 3) == 0 ? null : top.Lightened(0.6f),
                TopKind.Cardigan => ShirtColours[Roll(seed, 41, ShirtColours.Length)],
                _ => null,
            },
            Motif = kind == TopKind.Tee && (child || Roll(seed, 48, 4) == 0) ? (light ? bright : new Color("#FBE3A0")) : null,
            Legs = legs,
            Tights = legs == LegKind.Skirt && (old || Roll(seed, 49, 2) == 0) ? (child ? new Color("#F2F4F7") : old ? who.Skin.Darkened(0.18f) : new Color("#3B3B3B")) : null,
            ShoeColour = child ? BrightColours[Roll(seed, 50, BrightColours.Length)] : ShoeColours[Roll(seed, 50, old ? 2 : ShoeColours.Length)],
            LongSleeves = style == HairStyle.Hijab || (kind is TopKind.Shirt && Roll(seed, 51, 2) == 0),
            Style = style,
            Bobble = bright,
            Scale = toddler ? 0.5f : child ? Mathf.Lerp(0.6f, 0.78f, (age - 5) / 6f) : teen ? Mathf.Lerp(0.84f, 0.97f, (age - 12) / 5f) : old ? 0.95f : female ? 0.97f : 1f,
            HeadScale = toddler ? 0.8f : child ? 0.88f : teen ? 0.97f : 1f,
            Build = build,
            Belly = !female && age >= 40 && build >= 1.15f,
            Stoop = age >= 72,
            Jacket = coat && !bump && !child && kind is not (TopKind.Hoodie or TopKind.Cardigan or TopKind.Dress) ? CoatColours[Roll(seed, 52, CoatColours.Length)] : null,
            Stick = age >= 78 && age < 86,
            Frame = age >= 86,
            Bump = bump,
            Teddy = toddler || (age < 8 && Roll(seed, 53, 3) == 0),
        };

        return WithFace(look, seed, female, age, who.SkinIndex, neat: false);
    }

    /// <summary>A member of the public of a given age, for the art review sheets (and visitors later on).</summary>
    public static PersonLook Preview(int seed, bool female, int age, bool coat = false) => Civilian(seed, female, age, coat, bump: false);
}
