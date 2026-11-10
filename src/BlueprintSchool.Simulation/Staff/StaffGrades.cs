namespace BlueprintSchool.Simulation.Staff;

/// <summary>
/// Grades and pay. Teachers are early career, main pay range or upper pay range by how good they are.
/// Other roles have one grade each. Salaries are a rough guide to English state-school pay.
/// </summary>
public static class StaffGrades
{
    public sealed record Grade(StaffRole Role, string Title, double MinSkill, double MaxSkill, int MinYears, int MaxYears, int Salary, double Weight);

    /// <summary>Every grade, lowest first within each role. Weight is how often a candidate of that grade applies.</summary>
    public static readonly IReadOnlyList<Grade> All = new[]
    {
        new Grade(StaffRole.Teacher, "Early career teacher", 0.40, 0.55, 0, 1, 32_900, 3),
        new Grade(StaffRole.Teacher, "Teacher", 0.55, 0.75, 2, 8, 39_000, 4),
        new Grade(StaffRole.Teacher, "Experienced teacher", 0.75, 0.95, 8, 25, 48_000, 2),
        new Grade(StaffRole.TeachingAssistant, "Teaching assistant", 0.35, 0.65, 0, 15, 24_000, 2.5),
        new Grade(StaffRole.Receptionist, "Receptionist", 0.40, 0.60, 0, 15, 24_000, 1.5),
        new Grade(StaffRole.SiteStaff, "Site officer", 0.40, 0.70, 0, 20, 26_000, 1.5),
    };

    public static IEnumerable<Grade> For(StaffRole role) => All.Where(g => g.Role == role);

    /// <summary>The grade that fits someone in <paramref name="role"/> with this skill.</summary>
    public static Grade Of(StaffRole role, double skill)
    {
        var grades = For(role).ToList();
        return grades.LastOrDefault(g => skill >= g.MinSkill) ?? grades[0];
    }

    /// <summary>The role in plain words, e.g. "teacher" or "receptionist".</summary>
    public static string RoleName(StaffRole role, bool plural = false) => (role, plural) switch
    {
        (StaffRole.Teacher, false) => "teacher",
        (StaffRole.Teacher, true) => "teachers",
        (StaffRole.TeachingAssistant, false) => "teaching assistant",
        (StaffRole.TeachingAssistant, true) => "teaching assistants",
        (StaffRole.Receptionist, false) => "receptionist",
        (StaffRole.Receptionist, true) => "receptionists",
        (StaffRole.SiteStaff, false) => "site officer",
        _ => "site officers",
    };
}

/// <summary>Someone applying for a job. A fresh list arrives every few hours.</summary>
public sealed class JobCandidate
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required StaffRole Role { get; init; }

    public required string Grade { get; init; }

    public required double Skill { get; init; }

    public required double Speed { get; init; }

    public required int YearsExperience { get; init; }

    public required int Age { get; init; }

    /// <summary>Annual salary in pounds.</summary>
    public required int Salary { get; init; }
}
