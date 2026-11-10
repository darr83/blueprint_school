using BlueprintSchool.Simulation.Agents;
using BlueprintSchool.Simulation.Rooms;
using BlueprintSchool.Simulation.Simulation;
using BlueprintSchool.Simulation.World;

namespace BlueprintSchool.Simulation.Tests;

public class FloorTests
{
    private static readonly GridRect B = StarterSite.MainBuildingFootprint;

    /// <summary>A lift south of the main corridor, its door onto it.</summary>
    private static readonly GridRect LiftRect = new(B.X + 48, B.Y + 17, 5, 6);

    /// <summary>Stairs next to it.</summary>
    private static readonly GridRect StairsRect = new(B.X + 54, B.Y + 17, 5, 8);

    /// <summary>The main corridor along the ground floor, the way most schools start.</summary>
    private static readonly GridRect MainCorridor = new(B.X + 4, B.Y + 9, 56, 8);

    private static SimulationHarness Rich(int seed = 1234)
    {
        var h = new SimulationHarness(seed);
        h.Sim.AdjustBalance(5_000_000);
        Assert.True(h.Sim.BuildRoom(new RoomPlan("corridor", MainCorridor, WallSide.South)).CanBuild);
        return h;
    }

    private static GridRect Up(SimulationHarness h, GridRect local, int floor = 1) => h.Sim.Site.OnFloor(local, floor);

    private static void Build(SimulationHarness h, string template, GridRect rect)
    {
        var check = h.Sim.BuildRoom(h.Sim.AutoPlan(template, rect));
        Assert.True(check.CanBuild, $"{template}: {string.Join("; ", check.Problems)}");
    }

    /// <summary>Floor 1: a corridor along the same line as the main corridor below, starting from the lift (or stairs) door.</summary>
    private static GridRect UpstairsCorridor(SimulationHarness h) => Up(h, MainCorridor);

    [Fact]
    public void Adding_a_floor_costs_money_and_copies_the_outline()
    {
        var h = new SimulationHarness();
        var before = h.Sim.Balance;
        var cost = h.Sim.CheckAddFloor().Cost;
        Assert.Equal((int)Math.Round(h.Sim.MainBuilding.AreaSquareMetres * SchoolSimulation.FloorCostPerSquareMetre), cost);

        var check = h.Sim.AddFloor();
        Assert.True(check.CanBuild, string.Join("; ", check.Problems));
        Assert.Equal(2, h.Sim.FloorCount);
        Assert.Equal(before - cost, h.Sim.Balance);

        // Upstairs is floor inside the outline and nothing outside it.
        var inside = h.Sim.Site.OnFloor(new GridCoord(B.X + 10, B.Y + 10), 1);
        var outside = h.Sim.Site.OnFloor(new GridCoord(B.X - 3, B.Y + 10), 1);
        Assert.Equal(1, h.Sim.FloorOf(inside));
        Assert.True(h.Sim.Site.IsIndoors(inside));
        Assert.False(h.Sim.Site.IsIndoors(outside));
        Assert.True(h.Sim.Nav.IsWalkable(inside));
        Assert.False(h.Sim.Nav.IsWalkable(outside));

        // The next floor up costs more per square metre.
        Assert.True(h.Sim.CheckAddFloor().Cost > cost);
    }

    [Fact]
    public void The_building_has_at_most_five_floors()
    {
        var h = Rich();
        for (var i = 0; i < SiteMap.MaxUpperFloors; i++)
        {
            Assert.True(h.Sim.AddFloor().CanBuild);
        }

        Assert.Contains(h.Sim.CheckAddFloor().Problems, p => p.Contains("at most"));
    }

    [Fact]
    public void Rooms_upstairs_need_a_lift_or_stairs_to_reach_them()
    {
        var h = Rich();
        h.Sim.AddFloor();

        // No way up yet, so a corridor can't start anywhere upstairs.
        var corridor = h.Sim.CheckRoomAt("corridor", UpstairsCorridor(h));
        Assert.Contains(corridor.Problems, p => p.Contains("Must join"));

        Build(h, "lift", LiftRect);
        Assert.Equal(2, h.Sim.Spaces.Count(s => s.Kind == RoomKind.Lift));
        Build(h, "corridor", UpstairsCorridor(h));

        // A staff room upstairs, reached through the lift.
        Build(h, "staff_room", Up(h, new GridRect(B.X + 20, B.Y + 17, 14, 10)));
        Assert.Contains(h.Sim.Spaces, s => s.Kind == RoomKind.StaffRoom && h.Sim.FloorOf(s) == 1);
    }

    [Fact]
    public void Lifts_and_stairs_go_through_floors_added_later_and_cost_each_floor()
    {
        var h = Rich();
        var single = h.Sim.CheckRoomAt("lift", LiftRect).Cost;
        h.Sim.AddFloor();
        var twoFloors = h.Sim.CheckRoomAt("lift", LiftRect);
        Assert.Equal(single * 2, twoFloors.Cost);
        Build(h, "lift", LiftRect);

        var floorWithoutLift = (int)Math.Round(h.Sim.MainBuilding.AreaSquareMetres * (SchoolSimulation.FloorCostPerSquareMetre + SchoolSimulation.FloorCostRisePerSquareMetre));
        Assert.Equal(floorWithoutLift + single, h.Sim.CheckAddFloor().Cost);
        h.Sim.AddFloor();

        var shaft = Assert.Single(h.Sim.Shafts);
        Assert.Equal(new[] { 0, 1, 2 }, shaft.Select(h.Sim.FloorOf));
        Assert.All(shaft, s => Assert.Equal(h.Sim.Site.Local(shaft[0].Rect), h.Sim.Site.Local(s.Rect)));
        Assert.All(shaft, s => Assert.Equal(shaft[0].Name, s.Name));
        Assert.All(shaft, s => Assert.Equal(shaft[0].Doors, s.Doors));

        // Demolishing takes it out of every floor.
        var demolish = h.Sim.DemolishRoom(shaft[1].Id);
        Assert.True(demolish.CanBuild, string.Join("; ", demolish.Problems));
        Assert.DoesNotContain(h.Sim.Spaces, s => s.IsShaft);
    }

