using BlueprintSchool.Simulation.Rooms;
using BlueprintSchool.Simulation.Simulation;
using BlueprintSchool.Simulation.Staff;
using BlueprintSchool.Simulation.World;

namespace BlueprintSchool.Simulation.Tests;

public class ToiletTests
{
    private static readonly GridRect B = StarterSite.MainBuildingFootprint;

    private static Room Layout(SimulationHarness h, string template, double widthM, double lengthM, WallSide side = WallSide.South) =>
        RoomLayoutEngine.Build(h.Database.RoomTemplates[template], h.Database, "r", "R",
            RoomLayoutEngine.RectFor(new GridCoord(0, 0), Units.MetresToTiles(widthM), Units.MetresToTiles(lengthM), side), side);

    private static int Count(Room room, string item) => room.Furniture.Count(f => f.ItemId == item);

    /// <summary>A west wing with a corridor and, south of it, a toilet block, an accessible toilet and a staff toilet.</summary>
    private static SimulationHarness WithToilets(bool publicToilets = true, bool accessible = true, bool staff = true)
    {
        var h = new SimulationHarness(5, mainCorridor: true);
        h.Sim.AdjustBalance(2_000_000);
        Assert.True(h.Sim.ExtendBuilding(new GridRect(B.X - 30, B.Y, 30, 34)).CanBuild);
        Assert.True(h.Sim.BuildRoom(new RoomPlan("corridor", new GridRect(B.X - 30, B.Y + 9, 34, 8), WallSide.South)).CanBuild);
        void Build(string template, GridRect rect)
        {
            var check = h.Sim.BuildRoom(h.Sim.AutoPlan(template, rect));
            Assert.True(check.CanBuild, string.Join("; ", check.Problems));
        }

        if (publicToilets)
        {
            Build("public_toilets", new GridRect(B.X - 30, B.Y + 17, 10, 8));
        }

        if (accessible)
        {
            Build("accessible_toilet", new GridRect(B.X - 18, B.Y + 17, 5, 5));
        }

        if (staff)
        {
            Build("staff_toilet", new GridRect(B.X - 11, B.Y + 17, 3, 5));
        }

        return h;
    }

    [Fact]
    public void A_toilet_block_has_more_cubicles_the_wider_it_is_and_basins_inside_the_room()
    {
        var h = new SimulationHarness();
        var counts = new[] { 3.0, 3.5, 5, 7, 10 }.Select(w => Count(Layout(h, "public_toilets", w, 3.5), "toilet_cubicle")).ToList();
        Assert.Equal(new[] { 2, 3, 4, 6, 6 }, counts);

        foreach (var side in Enum.GetValues<WallSide>())
        {
            foreach (var (w, l) in new[] { (3.0, 3.0), (5.0, 3.5), (7.0, 4.0) })
            {
                var room = Layout(h, "public_toilets", w, l, side);
                Assert.Equal(Count(room, "toilet_cubicle"), room.ToiletSpots.Count);
                Assert.Equal(room.ToiletSpots.Count, room.ToiletSpots.Distinct().Count());
                Assert.NotEmpty(room.SinkSpots);
                Assert.Equal(room.SinkSpots.Count, Count(room, "wash_basin"));
                Assert.NotEmpty(room.QueueTiles);
                Assert.All(room.ToiletSpots.Concat(room.SinkSpots).Concat(room.QueueTiles), t => Assert.True(room.Rect.Contains(t)));
            }
        }
    }

    [Fact]
    public void Accessible_and_staff_toilets_hold_one_toilet_and_a_basin_at_every_size_and_direction()
    {
        var h = new SimulationHarness();
        foreach (var side in Enum.GetValues<WallSide>())
        {
            foreach (var (template, w, l, pan) in new[] { ("accessible_toilet", 2.5, 2.5, "accessible_wc"), ("accessible_toilet", 3.0, 3.0, "accessible_wc"), ("staff_toilet", 1.5, 2.5, "toilet_pan"), ("staff_toilet", 2.0, 3.0, "toilet_pan") })
            {
                var room = Layout(h, template, w, l, side);
                Assert.Equal(1, Count(room, pan));
                Assert.Equal(1, Count(room, "wash_basin"));
                Assert.Single(room.ToiletSpots);
                Assert.Single(room.SinkSpots);
                Assert.All(room.ToiletSpots.Concat(room.SinkSpots), t => Assert.True(room.Rect.Contains(t)));
            }
        }
    }

    [Fact]
    public void Staff_on_a_break_use_the_toilet_and_wash_their_hands()
    {
        var h = WithToilets();
        foreach (var _ in Enumerable.Range(0, 6))
        {
            h.Sim.HireStaff(StaffRole.Teacher);
        }

        ToiletTrip? trip = null;
        h.RunUntil(() => (trip = h.Sim.ToiletTrips.FirstOrDefault()) is not null, TimeSpan.FromHours(12), "a member of staff to go to the toilet on a break");
        var member = Assert.IsType<StaffMember>(trip!.Person);
        Assert.True(member.IsOnBreak);

        var washed = false;
        h.RunUntil(() =>
        {
            washed |= h.Sim.ToiletTripOf(member)?.Stage == ToiletStage.Washing;
            return h.Sim.ToiletTripOf(member) is null;
        }, TimeSpan.FromMinutes(30), "them to finish at the toilet");
        Assert.True(washed, "they didn't wash their hands");
    }

    [Fact]
    public void Public_toilets_count_as_toilets_for_pupils_and_visitors()
    {
        Assert.False(WithToilets(publicToilets: false, accessible: false).Sim.HasPublicToilets);
        Assert.True(WithToilets(staff: false).Sim.HasPublicToilets);
    }

    [Fact]
    public void Toilets_are_in_the_facilities_build_menu()
    {
        var h = new SimulationHarness();
        foreach (var id in new[] { "public_toilets", "accessible_toilet", "staff_toilet" })
        {
            Assert.Equal("Facilities", h.Database.RoomTemplates[id].Category);
        }
    }
}
