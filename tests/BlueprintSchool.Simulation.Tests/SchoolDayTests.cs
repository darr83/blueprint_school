using BlueprintSchool.Simulation.Data.Models;
using BlueprintSchool.Simulation.Rooms;
using BlueprintSchool.Simulation.SchoolDay;
using BlueprintSchool.Simulation.World;

namespace BlueprintSchool.Simulation.Tests;

public class SchoolDayTests
{
    private static readonly DateTime Monday = new(2027, 1, 4);

    [Fact]
    public void Key_stage_3_subjects_fill_a_25_period_week()
    {
        var h = new SimulationHarness();
        Assert.Equal(25, h.Database.Subjects.Values.Sum(s => s.Ks3PeriodsPerWeek));
        Assert.Equal(25, h.Database.Bells.Lessons.Count() * 5);
    }

    [Fact]
    public void Every_subject_has_a_department_and_rooms_that_exist()
    {
        var h = new SimulationHarness();
        foreach (var subject in h.Database.Subjects.Values)
        {
            Assert.True(h.Database.Departments.ContainsKey(subject.Department), $"{subject.Id}: no department {subject.Department}");
            Assert.NotEmpty(subject.Rooms);
            Assert.All(subject.Rooms, r => Assert.True(h.Database.RoomTemplates.ContainsKey(r), $"{subject.Id}: no room {r}"));
        }
    }

    [Fact]
    public void The_school_day_runs_from_gates_to_clubs_with_no_gaps()
    {
        var bells = new SimulationHarness().Database.Bells;
        Assert.Empty(bells.Problems());
        Assert.Equal(new TimeSpan(8, 20, 0), bells.DayStart);
        Assert.Equal(new TimeSpan(16, 0, 0), bells.DayEnd);
        Assert.Equal(5, bells.Lessons.Count());
    }

    [Fact]
    public void At_finds_the_period_and_nothing_at_weekends_or_at_night()
    {
        var bells = new SimulationHarness().Database.Bells;
        Assert.Equal("p1", bells.At(Monday.AddHours(9))?.Id);
        Assert.Equal(BellPeriodKind.Break, bells.At(Monday + new TimeSpan(10, 45, 0))?.Kind);
        Assert.Equal(BellPeriodKind.Lunch, bells.At(Monday + new TimeSpan(13, 0, 0))?.Kind);
        Assert.Null(bells.At(Monday.AddHours(7)));
        Assert.Null(bells.At(Monday.AddHours(17)));
        Assert.Null(bells.At(Monday.AddDays(5).AddHours(10)));
    }

    [Fact]
    public void Position_gives_progress_through_the_period_and_the_next_one()
    {
        var bells = new SimulationHarness().Database.Bells;
        var p = bells.Position(Monday + new TimeSpan(9, 45 + 27, 30));
        Assert.Equal("p2", p.Current?.Id);
        Assert.Equal(BellPeriodKind.Break, p.Next?.Kind);
        Assert.InRange(p.Progress, 0.49, 0.51);

        var early = bells.Position(Monday.AddHours(7));
        Assert.Null(early.Current);
        Assert.Equal("arrival", early.Next?.Id);
    }

    [Fact]
    public void A_bell_rings_once_at_the_start_of_a_period_but_not_when_the_gates_open()
    {
        var bells = new SimulationHarness().Database.Bells;
        var p1 = Monday + new TimeSpan(8, 50, 0);
        Assert.Equal("p1", bells.BellBetween(p1.AddSeconds(-5), p1)?.Id);
        Assert.Null(bells.BellBetween(p1, p1.AddSeconds(5)));
        var gates = Monday + new TimeSpan(8, 20, 0);
        Assert.Null(bells.BellBetween(gates.AddSeconds(-5), gates));
        Assert.Null(bells.BellBetween(p1.AddDays(5).AddSeconds(-5), p1.AddDays(5)));
    }

    [Fact]
    public void Problems_finds_gaps_and_overlaps()
    {
        BellPeriodDefinition P(string id, string start, string end) => new() { Id = id, Name = id, ShortName = id, Kind = BellPeriodKind.Lesson, Start = start, End = end };
        var gap = new BellSchedule(new[] { P("a", "09:00", "10:00"), P("b", "10:05", "11:00") });
        var overlap = new BellSchedule(new[] { P("a", "09:00", "10:00"), P("b", "09:55", "11:00") });
        Assert.Contains(gap.Problems(), p => p.Contains("gap"));
        Assert.Contains(overlap.Problems(), p => p.Contains("before"));
    }

    [Fact]
    public void The_simulation_rings_the_bell_for_each_period_in_a_day()
    {
        var h = new SimulationHarness(start: Monday.AddHours(8));
        var rung = new List<string>();
        h.Events.Subscribe<BellRang>(b => rung.Add(b.Period.Id));
        h.RunFor(TimeSpan.FromHours(9));
        Assert.Equal(h.Database.Bells.Periods.Where(p => p.Bell).Select(p => p.Id), rung);
    }

    [Theory]
    [InlineData("classroom", 24)]
    [InlineData("english_classroom", 24)]
    [InlineData("science_lab", 24)]
    [InlineData("ict_suite", 20)]
    [InlineData("small_group_room", 4)]
    public void Standard_teaching_rooms_seat_a_class(string templateId, int atLeast)
    {
        var h = new SimulationHarness();
        var t = h.Database.RoomTemplates[templateId];
        var size = t.Sizes.FirstOrDefault(s => s.Name == "Standard") ?? t.Sizes[^1];
        foreach (var side in Enum.GetValues<WallSide>())
        {
            var rect = RoomLayoutEngine.RectFor(new GridCoord(10, 10), Units.MetresToTiles(size.WidthM), Units.MetresToTiles(size.LengthM), side);
            var room = RoomLayoutEngine.Build(t, h.Database, "r", "R", rect, side);
            Assert.InRange(room.Seats.Count, atLeast, Math.Max(atLeast, t.MaxPupils));
        }
    }
}
