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

public class SchoolAnnouncementTests
{
    [Fact]
    public void A_school_day_has_its_announcements_in_order_and_each_has_a_line_to_record()
    {
        var h = new SimulationHarness(start: new DateTime(2027, 1, 5, 8, 0, 0));
        var heard = new List<Announcement>();
        h.Events.Subscribe<Announcement>(a => heard.Add(a));
        h.RunFor(TimeSpan.FromHours(8));

        var expected = h.Sim.AnnouncementsFor(DayOfWeek.Tuesday).OrderBy(a => a.At).Select(a => a.Clip).ToList();
        Assert.Equal(expected, heard.Select(a => a.Clips[0]));
        Assert.Contains("gen_morning", expected);
        Assert.Contains("gen_buses", expected);

        var lines = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(TestData.GameDataDirectory(), "..", "..", "tools", "SoundBaker", "pa_lines.json")))!;
        foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
        {
            Assert.All(h.Sim.AnnouncementsFor(day), a => Assert.Equal(lines[a.Clip], a.Text));
        }
    }

    [Fact]
    public void Nothing_is_announced_at_the_weekend()
    {
        var h = new SimulationHarness(start: new DateTime(2027, 1, 9, 8, 0, 0));
        var heard = new List<Announcement>();
        h.Events.Subscribe<Announcement>(a => heard.Add(a));
        h.RunFor(TimeSpan.FromHours(8));
        Assert.Empty(heard);
    }
}
