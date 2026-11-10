using System;
using System.Collections.Generic;
using System.Linq;
using BlueprintSchool.Simulation.Agents;
using BlueprintSchool.Simulation.Core;
using BlueprintSchool.Simulation.Data.Models;
using BlueprintSchool.Simulation.Rooms;
using BlueprintSchool.Simulation.Simulation;
using BlueprintSchool.Simulation.Staff;
using BlueprintSchool.Simulation.World;
using Godot;

namespace BlueprintSchool.School;

/// <summary>
/// Everything that stands up from the floor: walls, furniture, equipment and people.
/// Each object is its own node positioned at its depth anchor, and Godot's Y-sort draws them
/// back to front, so people pass correctly behind and in front of walls and desks.
/// </summary>
public partial class WorldLayer : Node2D
{
    /// <summary>Height of every wall on screen (2.5 m), level with the top of the door frames.</summary>
    public const float WallHeight = RoomSigns.FrameHeight;

    /// <summary>Wall thickness in tiles.</summary>
    public const double WallThickness = 0.3;

    private static readonly Color WallTop = new("#F4F6F8");
    private static readonly Color WallFace = new("#D9DFE5");

    private readonly Dictionary<Agent, AgentNode> _agentNodes = new();
    /// <summary>Every seat, and the way someone sitting on it looks.</summary>
    private readonly Dictionary<GridCoord, WallSide> _seatTiles = new();

    /// <summary>How far each placed chair stands off the middle of its tile, away from the walls beside it.</summary>
    private readonly Dictionary<GridCoord, (double X, double Y)> _chairShifts = new();
    private readonly List<Node> _staticNodes = new();
    private IDisposable? _layoutSubscription;
    private SchoolSimulation _sim = null!;
    private SimulationClock _clock = null!;

    public SchoolSimulation Simulation => _sim;

    public SimulationClock Clock => _clock;

    /// <summary>Seconds since the scene started, for animation.</summary>
    public double Time { get; private set; }

    /// <summary>The member of staff the player has selected, if any.</summary>
    public Agent? Selected { get; set; }

    public void Initialise(SchoolSimulation simulation, SimulationClock clock)
    {
        _sim = simulation;
        _clock = clock;
        YSortEnabled = true;
        _layoutSubscription = _sim.Events.Subscribe<LayoutChanged>(_ => _layoutDirty = true);
        RebuildStatic();
    }

    private bool _layoutDirty;

    public override void _ExitTree() => _layoutSubscription?.Dispose();

    /// <summary>Rebuilds walls and furniture, after rooms are built, moved or demolished.</summary>
    public void RebuildStatic()
    {
        foreach (var node in _staticNodes)
        {
            node.QueueFree();
        }

        _staticNodes.Clear();
        _seatTiles.Clear();
        _chairShifts.Clear();

        foreach (var space in _sim.Spaces)
        {
            foreach (var seat in space.Seats)
            {
                _seatTiles[seat] = WallSide.South;
            }

            foreach (var item in space.Furniture)
            {
                if (_sim.Database.Furniture.TryGetValue(item.ItemId, out var def) && def.Style == FurnitureStyle.Chair)
                {
                    _seatTiles[item.Tile] = RoomChairFacing(space, item);
                }
            }
        }

        foreach (var chair in _sim.Chairs)
        {
            _seatTiles[chair.Tile] = chair.Facing;
            _chairShifts[chair.Tile] = _sim.ChairShift(chair.Tile);
        }

        BuildWalls();
        BuildFurniture();
        foreach (var (anchor, draw) in Decorations.Plan(_sim))
        {
            AddProp(anchor, draw);
        }

        _layoutDirty = false;
    }

    public override void _Process(double delta)
    {
        Time += delta;
        if (_layoutDirty)
        {
            RebuildStatic();
        }

        SyncAgents();
    }

    // ------------------------------------------------------------------ people

