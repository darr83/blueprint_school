using BlueprintSchool.Simulation.Core;
using BlueprintSchool.Simulation.Data;
using BlueprintSchool.Simulation.Hospital;
using BlueprintSchool.Simulation.Staff;
using BlueprintSchool.Simulation.World;

namespace BlueprintSchool.Simulation.Simulation;

/// <summary>Options for a new simulation.</summary>
public sealed class SimulationOptions
{
    public int Seed { get; init; } = Environment.TickCount;

    /// <summary>The player can keep spending when the balance is below zero (the money is still counted).</summary>
    public bool UnlimitedMoney { get; init; }
}

/// <summary>
/// The running school. Owns staff and rooms and advances them one simulation tick at a time. Contains no
/// engine code, so it can be simulated and tested without Godot. A new game starts with the empty school
/// building and money in the bank; pupils, lessons and the timetable come in later milestones.
/// </summary>
public sealed partial class SchoolSimulation
{
    private readonly List<StaffMember> _staff = new();
    private readonly Dictionary<string, CareSpace> _spaces = new();
    private readonly SimulationOptions _options;
    private int _nextStaffId = 1;

    public SchoolSimulation(GameDatabase database, SimulationClock clock, EventBus events, SiteMap site, SimulationOptions? options = null)
    {
        Database = database;
        Clock = clock;
        Events = events;
        Site = site;
        _options = options ?? new SimulationOptions();
        Random = new GameRandom(_options.Seed);
        Notifications = new NotificationLog(events);

        Nav = SiteLayout.BuildNavGrid(site, _spaces.Values);
        var entrance = site.Buildings[0].Entrances.First();
        WalkInSpawn = new GridCoord(entrance.Tile.X + 4, site.Buildings[0].Footprint.Bottom + 16);

        UnlimitedMoney = _options.UnlimitedMoney;
        UnlockStartingDepartments();
        RefreshCandidatesIfDue();
        Notifications.Add(Now, NotificationLevel.Info, "A new school: an empty building and money in the bank. Start with a reception, corridors and a staff room.");
    }

    public GameDatabase Database { get; }

    public SimulationClock Clock { get; }

    public EventBus Events { get; }

    public SiteMap Site { get; }

    public NavGrid Nav { get; private set; }

    public GameRandom Random { get; }

    public NotificationLog Notifications { get; }

    public DateTime Now => Clock.Now;

    public IReadOnlyList<StaffMember> Staff => _staff;

    public IEnumerable<CareSpace> Spaces => _spaces.Values;

    /// <summary>Where people arrive on the site and leave it: on the front path, by the road.</summary>
    public GridCoord WalkInSpawn { get; }

    public CareSpace Space(string id) => _spaces[id];

    public CareSpace? FindSpace(string id) => _spaces.GetValueOrDefault(id);

    /// <summary>Rooms of one kind, in a stable order.</summary>
    public IEnumerable<CareSpace> SpacesOfKind(CareSpaceKind kind) =>
        _spaces.Values.Where(s => s.Kind == kind).OrderBy(s => s.Id, StringComparer.Ordinal);

    public StaffMember? FindStaff(int id) => _staff.FirstOrDefault(s => s.Id == id);

    /// <summary>Advances the school by one tick. Called by the clock.</summary>
    public void Tick()
    {
        var dt = SimulationClock.TickLength.TotalSeconds;

        RefreshCandidatesIfDue();

        foreach (var member in _staff)
        {
            member.Move(dt);
        }

        UpdateGeneralAnnouncements();
        UpdateToilets(dt);
        UpdateStaff(dt);
        ChargeRunningCosts(dt);
        UpdateHistory();
    }

    // ---------------------------------------------------------------- helpers

    internal void WalkTo(StaffMember member, GridCoord destination)
    {
        if (member.Tile == destination && !member.IsMoving)
        {
            return;
        }

        if (member.Destination == destination)
        {
            return;
        }

        var path = PathFor(member, destination);
        if (path.Count > 0)
        {
            member.FollowPath(path);
        }
        else
        {
            member.PlaceAt(destination);
        }
    }

    /// <summary>Just inside the main entrance: where people go when there is nowhere better.</summary>
    internal GridCoord EntranceTile
    {
        get
        {
            var e = MainBuilding.Entrances.First();
            var (dx, dy) = WaitingRoomLayout.Step(WaitingRoomLayout.Opposite(e.Side));
            var middle = e.Side is WallSide.North or WallSide.South ? new GridCoord(e.Tile.X + (e.WidthTiles / 2), e.Tile.Y) : new GridCoord(e.Tile.X, e.Tile.Y + (e.WidthTiles / 2));
            return new GridCoord(middle.X + (2 * dx), middle.Y + (2 * dy));
        }
    }

    // ---------------------------------------------------------------- staff roster

    internal GridCoord HomeTile(StaffMember member)
    {
        if (member.BaseRoomId is { } roomId && FindSpace(roomId) is { } room)
        {
            return room.StaffSpot;
        }

        if (member.Role == StaffRole.Receptionist)
        {
            return MainWaitingRoom?.StaffSpot ?? SpacesOfKind(CareSpaceKind.Waiting).FirstOrDefault()?.StaffSpot ?? WaitingPlace(_staff.IndexOf(member));
        }

        // Everyone else waits in the staff room (from the far seats, leaving the near ones for staff on a
        // break); without one they wait in the main corridor.
        var index = Math.Max(0, _staff.Where(s => s.Role != StaffRole.Receptionist).ToList().IndexOf(member));
        var seats = SpacesOfKind(CareSpaceKind.StaffRoom).SelectMany(s => s.Seats).ToList();
        return seats.Count > 0 ? seats[seats.Count - 1 - (index % seats.Count)] : WaitingPlace(5 + index);
    }

    /// <summary>Somewhere in a corridor to stand, spread out by <paramref name="index"/>.</summary>
    internal GridCoord WaitingPlace(int index)
    {
        var corridor = SpacesOfKind(CareSpaceKind.Corridor).OrderByDescending(c => c.Rect.Area).FirstOrDefault() ?? SpacesOfKind(CareSpaceKind.Waiting).FirstOrDefault();
        if (corridor is null)
        {
            return EntranceTile;
        }

        var r = corridor.Rect;
        return new GridCoord(r.X + 1 + ((index * 3) % Math.Max(1, r.Width - 2)), r.Y + 1);
    }
}