    [Fact]
    public void A_lift_needs_the_same_space_clear_on_every_floor()
    {
        var h = Rich();
        h.Sim.AddFloor();
        Build(h, "stairs", StairsRect);
        Build(h, "corridor", UpstairsCorridor(h));
        Build(h, "staff_room", Up(h, new GridRect(B.X + 44, B.Y + 17, 8, 8)));

        // Downstairs is clear, but upstairs the staff room is in the way.
        var check = h.Sim.CheckRoomAt("lift", new GridRect(B.X + 48, B.Y + 17, 5, 6));
        Assert.Contains(check.Problems, p => p.Contains("Overlaps") && p.Contains("every floor"));
    }

    [Fact]
    public void Walkers_take_the_stairs_for_one_floor_and_step_free_routes_take_the_lift()
    {
        var h = Rich();
        h.Sim.AddFloor();
        Build(h, "lift", LiftRect);
        Build(h, "stairs", StairsRect);
        Build(h, "corridor", UpstairsCorridor(h));

        var stairs = h.Sim.Spaces.First(s => s.Kind == RoomKind.Stairs && h.Sim.FloorOf(s) == 0);
        var lift = h.Sim.Spaces.First(s => s.Kind == RoomKind.Lift && h.Sim.FloorOf(s) == 0);
        var from = new GridCoord(B.X + 50, B.Y + 12);
        var to = h.Sim.Site.OnFloor(new GridCoord(B.X + 52, B.Y + 12), 1);

        var walk = (NavPath)h.Sim.Nav.FindPath(from, to);
        var rideAt = Enumerable.Range(1, walk.Count - 1).Single(i => walk.HopAt(i) > 0);
        Assert.Equal(stairs.UseSpot, walk[rideAt - 1]);

        var wheeled = (NavPath)h.Sim.Nav.FindPath(from, to, stepFreeOnly: true);
        var liftAt = Enumerable.Range(1, wheeled.Count - 1).Single(i => wheeled.HopAt(i) > 0);
        Assert.Equal(lift.UseSpot, wheeled[liftAt - 1]);
        Assert.Equal(VerticalTransport.LiftWait + VerticalTransport.LiftPerFloor, wheeled.HopAt(liftAt));
    }

    [Fact]
    public void Someone_walking_upstairs_waits_for_the_ride_then_steps_out_on_the_next_floor()
    {
        var h = Rich();
        h.Sim.AddFloor();
        Build(h, "stairs", StairsRect);
        Build(h, "corridor", UpstairsCorridor(h));

        var walker = new TestWalker();
        var start = new GridCoord(B.X + 50, B.Y + 12);
        var goal = h.Sim.Site.OnFloor(new GridCoord(B.X + 30, B.Y + 12), 1);
        walker.PlaceAt(start);
        walker.FollowPath(h.Sim.Nav.FindPath(start, goal));

        var floorsSeen = new List<int>();
        for (var i = 0; i < 2000 && walker.IsMoving; i++)
        {
            walker.Move(0.25);
            floorsSeen.Add(h.Sim.Site.FloorOf(walker.Y));

            // Never drawn halfway between floors.
            Assert.Equal(h.Sim.Site.FloorOf(walker.Y), h.Sim.Site.FloorOf(walker.PreviousY));
        }

        Assert.Equal(goal, walker.Tile);
        Assert.Equal(new[] { 0, 1 }, floorsSeen.Distinct());
    }

    [Fact]
    public void Floors_lifts_and_stairs_survive_save_and_load()
    {
        var h = Rich();
        h.Sim.AddFloor();
        Build(h, "lift", LiftRect);
        Build(h, "corridor", UpstairsCorridor(h));
        Build(h, "staff_room", Up(h, new GridRect(B.X + 20, B.Y + 17, 14, 10)));
        var layout = h.Sim.CaptureLayout();
        Assert.Equal(2, layout.Floors);

        var loaded = new SimulationHarness();
        loaded.Sim.RestoreLayout(layout);
        Assert.Equal(2, loaded.Sim.FloorCount);
        Assert.Equal(2, loaded.Sim.Spaces.Count(s => s.Kind == RoomKind.Lift));
        Assert.Contains(loaded.Sim.Spaces, s => s.Kind == RoomKind.StaffRoom && loaded.Sim.FloorOf(s) == 1);
        Assert.Contains(loaded.Sim.Nav.Links, l => l.Link.StepFree);

        // A layout with no floor count loads as one floor.
        var old = new LayoutState { Balance = 1000 };
        var fresh = new SimulationHarness();
        fresh.Sim.RestoreLayout(old);
        Assert.Equal(1, fresh.Sim.FloorCount);
    }

    [Fact]
    public void Extending_a_tall_building_extends_every_floor_and_costs_each_floor()
    {
        var h = Rich();
        var area = new GridRect(B.X - 10, B.Y, 10, 10);
        var single = h.Sim.CheckExtension(area).Cost;
        h.Sim.AddFloor();
        Assert.Equal(single * 2, h.Sim.CheckExtension(area).Cost);
        Assert.True(h.Sim.ExtendBuilding(area).CanBuild);
        Assert.True(h.Sim.Site.IsIndoors(h.Sim.Site.OnFloor(new GridCoord(B.X - 5, B.Y + 5), 1)));
    }

    private sealed class TestWalker : Agent;
}
