using System;
using System.Collections.Generic;
using System.Linq;
using BlueprintSchool.Simulation.Rooms;
using BlueprintSchool.Simulation.Simulation;
using BlueprintSchool.Simulation.World;
using Godot;

namespace BlueprintSchool.School;

/// <summary>
/// Room decorations that match what each room is for: a whiteboard in every classroom, a periodic
/// table and a skeleton in the science lab, a world map and a globe in humanities, easels in the
/// art room, posters, display boards and clocks on the walls, plants, bins and so on.
/// They are only for looks: the simulation doesn't know about them. Floor props stand in free
/// spots along the walls, away from doors, beds, seats and furniture. Wall pieces are painted on
/// the inside of the walls and only show on walls that face the viewer, as with a real cutaway.
/// </summary>
public static class Decorations
{
    private enum Wall
    {
        Tv, HandWashPoster, HeightChart, EyeChart, GelDispenser, Clock, Noticeboard, PastelPrint, Mirror, FireExtinguisher,
        Welcome, Directions, ToiletSign, AccessibleSign, FireExit, StaffOnly, NoSmoking, MenuBoard, Defibrillator,
        Whiteboard, DisplayBoard, PeriodicTable, WorldMap, HousePoints, ReadPoster, MathsPoster, LanguagePoster, MusicPoster,
        ArtPoster, SafetyPoster, BasketballHoop,
    }

    private enum Floor
    {
        Plant, WaterCooler, LeafletRack, Scales, Bookshelf, Fridge, Skeleton, PrivacyScreen, Kiosk, Bucket, GymBall, Bins,
        Globe, Easel, MusicStand,
    }

    private enum OnTop
    {
        Computer, Phone, Folders, KettleMugs, Books, Microscope,
    }

    private sealed record Recipe(Wall[] Walls, Floor[] Floors, (string Item, OnTop[] Things)[]? Tops = null);

    private static readonly Wall[] NoWalls = Array.Empty<Wall>();
    private static readonly Floor[] NoFloors = Array.Empty<Floor>();

    private static readonly (string, OnTop[])[] TeacherDesk = { ("teacher_desk", new[] { OnTop.Computer, OnTop.Books }) };

    private static Recipe? RecipeFor(Room space) => space.Kind switch
    {
        RoomKind.Waiting => new(new[] { Wall.Welcome, Wall.Directions, Wall.Noticeboard, Wall.Defibrillator, Wall.Tv, Wall.NoSmoking }, new[] { Floor.Kiosk, Floor.Plant, Floor.LeafletRack, Floor.Bins, Floor.Plant },
            new[] { ("reception_desk", new[] { OnTop.Computer, OnTop.Phone }) }),
        RoomKind.Cafeteria => new(new[] { Wall.MenuBoard, Wall.HousePoints, Wall.Clock, Wall.HandWashPoster }, new[] { Floor.Bins, Floor.Plant, Floor.Plant }),
        RoomKind.VendingPoint => new(new[] { Wall.NoSmoking }, new[] { Floor.Bins }),
        RoomKind.Toilets => new(new[] { Wall.ToiletSign, Wall.HandWashPoster }, NoFloors),
        RoomKind.AccessibleToilet => new(new[] { Wall.AccessibleSign }, NoFloors),
        RoomKind.StaffToilet => new(new[] { Wall.StaffOnly }, NoFloors),
        RoomKind.StaffRoom => new(new[] { Wall.Noticeboard, Wall.Clock, Wall.StaffOnly }, new[] { Floor.Fridge, Floor.Plant, Floor.Bins },
            new[] { ("kitchenette", new[] { OnTop.KettleMugs }) }),
        RoomKind.Classroom => space.TemplateId switch
        {
            "english_classroom" => new(new[] { Wall.Whiteboard, Wall.ReadPoster, Wall.DisplayBoard, Wall.Clock }, new[] { Floor.Bookshelf, Floor.Plant }, TeacherDesk),
            "maths_classroom" => new(new[] { Wall.Whiteboard, Wall.MathsPoster, Wall.DisplayBoard, Wall.Clock }, new[] { Floor.Plant }, TeacherDesk),
            "humanities_classroom" => new(new[] { Wall.Whiteboard, Wall.WorldMap, Wall.DisplayBoard, Wall.Clock }, new[] { Floor.Globe, Floor.Bookshelf }, TeacherDesk),
            "languages_classroom" => new(new[] { Wall.Whiteboard, Wall.LanguagePoster, Wall.DisplayBoard, Wall.Clock }, new[] { Floor.Plant }, TeacherDesk),
            _ => new(new[] { Wall.Whiteboard, Wall.DisplayBoard, Wall.Clock, Wall.PastelPrint }, new[] { Floor.Plant, Floor.Bins }, TeacherDesk),
        },
        RoomKind.SmallGroupRoom => new(new[] { Wall.Whiteboard, Wall.DisplayBoard }, new[] { Floor.Plant }, TeacherDesk),
        RoomKind.ScienceLab => new(new[] { Wall.Whiteboard, Wall.PeriodicTable, Wall.SafetyPoster, Wall.Clock }, new[] { Floor.Skeleton, Floor.Bins },
            new[] { ("teacher_desk", new[] { OnTop.Computer, OnTop.Books }), ("lab_bench", new[] { OnTop.Microscope }) }),
        RoomKind.PrepRoom => new(new[] { Wall.SafetyPoster, Wall.StaffOnly }, new[] { Floor.Fridge }, new[] { ("lab_bench", new[] { OnTop.Microscope, OnTop.Folders }) }),
        RoomKind.IctSuite => new(new[] { Wall.Whiteboard, Wall.DisplayBoard, Wall.Clock }, NoFloors,
            new[] { ("teacher_desk", new[] { OnTop.Computer, OnTop.Books }), ("computer_desk", new[] { OnTop.Computer, OnTop.Computer }) }),
        RoomKind.ArtRoom => new(new[] { Wall.ArtPoster, Wall.DisplayBoard, Wall.PastelPrint, Wall.Clock }, new[] { Floor.Easel, Floor.Easel, Floor.Bucket }, TeacherDesk),
        RoomKind.MusicRoom => new(new[] { Wall.Whiteboard, Wall.MusicPoster, Wall.DisplayBoard }, new[] { Floor.MusicStand, Floor.MusicStand }, TeacherDesk),
        RoomKind.DramaStudio => new(new[] { Wall.Mirror, Wall.DisplayBoard, Wall.Mirror }, NoFloors),
        RoomKind.DtWorkshop => new(new[] { Wall.Whiteboard, Wall.SafetyPoster, Wall.DisplayBoard }, new[] { Floor.Bins }, TeacherDesk),
        RoomKind.FoodRoom => new(new[] { Wall.Whiteboard, Wall.HandWashPoster, Wall.DisplayBoard }, new[] { Floor.Fridge, Floor.Bins }, TeacherDesk),
        RoomKind.SportsHall => new(new[] { Wall.BasketballHoop, Wall.HousePoints, Wall.Clock, Wall.BasketballHoop }, new[] { Floor.GymBall, Floor.GymBall }),
        RoomKind.ChangingRoom => new(new[] { Wall.Mirror }, NoFloors),
        RoomKind.Library => new(new[] { Wall.ReadPoster, Wall.DisplayBoard, Wall.Clock }, new[] { Floor.Plant, Floor.Globe, Floor.Plant },
            new[] { ("office_desk", new[] { OnTop.Computer, OnTop.Books }) }),
        RoomKind.SendBase => new(new[] { Wall.DisplayBoard, Wall.PastelPrint, Wall.Clock }, new[] { Floor.Plant, Floor.Bookshelf },
            new[] { ("office_desk", new[] { OnTop.Computer, OnTop.Folders }) }),
        RoomKind.Office => new(new[] { Wall.Noticeboard, Wall.Clock }, new[] { Floor.Bookshelf, Floor.Plant },
            new[] { ("office_desk", new[] { OnTop.Computer, OnTop.Phone, OnTop.Folders }) }),
        RoomKind.ReflectionRoom => new(new[] { Wall.Clock, Wall.Noticeboard }, NoFloors, TeacherDesk),
        RoomKind.MedicalRoom => new(new[] { Wall.EyeChart, Wall.HeightChart, Wall.HandWashPoster }, new[] { Floor.Scales, Floor.PrivacyScreen },
            new[] { ("office_desk", new[] { OnTop.Computer, OnTop.Phone }) }),
        _ => null,
    };

