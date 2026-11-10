using BlueprintSchool.Simulation.Simulation;
using BlueprintSchool.Simulation.Staff;

namespace BlueprintSchool.Simulation.Tests;

/// <summary>Grades, the candidates, hiring and letting staff go.</summary>
public class RecruitmentTests
{
    [Theory]
    [InlineData(StaffRole.Teacher, 0.45, "Early career teacher")]
    [InlineData(StaffRole.Teacher, 0.65, "Teacher")]
    [InlineData(StaffRole.Teacher, 0.85, "Experienced teacher")]
    [InlineData(StaffRole.TeachingAssistant, 0.5, "Teaching assistant")]
    [InlineData(StaffRole.Receptionist, 0.5, "Receptionist")]
    [InlineData(StaffRole.SiteStaff, 0.5, "Site officer")]
    public void Grades_follow_how_good_someone_is(StaffRole role, double skill, string grade)
    {
        Assert.Equal(grade, StaffGrades.Of(role, skill).Title);
    }

    [Fact]
    public void A_new_school_has_no_staff()
    {
        var h = new SimulationHarness();
        Assert.Empty(h.Sim.Staff);
    }

    [Fact]
    public void New_candidates_arrive_every_few_hours()
    {
        var h = new SimulationHarness(start: new DateTime(2027, 1, 4, 8, 0, 0));
        Assert.NotEmpty(h.Sim.Candidates);
        var first = h.Sim.Candidates.Select(c => c.Id).ToHashSet();

        h.RunFor(TimeSpan.FromHours(SchoolSimulation.CandidatesEveryHours - 0.5));
        Assert.Equal(first, h.Sim.Candidates.Select(c => c.Id).ToHashSet());

        h.RunFor(TimeSpan.FromHours(1));
        Assert.NotEmpty(h.Sim.Candidates);
        Assert.DoesNotContain(h.Sim.Candidates, c => first.Contains(c.Id));
    }

    [Fact]
    public void Every_kind_of_staff_applies_each_time()
    {
        var h = new SimulationHarness();
        Assert.Equal(Enum.GetValues<StaffRole>().Length, SchoolSimulation.RecruitableRoles.Count);
        for (var batch = 0; batch < 3; batch++)
        {
            var roles = h.Sim.Candidates.Select(c => c.Role).ToHashSet();
            Assert.All(SchoolSimulation.RecruitableRoles, r => Assert.Contains(r, roles));
            h.RunFor(TimeSpan.FromHours(SchoolSimulation.CandidatesEveryHours));
        }
    }

    [Fact]
    public void Hiring_and_letting_go()
    {
        var h = new SimulationHarness();
        var candidate = h.Sim.Candidates.First(c => c.Role == StaffRole.Teacher);

        var member = h.Sim.Hire(candidate.Id, null);
        Assert.NotNull(member);
        Assert.Equal(candidate.Name, member!.Name);
        Assert.Equal(candidate.Salary, member.Salary);
        Assert.Equal(StaffShift.Day, member.Shift);
        Assert.Single(h.Sim.Staff);
        Assert.DoesNotContain(h.Sim.Candidates, c => c.Id == candidate.Id);
        Assert.Null(h.Sim.Hire(candidate.Id, null));

        Assert.True(h.Sim.Fire(member.Id));
        Assert.Empty(h.Sim.Staff);
        Assert.False(h.Sim.Fire(member.Id));
    }

    [Fact]
    public void Recommended_staffing_asks_for_a_receptionist_and_a_site_officer()
    {
        var h = new SimulationHarness();
        var needs = h.Sim.RecommendedStaffing();
        Assert.Contains(needs, n => n.Role == StaffRole.Receptionist && n.Short == 1);
        Assert.Contains(needs, n => n.Role == StaffRole.SiteStaff && n.Short == 1);

        h.StaffToRecommended();
        Assert.All(h.Sim.RecommendedStaffing(), n => Assert.Equal(0, n.Short));
    }
}
