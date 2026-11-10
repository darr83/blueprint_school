using System.Collections.Generic;
using BlueprintSchool.Simulation.Data.Models;
using Godot;

namespace BlueprintSchool.UI;

/// <summary>
/// Line drawings for the build bar: one per department and one per kind of room, kept as SVG path
/// data on a 24 × 24 grid and turned into textures in any colour and size, so they stay crisp.
/// </summary>
public static class BuildIcons
{
    private static readonly Dictionary<string, string> Glyphs = new()
    {
        // Departments and the building.
        ["roller"] = "<rect x='3.5' y='3' width='14' height='5.5' rx='1.5'/><path d='M17.5 5.75h2.5v5.5h-8.5v3'/><rect x='10' y='14.25' width='3' height='7' rx='1'/>",
        ["building"] = "<path d='M3 21h18'/><path d='M5 21V9l7-5 7 5v12'/><path d='M9.5 21v-4.5h5V21'/><path d='M8.5 11h2M13.5 11h2M8.5 14h2M13.5 14h2'/>",
        ["book"] = "<path d='M12 6.5C10 5 7 4.5 3.5 5v13c3.5-.5 6.5 0 8.5 1.5 2-1.5 5-2 8.5-1.5V5C17 4.5 14 5 12 6.5z'/><path d='M12 6.5v13'/>",
        ["maths"] = "<rect x='3.5' y='3.5' width='17' height='17' rx='2'/><path d='M7 8h4M9 6v4M14 8h4M7 15.5l3 3M10 15.5l-3 3M14 15h4M14 18h4'/>",
        ["flask"] = "<path d='M9.5 3h5M10.5 3v6L5 18.5A1.8 1.8 0 0 0 6.6 21h10.8a1.8 1.8 0 0 0 1.6-2.5L13.5 9V3'/><path d='M7.5 14.5h9'/>",
        ["globe"] = "<circle cx='12' cy='12' r='8.5'/><path d='M3.5 12h17M12 3.5c2.5 2.5 3.5 5.5 3.5 8.5s-1 6-3.5 8.5c-2.5-2.5-3.5-5.5-3.5-8.5s1-6 3.5-8.5z'/>",
        ["speech"] = "<path d='M4 5h11a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2H9l-4 3.5V14H4a2 2 0 0 1-2-2V7a2 2 0 0 1 2-2z'/><path d='M19 9h1a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-1v3l-3.5-3H12'/>",
        ["monitor"] = "<rect x='3' y='4' width='18' height='12' rx='1.5'/><path d='M9 20h6M12 16v4'/><path d='M7.5 8.5l2.5 2-2.5 2M12 12.5h4'/>",
        ["palette"] = "<path d='M12 3.5a8.5 8.5 0 0 0 0 17c1.4 0 2-1 1.5-2.2-.6-1.4.4-2.8 1.9-2.8H18a3 3 0 0 0 3-3A8.5 8.5 0 0 0 12 3.5z'/><circle cx='7.5' cy='11' r='1.2'/><circle cx='10' cy='7' r='1.2'/><circle cx='14.5' cy='7' r='1.2'/>",
        ["note"] = "<path d='M9 18V5l11-2v13'/><circle cx='6.5' cy='18' r='2.5'/><circle cx='17.5' cy='16' r='2.5'/>",
        ["mask"] = "<path d='M4 4h16v7a8 8 0 0 1-16 0z'/><path d='M8 9h2M14 9h2M8.5 14a4 4 0 0 0 7 0'/>",
        ["saw"] = "<path d='M3 15l11-11 3 3-11 11z'/><path d='M6 12l1.5 1.5M9 9l1.5 1.5M12 6l1.5 1.5'/><path d='M16 14l5 5M19 14l-3 3'/>",
        ["pan"] = "<circle cx='10' cy='13' r='6'/><path d='M16 13h5.5'/><path d='M8 5c-1 1 1 1.5 0 3M12 4.5c-1 1 1 1.5 0 3'/>",
        ["ball"] = "<circle cx='12' cy='12' r='8.5'/><path d='M5 6.5c3 2 4 6 3 11M19 6.5c-3 2-4 6-3 11M3.5 12h17'/>",
        ["hand"] = "<path d='M8 13V5.5a1.5 1.5 0 0 1 3 0V11M11 10.5V4a1.5 1.5 0 0 1 3 0v6.5M14 10.5V5.5a1.5 1.5 0 0 1 3 0V14a7 7 0 0 1-7 7H9.5a5 5 0 0 1-4.2-2.3L3 15a1.5 1.5 0 0 1 2.5-1.6L8 16'/>",
        ["shield"] = "<path d='M12 3l8 3v6c0 4.5-3.4 8-8 9-4.6-1-8-4.5-8-9V6z'/><path d='M8.5 12l2.5 2.5 4.5-5'/>",
        ["facilities"] = "<path d='M4 9.5h12V14a5 5 0 0 1-5 5H9a5 5 0 0 1-5-5z'/><path d='M16 10.5h1.8a2.2 2.2 0 0 1 0 4.4H16'/><path d='M8 3c-1 1 1 2 0 3.5M12 3c-1 1 1 2 0 3.5'/><path d='M3.5 21.5h13'/>",

        // Rooms.
        ["bed"] = "<path d='M3 18V7M3 14h18v4'/><path d='M3 11h5a2 2 0 0 1 2 2v1'/><path d='M10 11h8a3 3 0 0 1 3 3'/><circle cx='6' cy='9' r='1.6'/>",
        ["whiteboard"] = "<rect x='3' y='3.5' width='18' height='12' rx='1'/><path d='M7 20.5l2-5M17 20.5l-2-5M7 8l3 3 3-4 4 3'/>",
        ["shelves"] = "<path d='M4 3v18M20 3v18M4 8h16M4 14h16M4 20h16'/><path d='M6.5 4.5v3M8.5 5v3M14 9.5v4M16 10v4M7 15.5v4'/>",
        ["chairs"] = "<path d='M5 12V7a1 1 0 0 1 1-1h3a1 1 0 0 1 1 1v5M14 12V7a1 1 0 0 1 1-1h3a1 1 0 0 1 1 1v5'/><path d='M3.5 12h17v3h-17zM5 15v4M19 15v4'/>",
        ["desk"] = "<path d='M3 10h18M5 10v10M19 10v10M14 10v5h5'/><rect x='7' y='3.5' width='7' height='5' rx='1'/>",
        ["people"] = "<circle cx='8' cy='8' r='2.8'/><circle cx='16.5' cy='9' r='2.3'/><path d='M3 19.5a5 5 0 0 1 10 0M13.5 19.5a4 4 0 0 1 7.5-1.5'/>",
        ["office"] = "<rect x='4' y='3' width='16' height='18' rx='1.5'/><path d='M8 8h8M8 12h8M8 16h4'/>",
        ["door"] = "<path d='M5 21V3h14v18'/><path d='M3 21h18'/><circle cx='15.5' cy='12' r='1'/>",
        ["corridor"] = "<path d='M3 20l5-16M21 20l-5-16'/><path d='M12 6v2.5M12 11v3M12 17v3.5'/>",
        ["lift"] = "<rect x='5' y='3' width='14' height='18' rx='1.5'/><path d='M12 3v18'/><path d='M7.5 9.5l1.5-2 1.5 2M13.5 7.5l1.5 2 1.5-2'/>",
        ["stairs"] = "<path d='M3 20h5v-5h5v-5h5V5h3'/>",
        ["floor"] = "<path d='M12 3l9 4.5-9 4.5-9-4.5z'/><path d='M3 12l9 4.5 9-4.5M3 16.5l9 4.5 9-4.5'/>",
        ["extend"] = "<rect x='3' y='7' width='10' height='10'/><path d='M13 7h2M17 7h4v4M21 13v4h-4M15 17h-2'/><path d='M15.5 12h4M17.5 10v4'/>",
        ["seat"] = "<path d='M7 11V5a1 1 0 0 1 1-1h8a1 1 0 0 1 1 1v6'/><path d='M5 11h14v4H5zM7 15v5M17 15v5'/>",
        ["vend"] = "<rect x='5' y='3' width='14' height='18' rx='1.5'/><path d='M8 7h5M8 10.5h5M8 14h5'/><path d='M16 7v2M8 17.5h8'/>",
        ["wc"] = "<path d='M6 4h5v7H6z'/><path d='M4 11h15a6 6 0 0 1-6 6H9a5 5 0 0 1-5-5z'/><path d='M9 17l-1 4h7l-1-4'/>",
        ["access"] = "<circle cx='11' cy='4' r='1.8'/><path d='M11 6.5V13h6l2 5'/><path d='M8 9.5a6 6 0 1 0 7.5 7.5'/>",
        ["reception"] = "<path d='M3 12h18v8H3z'/><circle cx='12' cy='6' r='2.5'/><path d='M8 12a4 4 0 0 1 8 0'/>",
        ["lock"] = "<rect x='5' y='11' width='14' height='10' rx='2'/><path d='M8 11V8a4 4 0 0 1 8 0v3'/>",
    };

