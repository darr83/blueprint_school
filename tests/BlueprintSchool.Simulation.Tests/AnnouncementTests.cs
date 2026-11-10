using BlueprintSchool.Simulation.Simulation;

namespace BlueprintSchool.Simulation.Tests;

public class AnnouncementTests
{
    [Fact]
    public void Fire_alarm_test_is_announced_on_wednesday_morning()
    {
        // 6 January 2027 is a Wednesday.
        var h = new SimulationHarness(start: new DateTime(2027, 1, 6, 9, 55, 0));
        var heard = new List<Announcement>();
        h.Events.Subscribe<Announcement>(a => heard.Add(a));

        h.RunFor(TimeSpan.FromMinutes(10));

        var test = Assert.Single(heard);
        Assert.Equal(AnnouncementKind.General, test.Kind);
        Assert.Equal(new[] { "gen_fire_test" }, test.Clips);
    }
}
