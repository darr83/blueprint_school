using BlueprintSchool.Simulation.Core;
using BlueprintSchool.Simulation.Simulation;
using BlueprintSchool.Simulation.Staff;

namespace BlueprintSchool.Simulation.Tests;

public class ShiftTests
{
    [Theory]
    [InlineData(10, StaffShift.Day)]
    [InlineData(8, StaffShift.Day)]
    [InlineData(19, StaffShift.Day)]
    [InlineData(20, StaffShift.Night)]
    [InlineData(2, StaffShift.Night)]
    [InlineData(7, StaffShift.Night)]
    public void Shifts_run_eight_to_eight(int hour, StaffShift expected)
    {
        var time = new DateTime(2027, 1, 4, hour, 30, 0);
        Assert.Equal(expected, SchoolSimulation.ShiftAt(time));
        var start = SchoolSimulation.ShiftStart(time);
        Assert.True(start <= time && time < start.AddHours(12));
        Assert.Equal(expected == StaffShift.Day ? 8 : 20, start.Hour);
    }

    /// <summary>A team of teachers on days, at work from mid-morning.</summary>
    private static SimulationHarness WithTeam(int count, int seed = 5, DateTime? start = null)
    {
        var h = new SimulationHarness(seed: seed, start: start);
        for (var i = 0; i < count; i++)
        {
            h.Sim.HireStaff(StaffRole.Teacher, shift: StaffShift.Day);
        }

        return h;
    }

    [Fact]
    public void At_eight_in_the_evening_the_day_team_goes_home()
    {
        var h = WithTeam(3, start: new DateTime(2027, 1, 4, 18, 0, 0));
        h.RunFor(TimeSpan.FromHours(2) + TimeSpan.FromMinutes(1));
        h.RunUntil(() => h.Sim.Staff.All(s => s.Activity == StaffActivity.OffDuty), TimeSpan.FromHours(1), "the day team to leave");
        Assert.All(h.Sim.Staff, s => Assert.Contains("in at 08:00", s.StatusText));

        h.RunUntil(() => h.Sim.Staff.All(s => s.IsOnDuty), TimeSpan.FromHours(13), "the day team to come back in the morning");
        Assert.Contains(h.Sim.Notifications.Recent, n => n.Text.StartsWith("Day shift has started"));
    }

    [Fact]
    public void Everyone_gets_a_meal_break_and_two_tea_breaks_away_from_the_start_and_end_of_the_shift()
    {
        var h = WithTeam(4, start: new DateTime(2027, 1, 4, 7, 0, 0));
        h.RunFor(TimeSpan.FromHours(1) + TimeSpan.FromMinutes(1));
        var shiftStart = new DateTime(2027, 1, 4, 8, 0, 0);

        Assert.Equal(4, h.Sim.OnDutyStaff.Count());
        foreach (var member in h.Sim.OnDutyStaff)
        {
            Assert.Equal(3, member.Breaks.Count);
            Assert.Equal(1, member.Breaks.Count(b => b.Minutes == SchoolSimulation.MealBreakMinutes));
            Assert.Equal(2, member.Breaks.Count(b => b.Minutes == SchoolSimulation.TeaBreakMinutes));
            Assert.All(member.Breaks, b => Assert.InRange(b.Start, shiftStart.AddHours(1), shiftStart.AddHours(11)));
        }
    }

    [Fact]
    public void Staff_take_their_breaks_and_come_back_afterwards()
    {
        var h = WithTeam(4, seed: 6, start: new DateTime(2027, 1, 4, 7, 59, 0));
        h.RunUntil(() => h.Clock.Now.Hour >= 19, TimeSpan.FromHours(12), "the end of the day");
        Assert.All(h.Sim.Staff, s => Assert.True(s.Breaks.Count(b => b.Done) >= 2, $"{s.Name} only had {s.Breaks.Count(b => b.Done)} breaks"));
    }