    /// <summary>World position of an agent's feet, interpolated between simulation ticks.</summary>
    public Vector2 FootPosition(Agent agent)
    {
        var (x, y) = Interpolated(agent);
        if (IsSeated(agent))
        {
            // Sitting on a chair, which stands a little forward of the middle of its tile.
            var (cx, cy) = ChairArt.Centre(agent.Tile, SeatFacing(agent), _chairShifts.TryGetValue(agent.Tile, out var shift) ? shift : null);
            (x, y) = (x + (cx - (agent.Tile.X + 0.5)), y + (cy - (agent.Tile.Y + 0.5)));
        }

        return IsoDraw.ToScreen(x, y);
    }

    public (double X, double Y) Interpolated(Agent agent)
    {
        // Just stepped out of the lift onto another floor: no sliding across from the floor below.
        if (IsoDraw.FloorOf(agent.PreviousY) != IsoDraw.FloorOf(agent.Y))
        {
            return (agent.X, agent.Y);
        }

        var t = _clock.TickProgress;
        return (agent.PreviousX + ((agent.X - agent.PreviousX) * t), agent.PreviousY + ((agent.Y - agent.PreviousY) * t));
    }

    /// <summary>True if the agent is on the floor in view.</summary>
    public bool OnViewFloor(Agent agent) => IsoDraw.OnViewFloor(agent.Y);

    /// <summary>The rooms on the floor in view.</summary>
    private IEnumerable<Room> ViewSpaces => _sim.Spaces.Where(s => IsoDraw.OnViewFloor(s.Rect.Y));

    /// <summary>The member of staff under a world-space point, nearest the viewer first.</summary>
    public Agent? HitTest(Vector2 world)
    {
        Agent? best = null;
        var bestDepth = float.MinValue;
        foreach (var (agent, node) in _agentNodes)
        {
            if (!node.Visible)
            {
                continue;
            }

            var foot = FootPosition(agent);
            var box = new Rect2(foot.X - 14, foot.Y - 62, 28, 66);
            if (box.HasPoint(world) && foot.Y > bestDepth)
            {
                best = agent;
                bestDepth = foot.Y;
            }
        }

        return best;
    }

    public bool IsSeated(Agent agent) => !agent.IsMoving && _seatTiles.ContainsKey(agent.Tile);

    /// <summary>The way someone sitting where <paramref name="agent"/> is looks.</summary>
    public WallSide SeatFacing(Agent agent) => _seatTiles.GetValueOrDefault(agent.Tile, WallSide.South);

    private void SyncAgents()
    {
        var present = new HashSet<Agent>();
        // Off-duty staff are at home, so they aren't drawn.
        foreach (var s in _sim.Staff.Where(s => s.IsInBuilding))
        {
            present.Add(s);
        }

        foreach (var gone in _agentNodes.Keys.Where(a => !present.Contains(a)).ToList())
        {
            _agentNodes[gone].QueueFree();
            _agentNodes.Remove(gone);
        }

        foreach (var agent in present)
        {
            if (!_agentNodes.TryGetValue(agent, out var node))
            {
                node = new AgentNode(this, agent);
                _agentNodes[agent] = node;
                AddChild(node);
            }

            node.Sync();
        }
    }

    // ------------------------------------------------------------------ walls

