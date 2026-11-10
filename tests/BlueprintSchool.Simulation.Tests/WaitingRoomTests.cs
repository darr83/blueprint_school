using BlueprintSchool.Simulation.Core;
using BlueprintSchool.Simulation.Rooms;
using BlueprintSchool.Simulation.Simulation;
using BlueprintSchool.Simulation.Staff;
using BlueprintSchool.Simulation.World;

namespace BlueprintSchool.Simulation.Tests;

/// <summary>The reception (a waiting room) the player builds and fits out, and chairs.</summary>
public class WaitingRoomTests
{
    private static readonly GridRect B = StarterSite.MainBuildingFootprint;

    /// <summary>The reception's spot: over the front path, joined to the main corridor.</summary>
    private static readonly GridRect Spot = new(B.X + 15, B.Y + 26, 22, 14);

    /// <summary>Middle of the front wall, where the path comes in.</summary>
    private static readonly GridCoord EntranceTile = new(B.X + 26, B.Bottom - 1);

    /// <summary>Middle of the north wall, onto the corridor coming down from the main corridor.</summary>
    private static readonly GridCoord InnerDoorTile = new(B.X + 26, B.Y + 26);

    private static readonly GridCoord DeskTile = new(B.X + 30, B.Y + 32);

    /// <summary>A new school with the main corridor, a link down to the front, and an empty reception built in the usual place.</summary>
    private static (SimulationHarness H, Room Room) EmptyWaitingRoom(int seed = 1234)
    {
        var h = new SimulationHarness(seed, waitingRoom: false);
        Assert.Empty(h.Sim.SpacesOfKind(RoomKind.Waiting));
        Assert.True(h.Sim.BuildRoom(new RoomPlan("corridor", new GridRect(B.X + 4, B.Y + 9, 56, 8), WallSide.South)).CanBuild);
        Assert.True(h.Sim.BuildRoom(new RoomPlan("corridor", new GridRect(B.X + 22, B.Y + 17, 8, 9), WallSide.South)).CanBuild);
        var plan = h.Sim.AutoPlan("waiting_room", Spot);
        var check = h.Sim.BuildRoom(plan);
        Assert.True(check.CanBuild, string.Join("; ", check.Problems));
        return (h, h.Sim.SpacesOfKind(RoomKind.Waiting).Single());
    }

    /// <summary>Puts the desk and both doors into a waiting room in the usual place.</summary>
    internal static void FitOut(SimulationHarness h, Room room)
    {
        var desk = h.Sim.PlaceReceptionDesk(room.Id, DeskTile, WallSide.South);
        Assert.True(desk.CanBuild, string.Join("; ", desk.Problems));
        var entrance = h.Sim.PlaceWaitingDoor(room.Id, EntranceTile);
        Assert.True(entrance.CanBuild, string.Join("; ", entrance.Problems));
        var inner = h.Sim.PlaceWaitingDoor(room.Id, InnerDoorTile);
        Assert.True(inner.CanBuild, string.Join("; ", inner.Problems));
    }

    private static Room Room(SimulationHarness h) => h.Sim.SpacesOfKind(RoomKind.Waiting).Single();

    [Fact]
    public void A_new_game_has_no_reception()
    {
        var h = new SimulationHarness(waitingRoom: false);
        Assert.Empty(h.Sim.SpacesOfKind(RoomKind.Waiting));
        Assert.Null(h.Sim.MainWaitingRoom);
    }

    [Fact]
    public void A_reception_is_built_empty_then_fitted_out()
    {
        var (h, room) = EmptyWaitingRoom();
        Assert.Empty(room.Doors);
        Assert.DoesNotContain(room.Furniture, f => f.ItemId == "reception_desk");
        Assert.Equal(3, h.Sim.WaitingRoomProblems(room).Count);

        var before = h.Sim.Balance;
        FitOut(h, Room(h));
        room = Room(h);
        Assert.Equal(before - h.Sim.ReceptionDeskCost - SchoolSimulation.EntranceDoorCost - SchoolSimulation.InnerDoorCost, h.Sim.Balance);
        Assert.Contains(room.Furniture, f => f.ItemId == "reception_desk");
        Assert.True(h.Sim.HasEntranceDoor(room));
        Assert.True(h.Sim.HasInnerDoor(room));
        Assert.Same(room, h.Sim.MainWaitingRoom);
        Assert.Empty(h.Sim.WaitingRoomProblems(room));

        // The receptionist works behind the desk; the queue runs out in front of it.
        Assert.Equal(new GridCoord(DeskTile.X, DeskTile.Y - 1), room.StaffSpot);
        Assert.Equal(new GridCoord(DeskTile.X, DeskTile.Y + 1), room.QueueTiles[0]);
        h.Sim.HireStaff(StaffRole.Receptionist);
        h.RunFor(TimeSpan.FromMinutes(5));
        Assert.Contains(h.Sim.Staff, s => s.Role == StaffRole.Receptionist && s.Tile == room.StaffSpot);
    }

