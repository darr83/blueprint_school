using BlueprintSchool.Simulation.World;

namespace BlueprintSchool.Simulation.Rooms;

/// <summary>What a room or area is used for.</summary>
public enum RoomKind
{
    /// <summary>Reception: the front desk, where visitors sign in and wait.</summary>
    Waiting,
    StaffRoom,
    Corridor,

    /// <summary>A general classroom: any subject can be taught here.</summary>
    Classroom,

    /// <summary>A small room for small groups and interventions.</summary>
    SmallGroupRoom,

    /// <summary>A science lab with benches, gas taps and sinks.</summary>
    ScienceLab,

    /// <summary>The science prep room, where the lab technician gets practicals ready.</summary>
    PrepRoom,

    /// <summary>A computer suite.</summary>
    IctSuite,

    ArtRoom,
    MusicRoom,
    DramaStudio,

    /// <summary>Design and technology workshop.</summary>
    DtWorkshop,

    /// <summary>Food and nutrition room, with cooking stations.</summary>
    FoodRoom,

    /// <summary>Sports hall: PE lessons, assemblies and exams.</summary>
    SportsHall,

    ChangingRoom,
    Library,

    /// <summary>Base for pupils with special educational needs and disabilities, run by the SENCO with TAs.</summary>
    SendBase,

    /// <summary>An office: a department office, the Head of Year office or the school office.</summary>
    Office,

    /// <summary>Where pupils removed from lessons go to work quietly.</summary>
    ReflectionRoom,

    /// <summary>First aid room for pupils who are unwell or hurt.</summary>
    MedicalRoom,

    /// <summary>Canteen: food counter, vending machines, water machine and tables.</summary>
    Cafeteria,

    /// <summary>A small alcove of vending machines and a water machine.</summary>
    VendingPoint,

    /// <summary>Toilets: a row of cubicles, wash basins and a hand dryer.</summary>
    Toilets,

    /// <summary>A single accessible toilet with grab rails and an emergency pull cord.</summary>
    AccessibleToilet,

    /// <summary>A single toilet for staff only.</summary>
    StaffToilet,

    /// <summary>A lift through every floor. Wheelchair users change floor only by lift.</summary>
    Lift,

    /// <summary>Stairs through every floor, for people on foot.</summary>
    Stairs,
}

/// <summary>A doorway in a room wall. <see cref="Offset"/> counts tiles along the wall from the room's corner.</summary>
public sealed record Doorway(WallSide Side, int Offset, int Width);

/// <summary>
/// A piece of furniture standing in a room. <see cref="X"/>/<see cref="Y"/> is its corner on the
/// site grid and <see cref="Width"/>/<see cref="Length"/> its footprint along X and Y, all in tiles.
/// </summary>
public sealed record PlacedFurniture(string ItemId, double X, double Y, double Width, double Length)
{
    public double CentreX => X + (Width / 2);

    public double CentreY => Y + (Length / 2);

    /// <summary>The tile under the middle of the item.</summary>
    public GridCoord Tile => new((int)Math.Floor(CentreX), (int)Math.Floor(CentreY));

    public bool Overlaps(double x, double y, double w, double l, double gap = 0) =>
        X < x + w + gap && x < X + Width + gap && Y < y + l + gap && y < Y + Length + gap;
}

/// <summary>A room or area in the school: a classroom, a corridor, reception, the canteen and so on.</summary>
public sealed class Room
{
    public required string Id { get; init; }

    /// <summary>Shown on the room's sign. The player can rename rooms.</summary>
    public required string Name { get; set; }

    public required RoomKind Kind { get; init; }

    public required string DepartmentId { get; init; }

    /// <summary>Footprint on the site grid.</summary>
    public required GridRect Rect { get; init; }

    /// <summary>Walls are drawn and block movement, except at the door. Open areas (corridors) have none.</summary>
    public bool HasWalls { get; init; } = true;