    /// <summary>Drawings for particular rooms; anything else is picked by the room's kind and layout.</summary>
    private static readonly Dictionary<string, string> RoomGlyphs = new()
    {
        ["extend_building"] = "extend", ["add_floor"] = "floor", ["waiting_chairs"] = "seat", ["waiting_desk"] = "reception", ["waiting_doors"] = "door", ["corridor"] = "corridor",
        ["lift"] = "lift", ["stairs"] = "stairs",
        ["waiting_room"] = "reception", ["staff_room"] = "people", ["school_office"] = "office", ["medical_room"] = "bed", ["hoy_office"] = "office",
        ["classroom"] = "whiteboard", ["small_group_room"] = "people", ["english_classroom"] = "book", ["maths_classroom"] = "maths", ["humanities_classroom"] = "globe",
        ["languages_classroom"] = "speech", ["science_lab"] = "flask", ["prep_room"] = "flask", ["ict_suite"] = "monitor", ["art_room"] = "palette", ["music_room"] = "note",
        ["drama_studio"] = "mask", ["dt_workshop"] = "saw", ["food_room"] = "pan", ["sports_hall"] = "ball", ["changing_room"] = "seat",
        ["library"] = "shelves", ["send_base"] = "hand", ["reflection_room"] = "desk",
        ["cafeteria"] = "facilities", ["vending_point"] = "vend", ["accessible_toilet"] = "access",
    };

