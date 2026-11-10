using BlueprintSchool.Simulation.Rooms;

namespace BlueprintSchool.Simulation.Data.Models;

/// <summary>A preset size offered in the build menu.</summary>
public sealed class RoomSize
{
    public string Name { get; init; } = string.Empty;

    /// <summary>Along the wall with the door, in metres.</summary>
    public double WidthM { get; init; }

    /// <summary>From the door wall to the back wall, in metres.</summary>
    public double LengthM { get; init; }
}

/// <summary>Which furniture layout a room uses. Each layout adapts to the room's size and door direction.</summary>
public enum RoomLayoutKind
{
    /// <summary>No furniture: corridors.</summary>
    None,

    /// <summary>
    /// Rows of pupil workstations (desks, lab benches, computers, workbenches) facing the teaching
    /// wall at the back of the room, with the teacher's desk at the front. More rows in a bigger room.
    /// </summary>
    Teaching,

    /// <summary>An open floor kept clear for PE, drama or assemblies, with a few items along the walls.</summary>
    Hall,

    /// <summary>Bookshelves along the walls and tables of four in the middle.</summary>
    Library,

    /// <summary>A reception desk with a queue, and rows of seats.</summary>
    Waiting,

    /// <summary>Desks with chairs (offices, the prep room, the reflection room).</summary>
    Office,

    /// <summary>A table, chairs and a kitchenette.</summary>
    StaffRoom,

    /// <summary>A food counter on the back wall, vending machines (up to 4) and a water machine, and tables of four filling the floor.</summary>
    Cafeteria,

    /// <summary>A row of vending machines (up to 4) and a water machine, with a seat or two if there is room.</summary>
    Vending,

    /// <summary>Toilets: a row of cubicles along the back wall with wash basins and a hand dryer, or (with no cubicles) a single toilet and basin.</summary>
    Toilets,

    /// <summary>A lift car: empty floor, drawn as the lift. It stands in the same place on every floor.</summary>
    Lift,

    /// <summary>A flight of stairs, drawn as the steps. It stands in the same place on every floor.</summary>
    Stairs,
}

/// <summary>
/// A room the player can build: what it is for, the sizes it comes in, what it costs and how
/// it is furnished. Loaded from Data/RoomTemplates.
/// </summary>
public sealed class RoomTemplate : IDataRecord
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    /// <summary>Build menu tab, e.g. "Teaching" or "Staff".</summary>
    public string Category { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public RoomKind Kind { get; init; }

    public string Department { get; init; } = string.Empty;

    public RoomLayoutKind Layout { get; init; }

    public IReadOnlyList<RoomSize> Sizes { get; init; } = Array.Empty<RoomSize>();

    public double MinWidthM { get; init; }

    public double MinLengthM { get; init; }

    public double MaxWidthM { get; init; }

    public double MaxLengthM { get; init; }

    public double DoorWidthM { get; init; } = 1.0;

    /// <summary>True if the room has a second door opposite the first (reception's street entrance).</summary>
    public bool DoorBothEnds { get; init; }

    /// <summary>For teaching rooms: the most pupils the room is laid out for (0 for no limit).</summary>
    public int MaxPupils { get; init; }

    /// <summary>Fixed cost to fit out the room, in pounds.</summary>
    public int BaseCost { get; init; }

    /// <summary>Construction cost per square metre of floor, in pounds.</summary>
    public int CostPerSquareMetre { get; init; }

    /// <summary>Who is needed to run the room, shown in the build menu.</summary>
    public string Staffing { get; init; } = string.Empty;

    /// <summary>Furniture and equipment ids the layout may use; checked against Data/Furniture.</summary>
    public IReadOnlyList<string> Furniture { get; init; } = Array.Empty<string>();

    /// <summary>True for corridors: open floor with no walls or furniture.</summary>
    public bool IsCorridor => Kind == RoomKind.Corridor;

    /// <summary>True for lifts and stairs, which are built through every floor at once.</summary>
    public bool IsShaft => Kind is RoomKind.Lift or RoomKind.Stairs;

    public void Validate(ValidationContext context)
    {
        context.RequireId("id", Id);
        context.RequireText("name", Name);
        context.RequireText("category", Category);
        context.RequireId("department", Department);
        context.RequireRange("min_width_m", MinWidthM, 1, 60);
        context.RequireRange("min_length_m", MinLengthM, 1, 60);
        context.RequireRange("max_width_m", MaxWidthM, MinWidthM, 60);
        context.RequireRange("max_length_m", MaxLengthM, MinLengthM, 60);
        context.RequireRange("door_width_m", DoorWidthM, 0.5, 10);
        context.RequireRange("base_cost", BaseCost, 0, 10_000_000);
        context.RequireRange("cost_per_square_metre", CostPerSquareMetre, 0, 100_000);
        context.RequireIds("furniture", Furniture);
        context.RequireRange("max_pupils", MaxPupils, 0, 300);

        if (Sizes.Count == 0)
        {
            context.Error("sizes", "must offer at least one size.");
        }

        for (var i = 0; i < Sizes.Count; i++)
        {
            var size = Sizes[i];
            context.RequireText($"sizes[{i}].name", size.Name);
            context.RequireRange($"sizes[{i}].width_m", size.WidthM, MinWidthM, MaxWidthM);
            context.RequireRange($"sizes[{i}].length_m", size.LengthM, MinLengthM, MaxLengthM);
        }

        if ((Layout == RoomLayoutKind.None) != IsCorridor)
        {
            context.Error("layout", "corridors, and only corridors, use the 'none' layout.");
        }
    }
}
