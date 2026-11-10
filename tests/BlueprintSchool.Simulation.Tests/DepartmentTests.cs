using BlueprintSchool.Simulation.Core;
using BlueprintSchool.Simulation.Hospital;
using BlueprintSchool.Simulation.Simulation;
using BlueprintSchool.Simulation.Staff;
using BlueprintSchool.Simulation.World;

namespace BlueprintSchool.Simulation.Tests;

public class DepartmentTests
{
    private static readonly GridRect B = StarterSite.MainBuildingFootprint;

    [Fact]
    public void The_school_and_facilities_are_open_from_the_start()
    {
        var h = new SimulationHarness();
        Assert.True(h.Sim.IsUnlocked(SchoolSimulation.SchoolDepartmentId));
        Assert.True(h.Sim.IsUnlocked("facilities"));
        Assert.Equal("school", h.Database.StartingDepartment.Id);
        Assert.Contains(h.Sim.CheckUnlock("school").Problems, p => p.Contains("already unlocked"));
        Assert.Contains(h.Sim.CheckUnlock("nowhere").Problems, p => p.StartsWith("Unknown department"));
    }

    [Fact]
    public void Staff_can_be_renamed()
    {
        var h = new SimulationHarness();
        var member = h.Sim.HireStaff(StaffRole.Teacher);
        h.Sim.RenameStaff(member.Id, "  Jo Bloggs ");
        Assert.Equal("Jo Bloggs", member.Name);
        h.Sim.RenameStaff(member.Id, "   ");
        Assert.Equal("Jo Bloggs", member.Name);
    }

    [Fact]
    public void Free_staff_wait_in_the_staff_room()
    {
        var h = new SimulationHarness(mainCorridor: true);
        h.Sim.AdjustBalance(2_000_000);
        Assert.True(h.Sim.ExtendBuilding(new GridRect(B.X - 30, B.Y, 30, 34)).CanBuild);
        Assert.True(h.Sim.BuildRoom(new RoomPlan("corridor", new GridRect(B.X - 30, B.Y + 9, 34, 8), WallSide.South)).CanBuild);
        var check = h.Sim.BuildRoom(h.Sim.AutoPlan("staff_room", new GridRect(B.X - 30, B.Y + 17, 12, 10)));
        Assert.True(check.CanBuild, string.Join("; ", check.Problems));
        var teacher = h.Sim.HireStaff(StaffRole.Teacher);
        teacher.Breaks.Clear();

        h.RunFor(TimeSpan.FromMinutes(10));
        var staffRoom = h.Sim.Spaces.Single(s => s.Kind == CareSpaceKind.StaffRoom);
        Assert.True(staffRoom.Rect.Contains(teacher.Destination ?? teacher.Tile), $"{teacher.Name} is at {teacher.Tile}");
    }

    [Fact]
    public void Unlocked_departments_survive_save_and_load()
    {
        var h = new SimulationHarness();
        var json = SaveSerializer.Serialize(new SaveGame { Clock = h.Clock.CaptureState(), Layout = h.Sim.CaptureLayout(), People = h.Sim.CapturePeople() });
        var save = SaveSerializer.Deserialize(json);
        var to = new SimulationHarness(seed: 99);
        to.Clock.RestoreState(save.Clock);
        to.Sim.RestoreLayout(save.Layout!);
        to.Sim.RestorePeople(save.People!);

        Assert.Equal(h.Sim.UnlockedDepartments.OrderBy(d => d), to.Sim.UnlockedDepartments.OrderBy(d => d));
    }

    [Fact]
    public void Unlimited_money_lets_spending_go_into_the_red()
    {
        var normal = new SimulationHarness();
        normal.Sim.AdjustBalance(-normal.Sim.Balance + 1000);
        Assert.Contains(normal.Sim.ExtendBuilding(new GridRect(B.X - 30, B.Y, 30, 34)).Problems, p => p.StartsWith("Not enough money"));

        var h = new SimulationHarness();
        h.Sim.UnlimitedMoney = true;
        h.Sim.AdjustBalance(-h.Sim.Balance + 1000);
        Assert.True(h.Sim.ExtendBuilding(new GridRect(B.X - 30, B.Y, 30, 34)).CanBuild);
        Assert.True(h.Sim.Balance < 0);
        Assert.DoesNotContain(h.Sim.Notifications.Recent, n => n.Text.Contains("overdrawn"));

        var json = SaveSerializer.Serialize(new SaveGame { Clock = h.Clock.CaptureState(), Layout = h.Sim.CaptureLayout(), People = h.Sim.CapturePeople() });
        var save = SaveSerializer.Deserialize(json);
        var to = new SimulationHarness(seed: 99);
        to.Sim.RestoreLayout(save.Layout!);
        to.Sim.RestorePeople(save.People!);
        Assert.True(to.Sim.UnlimitedMoney);
        Assert.Equal(h.Sim.Balance, to.Sim.Balance);
    }
}