    private void BuildWalls()
    {
        // Collect wall edges from the building shell and every walled room. An edge is a wall
        // if anything wants a wall there and nothing wants a doorway there.
        var walls = new HashSet<(int X, int Y, bool Horizontal)>();
        var doors = new HashSet<(int X, int Y, bool Horizontal)>();

        void Perimeter(GridRect r, Func<GridCoord, WallSide, bool> isOpening)
        {
            for (var x = r.X; x < r.Right; x++)
            {
                (isOpening(new GridCoord(x, r.Y), WallSide.North) ? doors : walls).Add((x, r.Y, true));
                (isOpening(new GridCoord(x, r.Bottom - 1), WallSide.South) ? doors : walls).Add((x, r.Bottom, true));
            }

            for (var y = r.Y; y < r.Bottom; y++)
            {
                (isOpening(new GridCoord(r.X, y), WallSide.West) ? doors : walls).Add((r.X, y, false));
                (isOpening(new GridCoord(r.Right - 1, y), WallSide.East) ? doors : walls).Add((r.Right, y, false));
            }
        }

        // The outer walls of the floor in view; only the ground floor has entrances.
        var up = IsoDraw.ViewFloor * _sim.Site.FloorStride;
        foreach (var building in _sim.Site.Buildings)
        {
            foreach (var (inside, _, side) in building.OuterEdges())
            {
                var edge = side switch
                {
                    WallSide.North => (inside.X, inside.Y + up, true),
                    WallSide.South => (inside.X, inside.Y + 1 + up, true),
                    WallSide.West => (inside.X, inside.Y + up, false),
                    _ => (inside.X + 1, inside.Y + up, false),
                };
                // A room built over an entrance (a waiting room by the front path) closes it unless it has a door there.
                var open = up == 0 && building.IsOpening(inside, side)
                    && _sim.Spaces.Where(s => s.HasWalls && s.Rect.Contains(inside)).All(s => s.IsDoor(inside, side));
                (open ? doors : walls).Add(edge);
            }
        }

        foreach (var space in ViewSpaces.Where(s => s.HasWalls))
        {
            Perimeter(space.Rect, space.IsDoor);
        }

        // Every wall is cut into one-tile pieces that sort on their own, joined by a square post at
        // every grid point a wall touches. Faces that meet a neighbour are left out, so straight
        // runs, corners, T-junctions and crossings all join seamlessly from all four view angles.
        var solid = walls.Where(w => !doors.Contains(w)).ToHashSet();
        var joints = new Dictionary<(int X, int Y), Sides>();
        void Join((int X, int Y) at, Sides side) => joints[at] = joints.GetValueOrDefault(at) | side;
        foreach (var (x, y, horizontal) in solid)
        {
            if (horizontal)
            {
                Join((x, y), Sides.East);
                Join((x + 1, y), Sides.West);
            }
            else
            {
                Join((x, y), Sides.South);
                Join((x, y + 1), Sides.North);
            }
        }

        var doorPoints = new HashSet<(int X, int Y)>();
        foreach (var (x, y, horizontal) in doors.Where(d => !solid.Contains(d)))
        {
            doorPoints.Add((x, y));
            doorPoints.Add(horizontal ? (x + 1, y) : (x, y + 1));
        }

        const double h = WallThickness / 2;
        const double span = 1 - WallThickness;

        // Which walled room each tile is in, so each face of a wall takes the finish of the room it looks into.
        var roomAt = new Dictionary<GridCoord, Room>();
        foreach (var space in ViewSpaces.Where(s => s.HasWalls).OrderByDescending(s => s.Rect.Area))
        {
            var r = space.Rect;
            for (var tx = r.X; tx < r.Right; tx++)
            {
                for (var ty = r.Y; ty < r.Bottom; ty++)
                {
                    roomAt[new GridCoord(tx, ty)] = space;
                }
            }
        }

        WallPaint?[] Paint(double px, double py, double pw, double pl)
        {
            var faces = new WallPaint?[4];
            var (cx, cy) = (px + (pw / 2), py + (pl / 2));
            var outward = new[] { (cx, py - 0.4), (px + pw + 0.4, cy), (cx, py + pl + 0.4), (px - 0.4, cy) };
            for (var i = 0; i < 4; i++)
            {
                var tile = new GridCoord((int)Math.Floor(outward[i].Item1), (int)Math.Floor(outward[i].Item2));
                if (roomAt.TryGetValue(tile, out var room) && DecorArt.LookOf(_sim, room) is { Walls: not WallFinish.Standard } look)
                {
                    faces[i] = new WallPaint(look.Walls, DecorArt.WallColour(look), DecorArt.WallAccent(room, look));
                }
            }

            return faces;
        }

        foreach (var (x, y, horizontal) in solid)
        {
            if (horizontal)
            {
                var paint = Paint(x + h, y - h, span, WallThickness);
                AddProp(FrontFace(x + h, y - h, span, WallThickness), c => Wall(c, x + h, y - h, span, WallThickness, Sides.East | Sides.West, paint: paint));
            }
            else
            {
                var paint = Paint(x - h, y + h, WallThickness, span);
                AddProp(FrontFace(x - h, y + h, WallThickness, span), c => Wall(c, x - h, y + h, WallThickness, span, Sides.North | Sides.South, paint: paint));
            }
        }

        foreach (var ((x, y), joined) in joints)
        {
            // Posts beside a doorway are its frame, in a darker trim.
            var frame = doorPoints.Contains((x, y));
            var paint = frame ? null : Paint(x - h, y - h, WallThickness, WallThickness);
            AddProp(FrontFace(x - h, y - h, WallThickness, WallThickness), c => Wall(c, x - h, y - h, WallThickness, WallThickness, joined, frame, paint));
        }

        // Each room's doorway: frame, doors and name board.
        foreach (var space in ViewSpaces.Where(RoomSigns.Named))
        {
            var (fx, fy, fw, fl) = RoomSigns.SignFootprint(space);
            var (mx, my) = RoomSigns.DoorMiddle(space);
            AddProp(FrontFace(fx, fy, fw, fl), c => RoomSigns.Doorway(c, space, DoorOpenness(space, mx, my), WallThickness, WallHeight, WallTop, WallFace, FrameTop, FrameFace), animated: true);
        }
    }

