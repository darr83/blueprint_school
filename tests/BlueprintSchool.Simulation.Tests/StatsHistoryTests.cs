using BlueprintSchool.Simulation.Core;
using BlueprintSchool.Simulation.Simulation;

namespace BlueprintSchool.Simulation.Tests;

public class StatsHistoryTests
{
    private static readonly DateTime Start = new(2027, 1, 4, 10, 0, 0);

    [Fact]
    public void Counts_fall_into_the_right_periods()
    {
        var history = new StatsHistory(Start);
        history.Add(Start, "x");                         // day 1, 10:00
        history.Add(Start.AddHours(30), "x", 2);         // day 2, 16:00
        history.Add(Start.AddDays(10), "x", 4);          // day 11
        var now = Start.AddDays(10).AddHours(1);

        Assert.Equal(4, history.Total(StatsPeriod.Last24Hours, now, "x"));
        Assert.Equal(4, history.Total(StatsPeriod.Last7Days, now, "x"));
        Assert.Equal(7, history.Total(StatsPeriod.Last28Days, now, "x"));
        Assert.Equal(7, history.Total(StatsPeriod.AllTime, now, "x"));
        Assert.Equal(TimeSpan.FromDays(7), history.Covered(StatsPeriod.Last7Days, now));
        Assert.Equal(TimeSpan.FromHours(24), history.Covered(StatsPeriod.Last24Hours, now));
    }

    [Fact]
    public void Old_hours_and_days_drop_off_but_all_time_keeps_them()
    {
        var history = new StatsHistory(Start);
        for (var day = 0; day < 40; day++)
        {
            history.Add(Start.AddDays(day), "x");
        }

        var now = Start.AddDays(39);
        Assert.Equal(1, history.Total(StatsPeriod.Last24Hours, now, "x"));
        Assert.Equal(28, history.Total(StatsPeriod.Last28Days, now, "x"));
        Assert.Equal(40, history.Total(StatsPeriod.AllTime, now, "x"));
        Assert.Equal(28, history.Series(StatsPeriod.Last28Days, now, "x").Count);
        Assert.Equal(24, history.Series(StatsPeriod.Last24Hours, now, "x").Count);
    }

    [Fact]
    public void Gauges_give_average_and_peak()
    {
        var history = new StatsHistory(Start);
        history.Sample(Start, "g", 2);
        history.Sample(Start.AddMinutes(15), "g", 6);
        history.Sample(Start.AddMinutes(30), "g", 4);

        var readings = history.Gauge(StatsPeriod.Last24Hours, Start.AddHours(1), "g");
        Assert.Equal(4, readings.Average, 3);
        Assert.Equal(6, readings.Peak);
        Assert.Equal(3, readings.Count);
    }

    [Fact]
    public void A_day_shows_up_in_the_school_stats()
    {
        var h = new SimulationHarness(seed: 5);
        h.Sim.HireStaff(Staff.StaffRole.Teacher);
        h.RunFor(TimeSpan.FromHours(12));

        var history = h.Sim.History;
        var now = h.Sim.Now;
        Assert.True(history.Gauge(StatsPeriod.Last24Hours, now, StatKeys.StaffOnDuty).Count >= 48);

        // Money is counted as it is booked.
        var wages = history.Total(StatsPeriod.Last24Hours, now, StatKeys.Money(MoneyCategory.Staff));
        Assert.True(wages < 0);
    }

    [Fact]
    public void Stats_survive_save_and_load()
    {
        var h = new SimulationHarness(seed: 7);
        h.Sim.HireStaff(Staff.StaffRole.Teacher);
        h.RunFor(TimeSpan.FromHours(6));
        var json = SaveSerializer.Serialize(new SaveGame { Clock = h.Clock.CaptureState(), Layout = h.Sim.CaptureLayout(), People = h.Sim.CapturePeople() });
        var save = SaveSerializer.Deserialize(json);
        var to = new SimulationHarness(seed: 99);
        to.Clock.RestoreState(save.Clock);
        to.Sim.RestoreLayout(save.Layout!);
        to.Sim.RestorePeople(save.People!);

        var now = to.Sim.Now;
        var key = StatKeys.Money(MoneyCategory.Staff);
        Assert.Equal(h.Sim.History.Total(StatsPeriod.Last24Hours, now, key), to.Sim.History.Total(StatsPeriod.Last24Hours, now, key));
        Assert.Equal(h.Sim.History.Since, to.Sim.History.Since);
    }
}