    // ------------------------------------------------------------------ placement

    /// <summary>Every decoration in the school: where it sorts, and how to draw it.</summary>
    public static List<(Vector2 Anchor, Action<CanvasItem> Draw)> Plan(SchoolSimulation sim)
    {
        var props = new List<(Vector2, Action<CanvasItem>)>();
        foreach (var space in sim.Spaces.Where(s => s.HasWalls && IsoDraw.OnViewFloor(s.Rect.Y)))
        {
            if (RecipeFor(space) is not { } recipe)
            {
                continue;
            }

            var blocked = Blocked(sim, space);
            PlaceWalls(sim, space, recipe.Walls, props);
            PlaceFloors(space, recipe.Floors, blocked, props);
            if (recipe.Tops is { } tops)
            {
                PlaceTops(sim, space, tops, props);
            }
        }

        // The extinguishers and plants follow the ground floor's outer walls, so only show them there.
        if (IsoDraw.ViewFloor == 0)
        {
            PlaceCorridorBits(sim, props);
        }
        return props;
    }

    /// <summary>Tiles in a room that a floor decoration must not stand on.</summary>
    private static HashSet<GridCoord> Blocked(SchoolSimulation sim, Room space)
    {
        var blocked = new HashSet<GridCoord>();
        void Area(double x, double y, double w, double l)
        {
            for (var tx = (int)Math.Floor(x); tx < (int)Math.Ceiling(x + w); tx++)
            {
                for (var ty = (int)Math.Floor(y); ty < (int)Math.Ceiling(y + l); ty++)
                {
                    blocked.Add(new GridCoord(tx, ty));
                }
            }
        }

        foreach (var item in space.Furniture)
        {
            Area(item.X - 0.3, item.Y - 0.3, item.Width + 0.6, item.Length + 0.6);
        }

        Area(space.UseSpot.X - 1.4, space.UseSpot.Y - 2.2, 3.8, 5.4);
        foreach (var tile in space.Seats.Concat(space.QueueTiles).Append(space.StaffSpot))
        {
            Area(tile.X - 1, tile.Y - 1, 3, 3);
        }

        // Keep the way in clear: the doorway and two tiles either side, three deep.
        var r = space.Rect;
        foreach (var door in space.Doors)
        {
            var (a, b) = (door.Offset - 2, door.Offset + door.Width + 2);
            switch (door.Side)
            {
                case WallSide.North: Area(r.X + a, r.Y, b - a, 3); break;
                case WallSide.South: Area(r.X + a, r.Bottom - 3, b - a, 3); break;
                case WallSide.West: Area(r.X, r.Y + a, 3, b - a); break;
                default: Area(r.Right - 3, r.Y + a, 3, b - a); break;
            }
        }

        // People walk to the other rooms' corridor chairs too.
        foreach (var chair in sim.Chairs)
        {
            blocked.Add(chair.Tile);
        }

        return blocked;
    }

    private static readonly WallSide[] Sides4 = { WallSide.North, WallSide.East, WallSide.South, WallSide.West };

    private static void PlaceFloors(Room space, Floor[] floors, HashSet<GridCoord> blocked, List<(Vector2, Action<CanvasItem>)> props)
    {
        if (floors.Length == 0)
        {
            return;
        }

        // Candidate spots: free tiles touching a wall, corners first, then spread along the walls.
        var r = space.Rect;
        var candidates = new List<(GridCoord Tile, double Score)>();
        for (var x = r.X; x < r.Right; x++)
        {
            for (var y = r.Y; y < r.Bottom; y++)
            {
                var edge = x == r.X || x == r.Right - 1 || y == r.Y || y == r.Bottom - 1;
                var tile = new GridCoord(x, y);
                if (!edge || blocked.Contains(tile))
                {
                    continue;
                }

                // Corners first, and the back walls of the usual view before the front ones, where a
                // low front wall would hide them.
                var corner = (x == r.X || x == r.Right - 1) && (y == r.Y || y == r.Bottom - 1);
                var back = x == r.X || y == r.Y;
                candidates.Add((tile, (corner ? 0 : 10) + (back ? 0 : 25) + ((x * 7 + y * 13) % 5)));
            }
        }

        var used = new List<GridCoord>();
        foreach (var floor in floors)
        {
            var pick = candidates.Where(c => used.All(u => Math.Abs(u.X - c.Tile.X) + Math.Abs(u.Y - c.Tile.Y) >= 3))
                .OrderBy(c => c.Score).Select(c => (GridCoord?)c.Tile).FirstOrDefault();
            if (pick is not { } tile)
            {
                break;
            }

            used.Add(tile);

            // Stand a little in from the wall the tile touches.
            var (gx, gy) = (tile.X + 0.5, tile.Y + 0.5);
            gx += tile.X == r.X ? 0.15 : tile.X == r.Right - 1 ? -0.15 : 0;
            gy += tile.Y == r.Y ? 0.15 : tile.Y == r.Bottom - 1 ? -0.15 : 0;
            var kind = floor;
            var scale = kind is Floor.Bins or Floor.Plant or Floor.Skeleton or Floor.PrivacyScreen or Floor.GymBall or Floor.Bucket or Floor.Easel or Floor.MusicStand ? FloorScale : 1.15f;
            props.Add((IsoDraw.ToScreen(gx, gy), c => Scaled(c, IsoDraw.ToScreen(gx, gy), scale, () => DrawFloor(c, kind, gx, gy))));
        }
    }

    /// <summary>Floor decorations are drawn a little larger than life so they read at normal zoom.</summary>
    private const float FloorScale = 1.45f;

    /// <summary>Draws with everything scaled about <paramref name="about"/>, then puts the prop's own transform back.</summary>
    private static void Scaled(CanvasItem c, Vector2 about, float k, Action draw)
    {
        if (c is not Node2D node)
        {
            draw();
            return;
        }

        c.DrawSetTransformMatrix(new Transform2D(0, new Vector2(k, k), 0, (about * (1 - k)) - node.Position));
        draw();
        c.DrawSetTransform(-node.Position);
    }