    private readonly Dictionary<Room, (float Open, double At)> _doors = new();

    /// <summary>How far a room's door stands open: it swings open as someone comes near and closes behind them.</summary>
    private float DoorOpenness(Room space, double mx, double my)
    {
        static bool Near(Agent a, double x, double y) => Math.Abs(a.X - x) < 1.6 && Math.Abs(a.Y - y) < 1.6;
        var near = _sim.Staff.Any(s => Near(s, mx, my));
        var (open, at) = _doors.GetValueOrDefault(space, (0f, Time));
        var step = (float)Math.Clamp((Time - at) * 4, 0, 1);
        open = Math.Clamp(open + (near ? step : -step), 0, 1);
        _doors[space] = (open, Time);
        return open;
    }

    private static readonly Color WallSkirting = new("#9FB0BE");
    private static readonly Color WallRim = new("#C3CDD6");
    private static readonly Color FrameFace = new("#A9B7C3");
    private static readonly Color FrameTop = new("#C7D2DB");

    /// <summary>A decorated wall face: its finish, colour and second colour.</summary>
    private sealed record WallPaint(WallFinish Finish, Color Colour, Color? Accent);

    private static void Wall(CanvasItem c, double x, double y, double w, double l, Sides joined, bool frame = false, WallPaint?[]? paint = null)
    {
        IsoDraw.Prism(c, x, y, w, l, WallHeight, frame ? FrameTop : WallTop, frame ? FrameFace : WallFace, joined, skirting: frame ? FrameFace.Darkened(0.15f) : WallSkirting, rim: WallRim);
        if (paint is null)
        {
            return;
        }

        foreach (var (a, b, side, left) in IsoDraw.VisibleFaces(x, y, w, l))
        {
            var index = side switch { Sides.North => 0, Sides.East => 1, Sides.South => 2, _ => 3 };
            if ((joined & side) == 0 && paint[index] is { } face)
            {
                DecorArt.DrawWallFace(c, a, b, WallHeight, left ? 0f : 0.1f, face.Finish, face.Colour, face.Accent, WallSkirting);
            }
        }
    }