    public IReadOnlyList<Doorway> Doors { get; init; } = Array.Empty<Doorway>();

    /// <summary>The room template it was built from.</summary>
    public string TemplateId { get; init; } = string.Empty;

    /// <summary>Which wall the main door is in.</summary>
    public WallSide DoorSide { get; init; } = WallSide.South;

    /// <summary>Furniture and equipment, placed by <see cref="RoomLayoutEngine"/>.</summary>
    public IReadOnlyList<PlacedFurniture> Furniture { get; init; } = Array.Empty<PlacedFurniture>();

    /// <summary>Where people queue (at the reception desk or the canteen counter), nearest the front first.</summary>
    public IReadOnlyList<GridCoord> QueueTiles { get; init; } = Array.Empty<GridCoord>();

    /// <summary>What the room cost to build, in pounds (used for refunds).</summary>
    public int BuildCost { get; init; }

    /// <summary>Where a visitor or pupil stands or sits to use the room (in front of the desk, at the till).</summary>
    public GridCoord UseSpot { get; init; }

    /// <summary>Where staff stand or sit when working in the room.</summary>
    public GridCoord StaffSpot { get; init; }

    /// <summary>Seats: chairs in reception, at desks or round tables.</summary>
    public IReadOnlyList<GridCoord> Seats { get; init; } = Array.Empty<GridCoord>();

    /// <summary>In a cafeteria or vending point: the tile in front of each vending machine, where customers stand to buy.</summary>
    public IReadOnlyList<GridCoord> VendingSpots { get; init; } = Array.Empty<GridCoord>();

    /// <summary>In a toilet room: where someone stands to use each toilet (inside each cubicle, or by the single toilet).</summary>
    public IReadOnlyList<GridCoord> ToiletSpots { get; init; } = Array.Empty<GridCoord>();

    /// <summary>In a toilet room: the tile in front of each wash basin.</summary>
    public IReadOnlyList<GridCoord> SinkSpots { get; init; } = Array.Empty<GridCoord>();

    /// <summary>Public toilets, an accessible toilet or a staff toilet.</summary>
    public bool IsToilet => Kind is RoomKind.Toilets or RoomKind.AccessibleToilet or RoomKind.StaffToilet;

    /// <summary>A canteen or vending point, where people can get something to eat and drink.</summary>
    public bool IsRefreshments => Kind is RoomKind.Cafeteria or RoomKind.VendingPoint;

    /// <summary>A lift or stairs: one of these stands in the same spot on every floor.</summary>
    public bool IsShaft => Kind is RoomKind.Lift or RoomKind.Stairs;

    /// <summary>Staff member based in the room, e.g. the receptionist at the front desk.</summary>
    public int? ReservedByStaffId { get; set; }

    /// <summary>A room where lessons are taught.</summary>
    public bool IsTeachingRoom => IsTeaching(Kind);

    /// <summary>True for every kind of room where lessons are taught.</summary>
    public static bool IsTeaching(RoomKind kind) => kind is RoomKind.Classroom or RoomKind.SmallGroupRoom or RoomKind.ScienceLab
        or RoomKind.IctSuite or RoomKind.ArtRoom or RoomKind.MusicRoom or RoomKind.DramaStudio or RoomKind.DtWorkshop
        or RoomKind.FoodRoom or RoomKind.SportsHall or RoomKind.Library or RoomKind.SendBase;

    public double AreaSquareMetres => Units.AreaSquareMetres(Rect.Width, Rect.Length);

    /// <summary>True if the edge from <paramref name="inside"/> outward on <paramref name="side"/> is a doorway.</summary>
    public bool IsDoor(GridCoord inside, WallSide side)
    {
        foreach (var door in Doors.Where(d => d.Side == side))
        {
            var along = side is WallSide.North or WallSide.South ? inside.X - Rect.X : inside.Y - Rect.Y;
            if (along >= door.Offset && along < door.Offset + door.Width)
            {
                return true;
            }
        }

        return false;
    }
}