    /// <summary>Spreads the wall pieces over all four walls, so each view shows some of them.</summary>
    private static void PlaceWalls(SchoolSimulation sim, Room space, Wall[] walls, List<(Vector2, Action<CanvasItem>)> props)
    {
        if (walls.Length == 0)
        {
            return;
        }

        var r = space.Rect;
        var next = 0;
        foreach (var side in Sides4)
        {
            var length = side is WallSide.North or WallSide.South ? r.Width : r.Length;

            // Free stretches of this wall: not a doorway, not behind tall furniture.
            var free = new bool[length];
            for (var t = 0; t < length; t++)
            {
                free[t] = t > 0 && t < length - 1;
            }

            foreach (var door in space.Doors.Where(d => d.Side == side))
            {
                for (var t = door.Offset - 1; t < door.Offset + door.Width + 1; t++)
                {
                    if (t >= 0 && t < length)
                    {
                        free[t] = false;
                    }
                }
            }

            foreach (var item in space.Furniture)
            {
                var tall = sim.Database.Furniture.TryGetValue(item.ItemId, out var def) && def.HeightM * IsoDraw.PixelsPerMetre > WorldLayer.WallHeight;
                if (!tall)
                {
                    continue;
                }

                var (near, from, to) = side switch
                {
                    WallSide.North => (item.Y - r.Y < 1.2, item.X - r.X, item.X + item.Width - r.X),
                    WallSide.South => (r.Bottom - (item.Y + item.Length) < 1.2, item.X - r.X, item.X + item.Width - r.X),
                    WallSide.West => (item.X - r.X < 1.2, item.Y - r.Y, item.Y + item.Length - r.Y),
                    _ => (r.Right - (item.X + item.Width) < 1.2, item.Y - r.Y, item.Y + item.Length - r.Y),
                };
                if (near)
                {
                    for (var t = (int)Math.Floor(from); t < (int)Math.Ceiling(to); t++)
                    {
                        if (t >= 0 && t < length)
                        {
                            free[t] = false;
                        }
                    }
                }
            }

            // Not over waiting chairs standing against this wall: the picture would sort in front of their backs.
            foreach (var chair in sim.Chairs.Where(c => r.Contains(c.Tile)))
            {
                var (against, at) = side switch
                {
                    WallSide.North => (chair.Tile.Y == r.Y, chair.Tile.X - r.X),
                    WallSide.South => (chair.Tile.Y == r.Bottom - 1, chair.Tile.X - r.X),
                    WallSide.West => (chair.Tile.X == r.X, chair.Tile.Y - r.Y),
                    _ => (chair.Tile.X == r.Right - 1, chair.Tile.Y - r.Y),
                };
                for (var t = at - 1; against && t <= at + 1; t++)
                {
                    if (t >= 0 && t < length)
                    {
                        free[t] = false;
                    }
                }
            }

            // Up to two pieces per wall, each about 1.5 tiles wide, in the longest free stretches.
            var runs = new List<(int Start, int Length)>();
            for (var t = 0; t < length;)
            {
                if (!free[t])
                {
                    t++;
                    continue;
                }

                var s = t;
                while (t < length && free[t])
                {
                    t++;
                }

                runs.Add((s, t - s));
            }

            foreach (var run in runs.Where(run => run.Length >= 3).OrderByDescending(run => run.Length).Take(length >= 8 ? 2 : 1))
            {
                var kind = walls[next++ % walls.Length];
                var width = Math.Min(WallWidth(kind), run.Length - 0.4);
                var centre = run.Start + (run.Length / 2.0);
                var (t0, t1) = (centre - (width / 2), centre + (width / 2));
                var wallSide = side;
                props.Add((AfterWall(r, wallSide, t0, t1), c =>
                {
                    if (Facing(r, wallSide))
                    {
                        DrawWall(c, kind, (u, v) => WallPoint(r, wallSide, t0, t1, u, v));
                    }
                }));
            }
        }
    }

    private static double WallWidth(Wall kind) => kind switch
    {
        Wall.Clock or Wall.GelDispenser or Wall.FireExtinguisher => 1.0,
        Wall.Whiteboard => 4.4,
        Wall.Tv or Wall.Noticeboard or Wall.Mirror or Wall.DisplayBoard or Wall.WorldMap => 3.2,
        Wall.Defibrillator => 0.9,
        Wall.ToiletSign or Wall.AccessibleSign or Wall.BasketballHoop => 1.6,
        Wall.Welcome => 3.4,
        Wall.Directions or Wall.HandWashPoster or Wall.PeriodicTable or Wall.SafetyPoster => 2.4,
        Wall.FireExit or Wall.StaffOnly or Wall.NoSmoking or Wall.MenuBoard or Wall.HousePoints => 2.0,
        _ => 1.7,
    };

    private static void PlaceTops(SchoolSimulation sim, Room space, (string Item, OnTop[] Things)[] tops, List<(Vector2, Action<CanvasItem>)> props)
    {
        foreach (var (itemId, things) in tops)
        {
            foreach (var item in space.Furniture.Where(f => f.ItemId == itemId).Take(space.IsTeachingRoom ? 40 : 2))
            {
                if (!sim.Database.Furniture.TryGetValue(item.ItemId, out var def))
                {
                    continue;
                }

                var height = FurnitureArt.TopHeight(item.ItemId, (float)(def.HeightM * IsoDraw.PixelsPerMetre));
                var alongX = item.Width >= item.Length;
                var length = alongX ? item.Width : item.Length;
                var anchor = new[]
                {
                    IsoDraw.ToScreen(item.X, item.Y), IsoDraw.ToScreen(item.X + item.Width, item.Y),
                    IsoDraw.ToScreen(item.X, item.Y + item.Length), IsoDraw.ToScreen(item.X + item.Width, item.Y + item.Length),
                }.MaxBy(p => p.Y);
                var spots = things.Select((thing, i) =>
                {
                    var f = (i + 0.5) / things.Length;
                    var along = length * f;
                    return (thing, alongX ? item.X + along : item.CentreX, alongX ? item.CentreY : item.Y + along);
                }).ToArray();
                props.Add((anchor + new Vector2(0, 0.8f), c =>
                {
                    foreach (var (thing, gx, gy) in spots)
                    {
                        DrawTop(c, thing, IsoDraw.ToScreen(gx, gy) + new Vector2(0, -height));
                    }
                }));
            }
        }
    }

    /// <summary>Fire extinguishers on the building's walls and plants in its open spaces, outside the rooms.</summary>
    private static void PlaceCorridorBits(SchoolSimulation sim, List<(Vector2, Action<CanvasItem>)> props)
    {
        var rooms = sim.Spaces.Where(s => s.HasWalls && IsoDraw.OnViewFloor(s.Rect.Y)).Select(s => s.Rect).ToList();
        var chairs = sim.Chairs.Select(c => c.Tile).ToHashSet();
        foreach (var building in sim.Site.Buildings)
        {
            var count = 0;
            foreach (var (inside, _, side) in building.OuterEdges())
            {
                if (building.IsOpening(inside, side) || rooms.Any(r => r.X - 2 <= inside.X && inside.X < r.Right + 2 && r.Y - 2 <= inside.Y && inside.Y < r.Bottom + 2)
                    || chairs.Contains(inside) || ((inside.X * 3) + (inside.Y * 5)) % 2 != 0)
                {
                    continue;
                }

                count++;
                if (count % 9 == 3)
                {
                    var rect = new GridRect(inside.X, inside.Y, 1, 1);
                    var wallSide = side;
                    props.Add((AfterWall(rect, wallSide, 0.15, 0.85), c =>
                    {
                        if (Facing(rect, wallSide))
                        {
                            DrawWall(c, Wall.FireExtinguisher, (u, v) => WallPoint(rect, wallSide, 0.15, 0.85, u, v));
                        }
                    }));
                }
                else if (count % 9 == 5)
                {
                    var rect = new GridRect(inside.X, inside.Y, 1, 1);
                    var wallSide = side;
                    props.Add((AfterWall(rect, wallSide, -0.4, 1.4), c =>
                    {
                        if (Facing(rect, wallSide))
                        {
                            DrawWall(c, Wall.FireExit, (u, v) => WallPoint(rect, wallSide, -0.4, 1.4, u, v));
                        }
                    }));
                }
                else if (count % 9 == 7)
                {
                    var (gx, gy) = (inside.X + 0.5, inside.Y + 0.5);
                    props.Add((IsoDraw.ToScreen(gx, gy), c => Scaled(c, IsoDraw.ToScreen(gx, gy), FloorScale, () => DrawFloor(c, Floor.Plant, gx, gy))));
                }
            }
        }
    }

    // ------------------------------------------------------------------ walls: geometry

    private const double Half = WorldLayer.WallThickness / 2;

    /// <summary>A point on the inside face of a room's wall: t tiles along it, v pixels up.</summary>
    private static Vector2 InnerFace(GridRect r, WallSide side, double t, float v)
    {
        var (x, y) = side switch
        {
            WallSide.North => (r.X + t, r.Y + Half),
            WallSide.South => (r.X + t, r.Bottom - Half),
            WallSide.West => (r.X + Half, r.Y + t),
            _ => (r.Right - Half, r.Y + t),
        };
        return IsoDraw.ToScreen(x, y) + new Vector2(0, -v);
    }