    [Fact]
    public void The_entrance_door_must_be_on_the_front_path()
    {
        var (h, room) = EmptyWaitingRoom();

        // The west wall looks onto empty floor, not a corridor or the path.
        var west = h.Sim.CheckWaitingDoor(room.Id, new GridCoord(Spot.X, Spot.Y + 7));
        Assert.Contains(west.Problems, p => p.Contains("front path"));

        // Along the front wall but off the path, where there is no way out.
        var offPath = h.Sim.CheckWaitingDoor(room.Id, new GridCoord(Spot.X + 3, Spot.Bottom - 1));
        Assert.Contains(offPath.Problems, p => p.Contains("front path"));

        Assert.True(h.Sim.CheckWaitingDoor(room.Id, EntranceTile).CanBuild);
        Assert.True(h.Sim.PlaceWaitingDoor(room.Id, EntranceTile).CanBuild);
        Assert.Contains(h.Sim.CheckWaitingDoor(room.Id, EntranceTile).Problems, p => p.Contains("already a door"));

        // Taking a door out refunds half of it.
        var before = h.Sim.Balance;
        Assert.True(h.Sim.RemoveWaitingDoor(room.Id, EntranceTile).CanBuild);
        Assert.Equal(before + (SchoolSimulation.EntranceDoorCost / 2), h.Sim.Balance);
        Assert.Empty(Room(h).Doors);
    }

    [Fact]
    public void The_desk_must_fit_with_room_for_the_receptionist_and_a_queue()
    {
        var (h, room) = EmptyWaitingRoom();

        // Against the back wall facing out: the receptionist would be outside the room.
        var check = h.Sim.CheckReceptionDesk(room.Id, new GridCoord(Spot.X + 10, Spot.Y), WallSide.South);
        Assert.Contains(check.Problems, p => p.Contains("inside the room"));

        Assert.True(h.Sim.PlaceReceptionDesk(room.Id, DeskTile, WallSide.South).CanBuild);

        // Moving it again is free.
        var before = h.Sim.Balance;
        Assert.True(h.Sim.PlaceReceptionDesk(room.Id, new GridCoord(Spot.X + 6, Spot.Y + 6), WallSide.East).CanBuild);
        Assert.Equal(before, h.Sim.Balance);
        Assert.Equal(new GridCoord(Spot.X + 7, Spot.Y + 6), Room(h).UseSpot);
    }