    /// <summary>The middle of whichever side of a footprint is nearest the viewer, so thin pieces sort right from every angle.</summary>
    internal static Vector2 FrontFace(double x, double y, double w, double l)
    {
        var sides = new[]
        {
            IsoDraw.ToScreen(x + (w / 2), y), IsoDraw.ToScreen(x + (w / 2), y + l),
            IsoDraw.ToScreen(x, y + (l / 2)), IsoDraw.ToScreen(x + w, y + (l / 2)),
        };
        return sides.MaxBy(p => p.Y);
    }

    // ------------------------------------------------------------------ furniture

    private void BuildFurniture()
    {
        foreach (var space in ViewSpaces)
        {
            foreach (var item in space.Furniture)
            {
                if (!_sim.Database.Furniture.TryGetValue(item.ItemId, out var def))
                {
                    continue;
                }

                var colour = new Color(def.Colour);
                var height = (float)(def.HeightM * IsoDraw.PixelsPerMetre);
                switch (def.Style)
                {
                    case FurnitureStyle.Bed:
                        AddBed(item.Tile);
                        break;
                    case FurnitureStyle.Chair:
                        Chair(item.Tile, RoomChairFacing(space, item), colour);
                        break;
                    case FurnitureStyle.Box when ToiletArt.Draws(def.Id):
                        ToiletArt.Add(def.Id, space, item, colour, _sim, (anchor, draw, animated) => AddProp(anchor, draw, animated));
                        break;
                    case FurnitureStyle.Box when CafeArt.Draws(def.Id):
                        CafeArt.Add(def.Id, space, item, (float)(def.HeightM * IsoDraw.PixelsPerMetre), colour, (anchor, draw) => AddProp(anchor, draw));
                        break;
                    default:
                        Block(item.ItemId, item.X, item.Y, item.Width, item.Length, (float)(def.HeightM * IsoDraw.PixelsPerMetre), colour, FurnitureArt.FrontOf(space, item));
                        break;
                }
            }
        }

        foreach (var chair in _sim.Chairs.Where(c => IsoDraw.OnViewFloor(c.Tile.Y)))
        {
            Chair(chair.Tile, chair.Facing, new Color(chair.Colour), _chairShifts.GetValueOrDefault(chair.Tile));
        }
    }

    /// <summary>A made bed (the medical-room leftovers from the hospital; schools will have their own furniture).</summary>
    private void AddBed(GridCoord spot)
    {
        var (cx, cy) = (spot.X + 0.5, spot.Y + 0.5);
        AddProp(IsoDraw.ToScreen(cx, cy), c => Figures.Bed(c, cx, cy, made: true, false, _ => { }));
    }

    /// <summary>A chair in two parts, so whoever sits on it sorts between the seat and the back.</summary>
    private void Chair(GridCoord tile, WallSide facing, Color colour, (double X, double Y)? shift = null)
    {
        AddProp(ChairArt.SeatAnchor(tile, facing, shift), c => ChairArt.Seat(c, tile, facing, colour, shift));
        AddProp(ChairArt.BackAnchor(tile, facing, shift), c => ChairArt.Back(c, tile, facing, colour, shift));
    }

    /// <summary>A chair in a room faces the desk or table beside it, otherwise south.</summary>
    private WallSide RoomChairFacing(Room space, PlacedFurniture chair)
    {
        var (cx, cy) = (chair.Tile.X + 0.5, chair.Tile.Y + 0.5);
        PlacedFurniture? best = null;
        var bestGap = 0.75;
        foreach (var item in space.Furniture)
        {
            if (ReferenceEquals(item, chair) || !_sim.Database.Furniture.TryGetValue(item.ItemId, out var def) || def.Style == FurnitureStyle.Chair)
            {
                continue;
            }

            var gx = Math.Max(0, Math.Max(item.X - cx, cx - (item.X + item.Width)));
            var gy = Math.Max(0, Math.Max(item.Y - cy, cy - (item.Y + item.Length)));
            var gap = Math.Max(gx, gy);
            if (gap < bestGap)
            {
                (best, bestGap) = (item, gap);
            }
        }

        if (best is null)
        {
            return WallSide.South;
        }

        var (dx, dy) = (best.CentreX - cx, best.CentreY - cy);
        return Math.Abs(dx) > Math.Abs(dy) ? (dx > 0 ? WallSide.East : WallSide.West) : (dy > 0 ? WallSide.South : WallSide.North);
    }