    /// <summary>
    /// Where a picture lying on a wall between t0 and t1 tiles sorts: just after every wall piece and
    /// corner post it covers, worked out the same way the walls themselves sort. Sorting by the
    /// picture's own ends is not enough: the wall piece under one end can sort later than that end
    /// and paint over it, cutting the sign off with a straight edge.
    /// </summary>
    private static Vector2 AfterWall(GridRect r, WallSide side, double t0, double t1)
    {
        // A footprint along the wall line, from a to a + length tiles along it.
        (double X, double Y, double W, double L) Along(double a, double length) => side switch
        {
            WallSide.North => (r.X + a, r.Y - Half, length, Half * 2),
            WallSide.South => (r.X + a, r.Bottom - Half, length, Half * 2),
            WallSide.West => (r.X - Half, r.Y + a, Half * 2, length),
            _ => (r.Right - Half, r.Y + a, Half * 2, length),
        };

        var best = new[] { InnerFace(r, side, t0, 0), InnerFace(r, side, t1, 0) }.MaxBy(p => p.Y);
        void Consider((double X, double Y, double W, double L) f)
        {
            var key = WorldLayer.FrontFace(f.X, f.Y, f.W, f.L);
            if (key.Y > best.Y)
            {
                best = key;
            }
        }

        for (var k = (int)Math.Floor(t0) - 1; k <= (int)Math.Ceiling(t1) + 1; k++)
        {
            // The post at each tile corner, then the piece of wall between it and the next.
            if (k + Half > t0 && k - Half < t1)
            {
                Consider(Along(k - Half, Half * 2));
            }

            if (k + 1 - Half > t0 && k + Half < t1)
            {
                Consider(Along(k + Half, 1 - (Half * 2)));
            }
        }

        return best + new Vector2(0, 0.6f);
    }

    /// <summary>True when the inside of this wall faces the viewer in the current view angle.</summary>
    private static bool Facing(GridRect r, WallSide side)
    {
        var (nx, ny) = side switch
        {
            WallSide.North => (0.0, 1.0),
            WallSide.South => (0.0, -1.0),
            WallSide.West => (1.0, 0.0),
            _ => (-1.0, 0.0),
        };
        return (IsoDraw.ToScreen(nx, ny) - IsoDraw.ToScreen(0, 0)).Y > 0;
    }

    /// <summary>
    /// A point on a wall piece: u runs 0 to 1 left to right as seen on screen (so pictures never
    /// appear mirrored), v runs 0 to 1 up the face from the floor to the top of the wall.
    /// </summary>
    private static Vector2 WallPoint(GridRect r, WallSide side, double t0, double t1, float u, float v)
    {
        var (a, b) = (InnerFace(r, side, t0, 0), InnerFace(r, side, t1, 0));
        var (left, right) = a.X <= b.X ? (a, b) : (b, a);
        return left.Lerp(right, u) + new Vector2(0, -v * WorldLayer.WallHeight);
    }

    // ------------------------------------------------------------------ walls: art

    private delegate Vector2 Frame(float u, float v);

    private static void Quad(CanvasItem c, Frame f, float u0, float v0, float u1, float v1, Color colour) =>
        c.DrawColoredPolygon(new[] { f(u0, v0), f(u1, v0), f(u1, v1), f(u0, v1) }, colour);

    private static void Line(CanvasItem c, Frame f, float u0, float v0, float u1, float v1, Color colour, float width = 1f) =>
        c.DrawLine(f(u0, v0), f(u1, v1), colour, width, true);

    /// <summary>A poster: white card with a coloured header, framed, with a picture drawn by <paramref name="picture"/>.</summary>
    private static void Poster(CanvasItem c, Frame f, Color header, Action<Frame> picture, float v0 = 0.2f, float v1 = 0.94f)
    {
        Quad(c, f, 0.04f, v0 - 0.03f, 0.96f, v1 + 0.03f, new Color("#8C99A5"));
        Quad(c, f, 0.08f, v0, 0.92f, v1, new Color("#FBFCFD"));
        Quad(c, f, 0.08f, v1 - ((v1 - v0) * 0.18f), 0.92f, v1, header);
        picture((u, v) => f(0.1f + (u * 0.8f), v0 + 0.04f + (v * (v1 - v0) * 0.72f)));
    }

    /// <summary>Lays a flat sign on the wall between two heights (0 to 1 up the wall): <paramref name="draw"/> gets its width and height in pixels.</summary>
    private static void Sign(CanvasItem c, Frame f, float v0, float v1, Action<float, float> draw)
    {
        var (a, b) = (f(0, v1), f(1, v1));
        Flat.On(c, a, b - a, () => draw((b - a).Length(), (v1 - v0) * WorldLayer.WallHeight));
    }

    private static void PicturePoster(CanvasItem c, Frame f, string colour, string title, Pictogram icon, string ink) =>
        Sign(c, (u, v) => f(0.08f + (u * 0.84f), v), 0.3f, 0.95f, (w, h) => Signs.Poster(c, w, h, new Color(colour), title,
            r => Signs.Draw(c, icon, r.GetCenter().X, r.GetCenter().Y, Math.Min(r.Size.X, r.Size.Y) * 0.92f, new Color(ink), Colors.White)));

