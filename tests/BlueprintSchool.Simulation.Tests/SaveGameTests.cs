using BlueprintSchool.Simulation.Core;

namespace BlueprintSchool.Simulation.Tests;

public class SaveGameTests
{
    [Fact]
    public void Save_round_trips()
    {
        var clock = new SimulationClock();
        clock.Advance(30);
        clock.SetSpeed(SimulationSpeed.Fast);
        var save = new SaveGame
        {
            SchoolName = "St Elsewhere Academy",
            SavedAtUtc = new DateTime(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc),
            Clock = clock.CaptureState(),
            Camera = new CameraState(120.5, -40, 1.5),
        };

        var loaded = SaveSerializer.Deserialize(SaveSerializer.Serialize(save));

        Assert.Equal("St Elsewhere Academy", loaded.SchoolName);
        Assert.Equal(save.Clock, loaded.Clock);
        Assert.Equal(save.Camera, loaded.Camera);
    }

    [Fact]
    public void Damaged_save_gives_clear_error()
    {
        var ex = Assert.Throws<InvalidDataException>(() => SaveSerializer.Deserialize("{ not json"));
        Assert.Contains("damaged", ex.Message);
    }

    [Fact]
    public void Newer_save_format_is_refused()
    {
        var json = SaveSerializer.Serialize(new SaveGame { FormatVersion = SaveGame.CurrentFormatVersion + 1 });
        Assert.Throws<InvalidDataException>(() => SaveSerializer.Deserialize(json));
    }

    [Theory]
    [InlineData("My Hospital", "my_hospital")]
    [InlineData("  ../../etc  ", "etc")]
    [InlineData("!!!", "save")]
    public void Slot_names_are_safe(string input, string expected)
    {
        Assert.Equal(expected, SaveSerializer.ToSlotName(input));
    }
}

public class FullSaveTests
{
    private static readonly World.GridRect B = World.StarterSite.MainBuildingFootprint;

    private static SimulationHarness BusySchool(int seed)
    {
        var h = new SimulationHarness(seed: seed, start: new DateTime(2027, 1, 4, 6, 0, 0), waitingRoom: true);
        Assert.True(h.Sim.BuildRoom(h.Sim.AutoPlan("staff_room", new World.GridRect(B.X + 4, B.Y, 12, 9))).CanBuild);
        h.StaffToRecommended();
        h.Sim.HireStaff(Staff.StaffRole.Teacher, shift: Staff.StaffShift.Day);
        h.Sim.HireStaff(Staff.StaffRole.TeachingAssistant, shift: Staff.StaffShift.Day);
        h.RunFor(TimeSpan.FromHours(6));
        return h;
    }

    private static SimulationHarness Load(SimulationHarness from)
    {
        var json = SaveSerializer.Serialize(new SaveGame
        {
            Clock = from.Clock.CaptureState(),
            Layout = from.Sim.CaptureLayout(),
            People = from.Sim.CapturePeople(),
        });
        var save = SaveSerializer.Deserialize(json);
        var to = new SimulationHarness(seed: 99);
        to.Clock.RestoreState(save.Clock);
        to.Sim.RestoreLayout(save.Layout!);
        to.Sim.RestorePeople(save.People!);
        return to;
    }

    [Fact]
    public void Staff_rooms_and_money_survive_save_and_load_exactly()
    {
        var h = BusySchool(seed: 8);
        Assert.NotEmpty(h.Sim.Staff);
        var loaded = Load(h);

        string Snapshot(SimulationHarness x) => SaveSerializer.Serialize(new SaveGame { People = x.Sim.CapturePeople(), Layout = x.Sim.CaptureLayout() });
        Assert.Equal(Snapshot(h), Snapshot(loaded));
        Assert.Equal(h.Sim.Balance, loaded.Sim.Balance);
    }

    [Fact]
    public void A_loaded_school_carries_on_running()
    {
        var h = BusySchool(seed: 3);
        var loaded = Load(h);
        var balance = loaded.Sim.Balance;

        loaded.RunFor(TimeSpan.FromHours(6));

        Assert.True(loaded.Sim.Balance < balance, "wages and upkeep should keep being paid");
        Assert.All(loaded.Sim.Staff, s => Assert.True(loaded.Sim.Nav.IsWalkable(s.Tile), $"{s.Name} is stuck at {s.Tile}"));
    }
}