    /// <summary>A piece of furniture, split into one-tile slices along its length so each slice sorts correctly.</summary>
    private void Block(string itemId, double x, double y, double w, double l, float height, Color colour, Sides front = Sides.None)
    {
        var alongX = w >= l;
        var length = alongX ? w : l;
        var slices = Math.Max(1, (int)Math.Ceiling(length));
        var step = length / slices;
        for (var i = 0; i < slices; i++)
        {
            var (sx, sy, sw, sl) = alongX ? (x + (i * step), y, step, l) : (x, y + (i * step), w, step);
            var joined = Sides.None;
            if (i > 0)
            {
                joined |= alongX ? Sides.West : Sides.North;
            }

            if (i < slices - 1)
            {
                joined |= alongX ? Sides.East : Sides.South;
            }

            AddProp(IsoDraw.ToScreen(sx + (sw / 2), sy + (sl / 2)), c => FurnitureArt.Slice(c, itemId, sx, sy, sw, sl, (x, y, w, l), height, colour, joined, front));
        }
    }

    private void AddProp(Vector2 anchor, Action<CanvasItem> draw, bool animated = false)
    {
        var node = new PropNode(draw, animated) { Position = anchor };
        _staticNodes.Add(node);
        AddChild(node);
    }

    /// <summary>A static or animated object drawn in world coordinates and sorted by its anchor.</summary>
    private sealed partial class PropNode(Action<CanvasItem> draw, bool animated) : Node2D
    {
        public override void _Ready() => SetProcess(animated);

        public override void _Process(double delta) => QueueRedraw();

        public override void _Draw()
        {
            DrawSetTransform(-Position);
            draw(this);
        }
    }

    /// <summary>A member of staff, kept at their interpolated position.</summary>
    private sealed partial class AgentNode(WorldLayer world, Agent agent) : Node2D
    {
        public void Sync()
        {
            var foot = world.FootPosition(agent);
            Visible = world.OnViewFloor(agent) && world.Simulation.ToiletTripOf(agent)?.Stage != ToiletStage.Using;

            // Seated people sit just in front of their chair.
            Position = foot + new Vector2(0, world.IsSeated(agent) ? 1 : 0);
            QueueRedraw();
        }

        public override void _Draw()
        {
            DrawSetTransform(-Position);
            var foot = world.FootPosition(agent);
            switch (agent)
            {
                case StaffMember member:
                    DrawRing(foot, ReferenceEquals(world.Selected, member) ? new Color("#FFEB3B") : (Color?)null);
                    var look = Looks.Staff(member);
                    if (world.IsSeated(member))
                    {
                        var facing = world.SeatFacing(member);
                        Figures.Seated(this, foot, look, away: ChairArt.FacesAway(facing), lean: ChairArt.Lean(facing));
                    }
                    else
                    {
                        Figures.Standing(this, foot, look, world.Time, member.IsMoving, member.Id * 7, away: WalkingAway(member, member.IsMoving));
                    }

                    break;
            }
        }

        /// <summary>True when someone walking is heading up the screen, so they are seen from behind.</summary>
        private static bool WalkingAway(Agent who, bool moving)
        {
            if (!moving)
            {
                return false;
            }

            var (fx, fy) = who.Facing;
            return (IsoDraw.ToScreen(fx, fy) - IsoDraw.ToScreen(0, 0)).Y < -0.01f;
        }

        private void DrawRing(Vector2 foot, Color? colour)
        {
            if (colour is not { } c)
            {
                return;
            }

            var ring = IsoDraw.FloorEllipse(foot, 13, 24);
            DrawPolyline(ring.Append(ring[0]).ToArray(), c, 2.5f, true);
        }
    }
}