    private static void DrawWall(CanvasItem c, Wall kind, Frame f)
    {
        var red = new Color("#E53935");
        var blue = new Color("#1F5FBF");
        var dark = new Color("#2B3640");
        switch (kind)
        {
            case Wall.Tv:
                Quad(c, f, 0.05f, 0.42f, 0.95f, 0.98f, new Color("#1B2228"));
                Quad(c, f, 0.09f, 0.48f, 0.91f, 0.93f, new Color("#123A63"));
                Quad(c, f, 0.09f, 0.8f, 0.91f, 0.93f, blue);
                Line(c, f, 0.15f, 0.7f, 0.6f, 0.7f, Colors.White, 1.4f);
                Line(c, f, 0.15f, 0.6f, 0.45f, 0.6f, new Color("#9EC3E8"), 1.2f);
                Line(c, f, 0.68f, 0.56f, 0.86f, 0.56f, new Color("#FFD54F"), 2.2f);
                Line(c, f, 0.68f, 0.67f, 0.86f, 0.67f, new Color("#FFD54F"), 2.2f);
                break;

            case Wall.HandWashPoster:
                Sign(c, f, 0.52f, 0.84f, (w, h) => Signs.Notice(c, w, h, new Color("#00897B"), Pictogram.Drop, "Wash your hands", "Soap, water, 20 seconds"));
                break;

            case Wall.Welcome:
                Sign(c, f, 0.56f, 0.88f, (w, h) => Signs.Banner(c, w, h, new Color("#1E6B47"), Pictogram.Info, "Welcome", "Visitors, please sign in at reception"));
                break;

            case Wall.Directions:
                Sign(c, f, 0.3f, 0.92f, (w, h) => Signs.Directions(c, w, h, "This way", ("Classrooms", 3, Pictogram.Pencil), ("Toilets", 0, Pictogram.Man), ("Canteen", 2, Pictogram.Cutlery)));
                break;

            case Wall.Whiteboard:
            {
                // A wide whiteboard with an aluminium frame, a pen tray and the lesson's notes on it.
                Quad(c, f, 0.02f, 0.38f, 0.98f, 0.96f, new Color("#A9B3BB"));
                Quad(c, f, 0.04f, 0.41f, 0.96f, 0.93f, new Color("#FBFCFD"));
                Quad(c, f, 0.04f, 0.37f, 0.96f, 0.4f, new Color("#8F9AA3"));
                Line(c, f, 0.08f, 0.86f, 0.42f, 0.86f, new Color("#1F5FBF"), 1.4f);
                Line(c, f, 0.08f, 0.77f, 0.36f, 0.77f, new Color("#2B3640"), 0.9f);
                Line(c, f, 0.08f, 0.7f, 0.4f, 0.7f, new Color("#2B3640"), 0.9f);
                Line(c, f, 0.08f, 0.63f, 0.3f, 0.63f, new Color("#2B3640"), 0.9f);
                Line(c, f, 0.08f, 0.52f, 0.33f, 0.52f, new Color("#C0392B"), 0.9f);
                // A little diagram on the right.
                Line(c, f, 0.55f, 0.5f, 0.55f, 0.86f, new Color("#2B3640"), 0.9f);
                Line(c, f, 0.55f, 0.5f, 0.9f, 0.5f, new Color("#2B3640"), 0.9f);
                Line(c, f, 0.57f, 0.55f, 0.88f, 0.82f, new Color("#2E9A5B"), 1.2f);
                foreach (var (u, colour) in new[] { (0.62f, "#1F5FBF"), (0.68f, "#C0392B"), (0.74f, "#2B3640") })
                {
                    Line(c, f, u, 0.385f, u + 0.04f, 0.385f, new Color(colour), 1.6f);
                }

                break;
            }

            case Wall.DisplayBoard:
            {
                // Pupils' work pinned on bright backing paper with a scalloped border.
                Quad(c, f, 0.03f, 0.32f, 0.97f, 0.95f, new Color("#E3A92B"));
                Quad(c, f, 0.06f, 0.36f, 0.94f, 0.91f, new Color("#2C6FB7"));
                foreach (var (u, v, colour) in new[] { (0.1f, 0.6f, "#FFFFFF"), (0.28f, 0.42f, "#FFF59D"), (0.32f, 0.66f, "#FFFFFF"), (0.5f, 0.48f, "#FFFFFF"), (0.66f, 0.64f, "#F8BBD0"), (0.76f, 0.42f, "#FFFFFF") })
                {
                    Quad(c, f, u, v, u + 0.15f, v + 0.2f, new Color(colour));
                    Line(c, f, u + 0.02f, v + 0.14f, u + 0.12f, v + 0.14f, new Color("#90A4AE"), 0.6f);
                    Line(c, f, u + 0.02f, v + 0.09f, u + 0.1f, v + 0.09f, new Color("#90A4AE"), 0.6f);
                }

                break;
            }

            case Wall.PeriodicTable:
                Poster(c, f, new Color("#2E9A9A"), p =>
                {
                    var colours = new[] { "#F48FB1", "#FFCC80", "#FFF59D", "#A5D6A7", "#80DEEA", "#B39DDB" };
                    for (var row = 0; row < 5; row++)
                    {
                        for (var col = 0; col < 9; col++)
                        {
                            // The table's shape: tall columns at each end, a dip in the middle of the top rows.
                            if (row < 2 && col is > 1 and < 7)
                            {
                                continue;
                            }

                            var (u, v) = (col / 9f, 1 - ((row + 1) / 5.5f));
                            c.DrawColoredPolygon(new[] { p(u, v), p(u + 0.1f, v), p(u + 0.1f, v + 0.16f), p(u, v + 0.16f) }, new Color(colours[(col + row) % colours.Length]));
                        }
                    }
                });
                break;

            case Wall.WorldMap:
            {
                Quad(c, f, 0.03f, 0.36f, 0.97f, 0.94f, new Color("#8C99A5"));
                Quad(c, f, 0.05f, 0.39f, 0.95f, 0.91f, new Color("#7FB8E0"));
                var land = new Color("#7CB860");
                foreach (var (u0, v0, u1, v1) in new[] { (0.1f, 0.62f, 0.28f, 0.86f), (0.2f, 0.42f, 0.3f, 0.6f), (0.42f, 0.66f, 0.55f, 0.86f), (0.44f, 0.44f, 0.56f, 0.64f), (0.58f, 0.6f, 0.85f, 0.86f), (0.78f, 0.42f, 0.88f, 0.52f) })
                {
                    Quad(c, f, u0, v0, u1, v1, land);
                }

                Line(c, f, 0.05f, 0.65f, 0.95f, 0.65f, new Color("#E3F2FD", 0.6f), 0.6f);
                break;
            }

            case Wall.HousePoints:
                Poster(c, f, new Color("#1E6B47"), p =>
                {
                    var houses = new[] { ("#C0392B", 0.7f), ("#2C6FB7", 0.9f), ("#E3A92B", 0.55f), ("#2E9A5B", 0.8f) };
                    for (var i = 0; i < houses.Length; i++)
                    {
                        var u = 0.08f + (i * 0.23f);
                        c.DrawColoredPolygon(new[] { p(u, 0.05f), p(u + 0.16f, 0.05f), p(u + 0.16f, 0.05f + houses[i].Item2), p(u, 0.05f + houses[i].Item2) }, new Color(houses[i].Item1));
                    }
                });
                break;

            case Wall.ReadPoster:
                PicturePoster(c, f, "#C98A1E", "READ!", Pictogram.Book, "#C98A1E");
                break;

            case Wall.MathsPoster:
                PicturePoster(c, f, "#2C6FB7", "TIMES TABLES", Pictogram.Pencil, "#2C6FB7");
                break;

            case Wall.LanguagePoster:
                PicturePoster(c, f, "#A23B86", "BONJOUR", Pictogram.Speech, "#A23B86");
                break;

            case Wall.MusicPoster:
                PicturePoster(c, f, "#6A4C93", "MUSIC", Pictogram.Note, "#6A4C93");
                break;

            case Wall.ArtPoster:
                PicturePoster(c, f, "#C0392B", "COLOUR WHEEL", Pictogram.Brush, "#C0392B");
                break;

            case Wall.SafetyPoster:
                Sign(c, f, 0.52f, 0.84f, (w, h) => Signs.Notice(c, w, h, new Color("#E08A1E"), Pictogram.Flask, "Goggles on", "For every practical"));
                break;

            case Wall.BasketballHoop:
            {
                Quad(c, f, 0.12f, 0.62f, 0.88f, 0.98f, Colors.White);
                Quad(c, f, 0.14f, 0.64f, 0.86f, 0.96f, new Color(0, 0, 0, 0));
                Line(c, f, 0.12f, 0.62f, 0.88f, 0.62f, new Color("#C0392B"), 1.2f);
                Line(c, f, 0.12f, 0.98f, 0.88f, 0.98f, new Color("#C0392B"), 1.2f);
                Line(c, f, 0.12f, 0.62f, 0.12f, 0.98f, new Color("#C0392B"), 1.2f);
                Line(c, f, 0.88f, 0.62f, 0.88f, 0.98f, new Color("#C0392B"), 1.2f);
                Quad(c, f, 0.38f, 0.66f, 0.62f, 0.8f, new Color(0, 0, 0, 0));
                Line(c, f, 0.38f, 0.66f, 0.62f, 0.66f, new Color("#C0392B"), 1f);
                var rim = f(0.5f, 0.6f) + new Vector2(0, 5);
                c.DrawArc(rim, 5f, 0, Mathf.Tau, 14, new Color("#E8590C"), 1.6f, true);
                c.DrawLine(rim + new Vector2(-4, 1), rim + new Vector2(-2, 8), new Color("#ECEFF1"), 0.7f);
                c.DrawLine(rim + new Vector2(4, 1), rim + new Vector2(2, 8), new Color("#ECEFF1"), 0.7f);
                c.DrawLine(rim + new Vector2(0, 2), rim + new Vector2(0, 8), new Color("#ECEFF1"), 0.7f);
                break;
            }

            case Wall.ToiletSign:
                Sign(c, f, 0.45f, 0.9f, (w, h) => Signs.Toilets(c, w, h, Pictogram.Man, Pictogram.Woman));
                break;

            case Wall.AccessibleSign:
                Sign(c, f, 0.45f, 0.9f, (w, h) => Signs.Toilets(c, w, h, Pictogram.Wheelchair, Pictogram.Baby));
                break;

            case Wall.FireExit:
                Sign(c, f, 0.7f, 0.92f, (w, h) => Signs.FireExit(c, w, h, 0));
                break;

            case Wall.StaffOnly:
                Sign(c, f, 0.6f, 0.83f, (w, h) => Signs.Prohibition(c, w, h, "STAFF ONLY", false));
                break;

            case Wall.NoSmoking:
                Sign(c, f, 0.6f, 0.83f, (w, h) => Signs.Prohibition(c, w, h, "No smoking", true));
                break;

            case Wall.MenuBoard:
                Sign(c, f, 0.28f, 0.94f, (w, h) => Signs.Menu(c, w, h));
                break;

            case Wall.Defibrillator:
                Sign(c, f, 0.38f, 0.8f, (w, h) => Signs.Defibrillator(c, w, h));
                break;

            case Wall.HeightChart:
                Quad(c, f, 0.35f, 0.05f, 0.65f, 0.98f, Colors.White);
                for (var i = 1; i < 10; i++)
                {
                    var v = 0.05f + (i * 0.093f);
                    Line(c, f, 0.35f, v, i % 2 == 0 ? 0.6f : 0.48f, v, new Color("#5B6B78"), 0.8f);
                }

                Quad(c, f, 0.35f, 0.05f, 0.4f, 0.98f, new Color("#7E57C2"));
                break;

            case Wall.EyeChart:
                Quad(c, f, 0.2f, 0.25f, 0.8f, 0.97f, Colors.White);
                Quad(c, f, 0.2f, 0.25f, 0.8f, 0.97f, new Color(0, 0, 0, 0));
                for (var i = 0; i < 6; i++)
                {
                    var v = 0.88f - (i * 0.11f);
                    var w = 0.18f - (i * 0.025f);
                    for (var k = -i / 2; k <= i / 2; k++)
                    {
                        var u = 0.5f + (k * (w + 0.04f));
                        Quad(c, f, u - (w / 2), v - 0.035f - (0.01f * (5 - i)), u + (w / 2), v, dark);
                    }
                }

                break;

            case Wall.GelDispenser:
                Quad(c, f, 0.25f, 0.45f, 0.75f, 0.85f, Colors.White);
                Quad(c, f, 0.25f, 0.7f, 0.75f, 0.85f, new Color("#2BB3A3"));
                Quad(c, f, 0.4f, 0.4f, 0.6f, 0.46f, new Color("#9AA6B0"));
                break;

            case Wall.Clock:
            {
                var centre = f(0.5f, 0.7f);
                c.DrawCircle(centre, 6.5f, dark);
                c.DrawCircle(centre, 5.3f, Colors.White);
                c.DrawLine(centre, centre + new Vector2(0, -4), dark, 1f);
                c.DrawLine(centre, centre + new Vector2(3, 0.5f), dark, 1f);
                c.DrawCircle(centre, 0.8f, red);
                break;
            }

            case Wall.Noticeboard:
                Quad(c, f, 0.03f, 0.35f, 0.97f, 0.95f, new Color("#7A5A3A"));
                Quad(c, f, 0.06f, 0.39f, 0.94f, 0.91f, new Color("#C69C6D"));
                foreach (var (u, v, colour) in new[] { (0.12f, 0.55f, "#FFF59D"), (0.3f, 0.7f, "#FFFFFF"), (0.5f, 0.5f, "#90CAF9"), (0.68f, 0.68f, "#F48FB1"), (0.8f, 0.48f, "#FFFFFF") })
                {
                    Quad(c, f, u, v, u + 0.13f, v + 0.17f, new Color(colour));
                    c.DrawCircle(f(u + 0.065f, v + 0.15f), 0.8f, red);
                }

                break;

            case Wall.PastelPrint:
                Quad(c, f, 0.08f, 0.32f, 0.92f, 0.94f, new Color("#E7DCCB"));
                Quad(c, f, 0.13f, 0.38f, 0.87f, 0.88f, new Color("#F3E5F5"));
                Quad(c, f, 0.13f, 0.38f, 0.87f, 0.55f, new Color("#C8E6C9"));
                foreach (var (u, colour) in new[] { (0.3f, "#F48FB1"), (0.5f, "#CE93D8"), (0.7f, "#FFCC80") })
                {
                    Line(c, f, u, 0.45f, u, 0.62f, new Color("#81C784"), 0.8f);
                    c.DrawCircle(f(u, 0.65f), 2f, new Color(colour));
                }

                break;

            case Wall.Mirror:
                Quad(c, f, 0.03f, 0.1f, 0.97f, 0.97f, new Color("#9AA6B0"));
                Quad(c, f, 0.06f, 0.14f, 0.94f, 0.93f, new Color("#D6E9F2"));
                Line(c, f, 0.15f, 0.2f, 0.4f, 0.85f, new Color("#FFFFFF", 0.7f), 2f);
                Line(c, f, 0.3f, 0.2f, 0.5f, 0.7f, new Color("#FFFFFF", 0.5f), 1f);
                break;

            case Wall.FireExtinguisher:
                Quad(c, f, 0.1f, 0.4f, 0.9f, 0.95f, new Color("#ECEFF1"));
                Quad(c, f, 0.32f, 0.12f, 0.68f, 0.68f, red);
                Quad(c, f, 0.32f, 0.5f, 0.68f, 0.57f, new Color("#FBFCFD"));
                Line(c, f, 0.5f, 0.68f, 0.5f, 0.78f, Colors.Black, 1.4f);
                Line(c, f, 0.5f, 0.76f, 0.75f, 0.6f, Colors.Black, 0.9f);
                break;
        }
    }

