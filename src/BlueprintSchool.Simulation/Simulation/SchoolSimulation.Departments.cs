using BlueprintSchool.Simulation.Hospital;

namespace BlueprintSchool.Simulation.Simulation;

/// <summary>
/// Departments. The school department and the shared services are open from the start; any other
/// department is unlocked by paying its opening cost, which lets its rooms be built.
/// </summary>
public sealed partial class SchoolSimulation
{
    /// <summary>The department every new school starts with.</summary>
    public const string SchoolDepartmentId = "school";

    private readonly HashSet<string> _unlockedDepartments = new();

    public IReadOnlyCollection<string> UnlockedDepartments => _unlockedDepartments;

    /// <summary>Whether a department's rooms can be built.</summary>
    public bool IsUnlocked(string departmentId) => _unlockedDepartments.Contains(departmentId);

    /// <summary>Departments open from the start: the school, any department with no opening cost, and the shared services.</summary>
    private void UnlockStartingDepartments()
    {
        foreach (var d in Database.Departments.Values.Where(d => d.StartsUnlocked || d.IsSharedService || d.OpeningCost == 0))
        {
            _unlockedDepartments.Add(d.Id);
        }
    }

    /// <summary>Whether a department can be unlocked now, and what it costs.</summary>
    public BuildCheck CheckUnlock(string departmentId)
    {
        if (!Database.Departments.TryGetValue(departmentId, out var department))
        {
            return new BuildCheck(0, new[] { $"Unknown department '{departmentId}'." });
        }

        var problems = new List<string>();
        if (IsUnlocked(departmentId))
        {
            problems.Add($"{department.Name} is already unlocked.");
        }
        else if (!CanAfford(department.OpeningCost))
        {
            problems.Add($"Not enough money: costs £{department.OpeningCost:N0}, you have £{Balance:N0}.");
        }

        return new BuildCheck(department.OpeningCost, problems);
    }

    /// <summary>Pays a department's opening cost so its rooms can be built.</summary>
    public BuildCheck UnlockDepartment(string departmentId)
    {
        var check = CheckUnlock(departmentId);
        if (!check.CanBuild)
        {
            return check;
        }

        var department = Database.Departments[departmentId];
        _unlockedDepartments.Add(departmentId);
        Book(MoneyCategory.Construction, -check.Cost);
        Notifications.Add(Now, NotificationLevel.Info, $"Unlocked {department.Name} for £{check.Cost:N0}. Its rooms can now be built.");
        return check;
    }

    public string DepartmentName(string departmentId) =>
        Database.Departments.TryGetValue(departmentId, out var d) ? d.Name : departmentId;

    // ---------------------------------------------------------------- names

    /// <summary>Renames a member of staff.</summary>
    public void RenameStaff(int staffId, string name)
    {
        if (FindStaff(staffId) is { } member && !string.IsNullOrWhiteSpace(name))
        {
            member.Name = name.Trim();
        }
    }
}