    [Fact]
    public void Chairs_go_in_waiting_rooms_and_corridors_but_not_doorways_queues_or_other_rooms()
    {
        var (h, room) = EmptyWaitingRoom();
        FitOut(h, room);
        Assert.True(h.Sim.BuildRoom(new RoomPlan("staff_room", new GridRect(B.X + 4, B.Y, 8, 9), WallSide.South)).CanBuild);
        room = Room(h);
        var colour = ChairColours.Default;

        Assert.True(h.Sim.CheckChair(new GridCoord(Spot.X + 2, Spot.Y + 4), WallSide.South, colour).CanBuild);
        Assert.True(h.Sim.CheckChair(new GridCoord(B.X + 10, B.Y + 12), WallSide.South, colour).CanBuild);

        Assert.Contains(h.Sim.CheckChair(room.QueueTiles[1], WallSide.South, colour).Problems, p => p.Contains("reception desk"));
        Assert.Contains(h.Sim.CheckChair(new GridCoord(EntranceTile.X, EntranceTile.Y), WallSide.South, colour).Problems, p => p.Contains("doorway"));
        Assert.Contains(h.Sim.CheckChair(new GridCoord(InnerDoorTile.X, InnerDoorTile.Y - 1), WallSide.South, colour).Problems, p => p.Contains("doorway"));
        var staffRoom = h.Sim.SpacesOfKind(RoomKind.StaffRoom).Single();
        Assert.Contains(h.Sim.CheckChair(new GridCoord(staffRoom.Rect.X + 2, staffRoom.Rect.Y + 2), WallSide.South, colour).Problems, p => p.Contains("waiting room or along a corridor"));

        // A new chair lines up with the one beside it; next to a wall it has its back to the wall.
        Assert.True(h.Sim.PlaceChair(new GridCoord(Spot.X + 2, Spot.Y + 4), WallSide.East, colour).CanBuild);
        Assert.Equal(WallSide.East, h.Sim.SuggestChairFacing(new GridCoord(Spot.X + 2, Spot.Y + 5)));
        Assert.Equal(WallSide.East, h.Sim.SuggestChairFacing(new GridCoord(Spot.X, Spot.Y + 9)));

        // Against a wall the back goes to the wall, even beside a chair facing along the wall.
        Assert.True(h.Sim.PlaceChair(new GridCoord(Spot.X, Spot.Y + 10), WallSide.South, colour).CanBuild);
        Assert.Equal(WallSide.East, h.Sim.SuggestChairFacing(new GridCoord(Spot.X, Spot.Y + 9)));
        Assert.Equal(WallSide.East, h.Sim.SuggestChairFacing(new GridCoord(Spot.X, Spot.Y + 11)));

        // In a corner it follows the chairs beside it, as long as that keeps its back to a wall.
        Assert.True(h.Sim.PlaceChair(new GridCoord(Spot.X + 1, Spot.Y), WallSide.South, colour).CanBuild);
        Assert.Equal(WallSide.South, h.Sim.SuggestChairFacing(new GridCoord(Spot.X, Spot.Y)));

        // Chairs stand clear of the walls beside them; out in the open they stand in the middle of their tile.
        Assert.Equal((SchoolSimulation.ChairWallGap, 0.0), h.Sim.ChairShift(new GridCoord(Spot.X, Spot.Y + 9)));
        Assert.Equal((SchoolSimulation.ChairWallGap, SchoolSimulation.ChairWallGap), h.Sim.ChairShift(new GridCoord(Spot.X, Spot.Y)));
        Assert.Equal((0.0, 0.0), h.Sim.ChairShift(new GridCoord(Spot.X + 2, Spot.Y + 4)));
    }

    [Fact]
    public void Desk_doors_and_chairs_are_saved_and_loaded()
    {
        var (h, room) = EmptyWaitingRoom();
        FitOut(h, room);
        var plum = ChairColours.All[3].Hex;
        h.Sim.PlaceChair(new GridCoord(Spot.X + 2, Spot.Y + 4), WallSide.East, plum);
        h.Sim.PlaceChair(new GridCoord(B.X + 10, B.Y + 12), WallSide.North, ChairColours.Default);

        var save = SaveSerializer.Deserialize(SaveSerializer.Serialize(new SaveGame { Layout = h.Sim.CaptureLayout() }));
        var to = new SimulationHarness(waitingRoom: false);
        to.Sim.RestoreLayout(save.Layout!);

        var loaded = Room(to);
        Assert.Equal(Room(h).Doors, loaded.Doors);
        Assert.Equal(Room(h).StaffSpot, loaded.StaffSpot);
        Assert.Equal(Room(h).QueueTiles, loaded.QueueTiles);
        Assert.Equal(h.Sim.Chairs.OrderBy(c => c.Tile.X).ThenBy(c => c.Tile.Y), to.Sim.Chairs.OrderBy(c => c.Tile.X).ThenBy(c => c.Tile.Y));
        Assert.Same(loaded, to.Sim.MainWaitingRoom);
    }

    [Fact]
    public void The_starter_reception_comes_fitted_out_with_its_corridors()
    {
        var h = new SimulationHarness(waitingRoom: true);
        var room = Room(h);
        Assert.Same(room, h.Sim.MainWaitingRoom);
        Assert.Empty(h.Sim.WaitingRoomProblems(room));
        Assert.Equal(2, h.Sim.SpacesOfKind(RoomKind.Corridor).Count());
        Assert.NotEmpty(h.Sim.Chairs);
    }
}