    // ------------------------------------------------------------------ floor props

    private static void Shadow(CanvasItem c, Vector2 at, float radius) =>
        c.DrawColoredPolygon(IsoDraw.FloorEllipse(at + new Vector2(1, 1), radius), new Color(0, 0, 0, 0.16f));

    /// <summary>A small box drawn about a floor point, (w, l) tiles in size.</summary>
    private static void Cube(CanvasItem c, double gx, double gy, double w, double l, float height, Color top, Color? face = null, float lift = 0, Color? rim = null) =>
        IsoDraw.Prism(c, gx - (w / 2), gy - (l / 2), w, l, height, top, face ?? top.Darkened(0.2f), lift: lift, rim: rim ?? top.Lightened(0.25f));

    private static void Pedestal(CanvasItem c, double gx, double gy) =>
        Cube(c, gx, gy, 0.6, 0.6, 22, new Color("#E7EBEE"), new Color("#C9D2DA"));

    private static void DrawFloor(CanvasItem c, Floor kind, double gx, double gy)
    {
        var p = IsoDraw.ToScreen(gx, gy);
        switch (kind)
        {
            case Floor.Plant:
            {
                Shadow(c, p, 9);
                c.DrawColoredPolygon(new[] { p + new Vector2(-6, -12), p + new Vector2(6, -12), p + new Vector2(4.5f, 0), p + new Vector2(-4.5f, 0) }, new Color("#B5653A"));
                c.DrawRect(new Rect2(p + new Vector2(-6.5f, -14), new Vector2(13, 3)), new Color("#C9774A"));
                var leaf = new Color("#3F8F4A");
                foreach (var (x, y, r) in new[] { (-6f, -22f, 6f), (6f, -23f, 6f), (0f, -30f, 7f), (-3f, -18f, 5f), (4f, -17f, 5f), (0f, -24f, 6f) })
                {
                    c.DrawCircle(p + new Vector2(x, y), r, leaf);
                    c.DrawCircle(p + new Vector2(x - 1.5f, y - 1.5f), r * 0.5f, leaf.Lightened(0.2f));
                }

                break;
            }

            case Floor.WaterCooler:
                Cube(c, gx, gy, 0.6, 0.6, 22, new Color("#E9EDF0"));
                c.DrawColoredPolygon(new[] { p + new Vector2(-5, -24), p + new Vector2(5, -24), p + new Vector2(6, -36), p + new Vector2(0, -40), p + new Vector2(-6, -36) }, new Color("#64B5F6", 0.85f));
                c.DrawLine(p + new Vector2(-3, -30), p + new Vector2(-3, -36), new Color("#E3F2FD", 0.9f), 1.5f);
                c.DrawRect(new Rect2(p + new Vector2(-2, -14), new Vector2(4, 2)), new Color("#1E88E5"));
                break;

            case Floor.LeafletRack:
                Cube(c, gx, gy, 0.7, 0.3, 30, new Color("#B9C3CB"));
                foreach (var (y, colour) in new[] { (-28f, "#43A047"), (-21f, "#1F5FBF"), (-14f, "#F9A825"), (-7f, "#E53935") })
                {
                    c.DrawRect(new Rect2(p + new Vector2(-8, y), new Vector2(7, 6)), new Color(colour));
                    c.DrawRect(new Rect2(p + new Vector2(1, y), new Vector2(7, 6)), Colors.White);
                }

                break;

            case Floor.Scales:
                Cube(c, gx, gy, 0.6, 0.6, 3, new Color("#ECEFF1"));
                c.DrawLine(p + new Vector2(0, -3), p + new Vector2(0, -30), new Color("#9AA6B0"), 2f);
                c.DrawRect(new Rect2(p + new Vector2(-5, -36), new Vector2(10, 7)), new Color("#37474F"));
                c.DrawRect(new Rect2(p + new Vector2(-3.5f, -35), new Vector2(7, 4)), new Color("#A5D6A7"));
                break;

            case Floor.Bookshelf:
                Cube(c, gx, gy, 0.9, 0.35, 44, new Color("#8C6D4F"), new Color("#6E543B"));
                for (var i = 0; i < 4; i++)
                {
                    for (var k = 0; k < 4; k++)
                    {
                        var colour = new[] { "#1F5FBF", "#E53935", "#43A047", "#F9A825", "#ECEFF1", "#7E57C2" }[(i + (k * 2)) % 6];
                        c.DrawRect(new Rect2(p + new Vector2(-10 + (k * 5), -42 + (i * 10)), new Vector2(3.5f, 8)), new Color(colour));
                    }
                }

                break;

            case Floor.Fridge:
                Cube(c, gx, gy, 0.7, 0.7, 42, new Color("#F2F4F6"), new Color("#D9DFE5"));
                c.DrawLine(p + new Vector2(-9, -26), p + new Vector2(9, -26), new Color("#AEB8C1"), 1f);
                c.DrawLine(p + new Vector2(6, -36), p + new Vector2(6, -30), new Color("#8D99A3"), 1.5f);
                c.DrawCircle(p + new Vector2(-4, -34), 1.5f, new Color("#F48FB1"));
                break;

            case Floor.Skeleton:
                Skeleton(c, p);
                break;

            case Floor.PrivacyScreen:
            {
                Shadow(c, p, 12);
                for (var i = 0; i < 3; i++)
                {
                    var x = -13 + (i * 9);
                    var lean = i == 1 ? -2 : 0;
                    c.DrawRect(new Rect2(p + new Vector2(x, -36 + lean), new Vector2(8, 32)), new Color("#F3E5F5"));
                    c.DrawRect(new Rect2(p + new Vector2(x, -36 + lean), new Vector2(8, 3)), new Color("#9C8AD0"));
                    c.DrawLine(p + new Vector2(x, -36 + lean), p + new Vector2(x, -1), new Color("#9AA6B0"), 1f);
                }

                break;
            }

            case Floor.Kiosk:
                // The visitor sign-in screen.
                Cube(c, gx, gy, 0.5, 0.4, 26, new Color("#E7EBEE"), new Color("#C9D2DA"));
                c.DrawColoredPolygon(new[] { p + new Vector2(-7, -40), p + new Vector2(7, -40), p + new Vector2(7, -27), p + new Vector2(-7, -27) }, new Color("#1B2228"));
                c.DrawRect(new Rect2(p + new Vector2(-5.5f, -38.5f), new Vector2(11, 10)), new Color("#1E6B47"));
                c.DrawRect(new Rect2(p + new Vector2(-4, -33), new Vector2(8, 2.5f)), Colors.White);
                break;

            case Floor.Bucket:
                Shadow(c, p, 6);
                c.DrawColoredPolygon(new[] { p + new Vector2(-5, -11), p + new Vector2(5, -11), p + new Vector2(4, 0), p + new Vector2(-4, 0) }, new Color("#90A4AE"));
                c.DrawColoredPolygon(IsoDraw.FloorEllipse(p + new Vector2(0, -11), 5, 12), new Color("#D6E4EC"));
                c.DrawArc(p + new Vector2(0, -12), 5, Mathf.Pi, Mathf.Tau, 8, new Color("#607D8B"), 1f, true);
                break;

            case Floor.GymBall:
                Shadow(c, p, 9);
                c.DrawCircle(p + new Vector2(0, -9), 9, new Color("#1E88E5"));
                c.DrawCircle(p + new Vector2(-3, -12), 3, new Color("#64B5F6"));
                break;

            case Floor.Globe:
                Cube(c, gx, gy, 0.4, 0.4, 16, new Color("#8C6D4F"), new Color("#6E543B"));
                c.DrawLine(p + new Vector2(0, -16), p + new Vector2(0, -20), new Color("#B8A07A"), 1.5f);
                c.DrawCircle(p + new Vector2(0, -27), 8, new Color("#4F8FC0"));
                c.DrawCircle(p + new Vector2(-2.5f, -29), 3.2f, new Color("#7CB860"));
                c.DrawCircle(p + new Vector2(3, -24), 2.4f, new Color("#7CB860"));
                c.DrawArc(p + new Vector2(0, -27), 9.5f, -2.3f, 0.9f, 12, new Color("#B8A07A"), 1.2f, true);
                break;

            case Floor.Easel:
            {
                var wood = new Color("#A9865F");
                Shadow(c, p, 7);
                c.DrawLine(p + new Vector2(-6, 0), p + new Vector2(-1, -40), wood, 1.8f);
                c.DrawLine(p + new Vector2(6, 0), p + new Vector2(1, -40), wood, 1.8f);
                c.DrawLine(p + new Vector2(0, 0), p + new Vector2(0, -36), wood.Darkened(0.2f), 1.4f);
                c.DrawRect(new Rect2(p + new Vector2(-8, -36), new Vector2(16, 18)), new Color("#FBFCFD"));
                c.DrawCircle(p + new Vector2(-3, -29), 3.5f, new Color("#F9A825"));
                c.DrawRect(new Rect2(p + new Vector2(-6, -24), new Vector2(12, 4)), new Color("#43A047"));
                c.DrawLine(p + new Vector2(-9, -17), p + new Vector2(9, -17), wood, 2f);
                break;
            }

            case Floor.MusicStand:
            {
                var metal = new Color("#3A3F45");
                Shadow(c, p, 6);
                c.DrawLine(p + new Vector2(-5, 0), p + new Vector2(0, -4), metal, 1.2f);
                c.DrawLine(p + new Vector2(5, 0), p + new Vector2(0, -4), metal, 1.2f);
                c.DrawLine(p + new Vector2(0, -4), p + new Vector2(0, -26), metal, 1.4f);
                c.DrawColoredPolygon(new[] { p + new Vector2(-8, -36), p + new Vector2(8, -36), p + new Vector2(8, -25), p + new Vector2(-8, -25) }, metal);
                c.DrawRect(new Rect2(p + new Vector2(-6, -35), new Vector2(12, 9)), new Color("#FBFCFD"));
                for (var i = 0; i < 3; i++)
                {
                    c.DrawLine(p + new Vector2(-5, -33 + (i * 3)), p + new Vector2(5, -33 + (i * 3)), new Color("#90A4AE"), 0.5f);
                }

                break;
            }

            case Floor.Bins:
                // A pair of pedal bins: general waste (black lid) and recycling (green lid).
                foreach (var (dx, lid, badge) in new[] { (-0.2, "#2B3036", "#9AA6B0"), (0.2, "#2E7D32", "#FFFFFF") })
                {
                    var q = IsoDraw.ToScreen(gx + dx, gy - dx);
                    Shadow(c, q, 6);
                    c.DrawColoredPolygon(new[] { q + new Vector2(-4.5f, 0), q + new Vector2(4.5f, 0), q + new Vector2(5.2f, -13), q + new Vector2(-5.2f, -13) }, new Color("#E4E9ED"));
                    c.DrawColoredPolygon(new[] { q + new Vector2(1.5f, 0), q + new Vector2(4.5f, 0), q + new Vector2(5.2f, -13), q + new Vector2(2f, -13) }, new Color("#CBD4DB"));
                    c.DrawColoredPolygon(IsoDraw.FloorEllipse(q + new Vector2(0, -13.5f), 5.6f, 12), new Color(lid));
                    c.DrawRect(new Rect2(q + new Vector2(-2.2f, -9.5f), new Vector2(4.4f, 4.4f)), new Color(lid));
                    c.DrawCircle(q + new Vector2(0, -7.3f), 1.3f, new Color(badge));
                    c.DrawRect(new Rect2(q + new Vector2(-2, -1.2f), new Vector2(4, 1.2f)), new Color("#55616B"));
                }

                break;

        }
    }

