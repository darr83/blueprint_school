using BlueprintSchool.Simulation.Simulation;
using BlueprintSchool.Simulation.Staff;
using BlueprintSchool.Simulation.World;

namespace BlueprintSchool.Simulation.Tests;

public class FinanceTests
{
    [Fact]
    public void Wages_and_upkeep_are_charged_through_the_day()
    {
        var h = new SimulationHarness();
        h.Sim.HireStaff(StaffRole.Teacher);
        h.Sim.HireStaff(StaffRole.Receptionist);
        var start = h.Sim.Balance;
        var perDay = h.Sim.StaffCostPerDay + h.Sim.UpkeepPerDay;
        Assert.True(perDay > 100, $"Running costs look too low: £{perDay}/day");

        h.RunFor(TimeSpan.FromHours(12));
        var spent = start - h.Sim.Balance;
        Assert.InRange(spent, (perDay / 2) - 30, (perDay / 2) + 30);
        Assert.True(h.Sim.Today[MoneyCategory.Staff] < 0);
        Assert.True(h.Sim.Today[MoneyCategory.Upkeep] < 0);
    }

    [Fact]
    public void Building_is_booked_as_construction()
    {
        var h = new SimulationHarness();
        var b = StarterSite.MainBuildingFootprint;
        var check = h.Sim.ExtendBuilding(new GridRect(b.X - 10, b.Y, 10, 20));
        Assert.True(check.CanBuild);
        Assert.Equal(-check.Cost, h.Sim.Today[MoneyCategory.Construction]);
    }
}