    private static readonly Dictionary<string, Texture2D> Cache = new();

    /// <summary>The drawing used for a room, special build item or room template.</summary>
    public static string RoomGlyph(string id, RoomTemplate? template)
    {
        if (RoomGlyphs.TryGetValue(id, out var glyph))
        {
            return glyph;
        }

        if (id.Contains("toilet", System.StringComparison.Ordinal))
        {
            return "wc";
        }

        return template?.Layout switch
        {
            RoomLayoutKind.Teaching => "whiteboard",
            RoomLayoutKind.Library => "shelves",
            RoomLayoutKind.Waiting => "chairs",
            RoomLayoutKind.Office => "office",
            RoomLayoutKind.StaffRoom => "people",
            _ => "door",
        };
    }

    public static bool Has(string glyph) => Glyphs.ContainsKey(glyph);

    /// <summary>A glyph drawn with <paramref name="colour"/> lines, <paramref name="pixels"/> square.</summary>
    public static Texture2D Get(string glyph, Color colour, int pixels)
    {
        var key = $"{glyph}|{colour.ToHtml(false)}|{pixels}";
        if (Cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var body = Glyphs.TryGetValue(glyph, out var paths) ? paths : Glyphs["door"];
        var svg = $"<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='#{colour.ToHtml(false)}' stroke-width='1.9' stroke-linecap='round' stroke-linejoin='round'>{body}</svg>";
        var image = new Image();
        var error = image.LoadSvgFromString(svg, pixels / 24f);
        Texture2D texture = error == Error.Ok ? ImageTexture.CreateFromImage(image) : new PlaceholderTexture2D { Size = new Vector2(pixels, pixels) };
        Cache[key] = texture;
        return texture;
    }
}