    private static void Skeleton(CanvasItem c, Vector2 p)
    {
        var bone = new Color("#EFEADF");
        var joint = new Color("#D8D0BE");
        var stand = new Color("#6E7A84");
        Shadow(c, p, 9);
        c.DrawLine(p + new Vector2(-7, 0), p + new Vector2(7, 0), stand, 2f);
        c.DrawLine(p + new Vector2(0, 0), p + new Vector2(0, -60), stand, 1.4f);

        // Legs, pelvis, spine and ribs, arms, skull.
        foreach (var s in new[] { -1f, 1f })
        {
            c.DrawLine(p + new Vector2(s * 3, -26), p + new Vector2(s * 3.5f, -14), bone, 1.8f);
            c.DrawLine(p + new Vector2(s * 3.5f, -14), p + new Vector2(s * 3.5f, -3), bone, 1.6f);
            c.DrawCircle(p + new Vector2(s * 3.5f, -14), 1.3f, joint);
            c.DrawLine(p + new Vector2(s * 3.5f, -3), p + new Vector2(s * 5.5f, -2), bone, 1.5f);
            c.DrawLine(p + new Vector2(s * 7, -44), p + new Vector2(s * 8.5f, -34), bone, 1.5f);
            c.DrawLine(p + new Vector2(s * 8.5f, -34), p + new Vector2(s * 9, -25), bone, 1.4f);
            c.DrawCircle(p + new Vector2(s * 8.5f, -34), 1.2f, joint);
        }

        c.DrawColoredPolygon(new[] { p + new Vector2(-6, -30), p + new Vector2(6, -30), p + new Vector2(4, -25), p + new Vector2(0, -27), p + new Vector2(-4, -25) }, bone);
        c.DrawLine(p + new Vector2(0, -30), p + new Vector2(0, -46), bone, 1.4f);
        for (var i = 0; i < 4; i++)
        {
            var y = -43 + (i * 3);
            var w = 5.5f - (i * 0.6f);
            c.DrawArc(p + new Vector2(0, y + 2), w, Mathf.Pi * 1.05f, Mathf.Tau * 0.97f, 8, bone, 1.1f, true);
        }

        c.DrawLine(p + new Vector2(-7, -45), p + new Vector2(7, -45), bone, 1.6f);
        c.DrawCircle(p + new Vector2(0, -51), 5, bone);
        c.DrawRect(new Rect2(p + new Vector2(-3, -49), new Vector2(6, 4)), bone);
        c.DrawCircle(p + new Vector2(-1.8f, -51), 1.2f, new Color("#3A3A3A"));
        c.DrawCircle(p + new Vector2(1.8f, -51), 1.2f, new Color("#3A3A3A"));
        c.DrawLine(p + new Vector2(-2, -46.5f), p + new Vector2(2, -46.5f), new Color("#8A8170"), 0.8f);
    }