    [Fact]
    public void No_more_than_a_third_of_a_team_go_on_a_planned_break_at_once()
    {
        var h = WithTeam(4, seed: 2);
        var team = h.Sim.OnDutyStaff.ToList();
        foreach (var member in team)
        {
            member.Breaks.Clear();
            member.Breaks.Add(new PlannedBreak { Start = h.Clock.Now, Minutes = 30 });
        }

        h.RunFor(TimeSpan.FromMinutes(5));
        Assert.Equal(1, team.Count(n => n.IsOnBreak));

        // The others go in turn.
        h.RunFor(TimeSpan.FromMinutes(40));
        Assert.Equal(1, team.Count(n => n.Breaks[0].Done));
        Assert.Equal(1, team.Count(n => n.IsOnBreak));
    }

    [Fact]
    public void The_player_can_send_someone_on_a_break()
    {
        var h = WithTeam(1);
        var member = h.Sim.Staff.Single();
        Assert.True(h.Sim.SendOnBreak(member.Id));
        Assert.False(h.Sim.SendOnBreak(member.Id));
        h.RunFor(TimeSpan.FromMinutes(1));
        Assert.True(member.IsOnBreak);
        h.RunFor(TimeSpan.FromMinutes(30));
        Assert.False(member.IsOnBreak);
        Assert.True(member.Breaks.Single(b => b.Requested).Done);
    }

    [Fact]
    public void Moving_someone_to_the_night_team_sends_them_home_and_back_at_eight()
    {
        var h = WithTeam(1, start: new DateTime(2027, 1, 4, 15, 0, 0));
        var member = h.Sim.Staff.Single();
        Assert.True(h.Sim.SetShift(member.Id, StaffShift.Night));

        h.RunUntil(() => member.Activity == StaffActivity.OffDuty, TimeSpan.FromHours(1), "them to go home");
        Assert.Contains("in at 20:00", member.StatusText);
        h.RunFor(TimeSpan.FromHours(5));
        Assert.True(member.IsOnDuty);

        // In the morning they go home; moved back to days, they come straight in again.
        h.RunUntil(() => member.Activity == StaffActivity.OffDuty, TimeSpan.FromHours(14), "them to go home after the night");
        Assert.True(h.Sim.SetShift(member.Id, StaffShift.Day));
        h.RunFor(TimeSpan.FromMinutes(1));
        Assert.True(member.IsOnDuty);
    }

    [Fact]
    public void A_hire_costs_their_salary_spread_over_the_year()
    {
        var h = WithTeam(3);
        var salaries = h.Sim.Staff.Sum(s => s.Salary);
        Assert.Equal((long)Math.Round(salaries / 365.0), h.Sim.StaffCostPerDay);
    }

    [Fact]
    public void Shifts_and_breaks_survive_save_and_load()
    {
        var h = WithTeam(4, seed: 8);
        h.RunFor(TimeSpan.FromHours(3));
        var json = SaveSerializer.Serialize(new SaveGame
        {
            Clock = h.Clock.CaptureState(),
            Layout = h.Sim.CaptureLayout(),
            People = h.Sim.CapturePeople(),
        });

        var save = SaveSerializer.Deserialize(json);
        var loaded = new SimulationHarness(seed: 99);
        loaded.Clock.RestoreState(save.Clock);
        loaded.Sim.RestoreLayout(save.Layout!);
        loaded.Sim.RestorePeople(save.People!);

        foreach (var member in h.Sim.Staff)
        {
            var copy = loaded.Sim.FindStaff(member.Id)!;
            Assert.Equal(member.Shift, copy.Shift);
            Assert.Equal(member.Activity, copy.Activity);
            Assert.Equal(member.Breaks.Select(b => (b.Start, b.Minutes, b.Done)), copy.Breaks.Select(b => (b.Start, b.Minutes, b.Done)));
        }
    }
}