    // ------------------------------------------------------------------ on desks and counters

    private static void DrawTop(CanvasItem c, OnTop kind, Vector2 at)
    {
        switch (kind)
        {
            case OnTop.Computer:
                c.DrawRect(new Rect2(at + new Vector2(-1.5f, -6), new Vector2(3, 6)), new Color("#3A4148"));
                c.DrawRect(new Rect2(at + new Vector2(-9, -18), new Vector2(18, 12)), new Color("#2B3036"));
                c.DrawRect(new Rect2(at + new Vector2(-8, -17), new Vector2(16, 10)), new Color("#2F6FB5"));
                c.DrawRect(new Rect2(at + new Vector2(-8, -17), new Vector2(16, 2.5f)), new Color("#1F5FBF"));
                c.DrawLine(at + new Vector2(-6, -11), at + new Vector2(3, -11), new Color("#D6E9F8"), 1f);
                c.DrawRect(new Rect2(at + new Vector2(-6, 1), new Vector2(12, 3)), new Color("#E3E7EA"));
                break;

            case OnTop.Phone:
                c.DrawRect(new Rect2(at + new Vector2(-4, -3), new Vector2(8, 4)), new Color("#2B3036"));
                c.DrawLine(at + new Vector2(-4, -4), at + new Vector2(4, -4), new Color("#3A4148"), 2.2f);
                break;

            case OnTop.Folders:
                foreach (var (i, colour) in new[] { (0, "#E53935"), (1, "#1F5FBF"), (2, "#F9A825") })
                {
                    c.DrawRect(new Rect2(at + new Vector2(-6 + (i * 0.6f), -2 - (i * 2)), new Vector2(11, 2)), new Color(colour));
                }

                break;

            case OnTop.Books:
                // A pile of exercise books waiting to be marked.
                foreach (var (y, colour) in new[] { (-2f, "#2C6FB7"), (-4f, "#C0392B"), (-6f, "#2E9A5B"), (-8f, "#E3A92B") })
                {
                    c.DrawRect(new Rect2(at + new Vector2(-5, y), new Vector2(9, 2)), new Color(colour));
                }

                break;

            case OnTop.Microscope:
                c.DrawRect(new Rect2(at + new Vector2(-4, -2), new Vector2(8, 2)), new Color("#37474F"));
                c.DrawLine(at + new Vector2(2, -2), at + new Vector2(2, -10), new Color("#37474F"), 1.5f);
                c.DrawLine(at + new Vector2(2, -10), at + new Vector2(-2, -14), new Color("#ECEFF1"), 2.2f);
                c.DrawRect(new Rect2(at + new Vector2(-3, -6), new Vector2(5, 1)), new Color("#90A4AE"));
                break;

            case OnTop.KettleMugs:
                c.DrawRect(new Rect2(at + new Vector2(-9, -9), new Vector2(7, 9)), new Color("#ECEFF1"));
                c.DrawRect(new Rect2(at + new Vector2(-9, -10), new Vector2(7, 2)), new Color("#2B3036"));
                c.DrawLine(at + new Vector2(-2, -7), at + new Vector2(0, -6), new Color("#ECEFF1"), 1.5f);
                foreach (var (x, colour) in new[] { (3f, "#E53935"), (8f, "#1F5FBF") })
                {
                    c.DrawRect(new Rect2(at + new Vector2(x, -5), new Vector2(4, 5)), new Color(colour));
                }

                break;

        }
    }

    // ------------------------------------------------------------------ hanging over the table

}
