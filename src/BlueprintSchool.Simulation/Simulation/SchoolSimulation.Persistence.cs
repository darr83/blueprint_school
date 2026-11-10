using BlueprintSchool.Simulation.Agents;
using BlueprintSchool.Simulation.Hospital;
using BlueprintSchool.Simulation.Staff;
using BlueprintSchool.Simulation.World;

namespace BlueprintSchool.Simulation.Simulation;

/// <summary>Where an agent stands and where it was walking to.</summary>
public sealed record PositionState(int X, int Y, int? ToX = null, int? ToY = null);

/// <summary>Everything about one member of staff.</summary>
public sealed class StaffState
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public StaffRole Role { get; init; }

    public string Grade { get; init; } = string.Empty;

    public string DepartmentId { get; init; } = string.Empty;

    public string? BaseRoomId { get; init; }

    public int Age { get; init; }

    public int YearsExperience { get; init; }

    public double Skill { get; init; }

    public double Speed { get; init; }

    public int Salary { get; init; }

    public double Fatigue { get; init; }

    public double Morale { get; init; }

    public StaffActivity Activity { get; init; }

    public DateTime? BreakEndsAt { get; init; }

    public string StatusText { get; init; } = string.Empty;

    public PositionState Position { get; init; } = new(0, 0);

    public StaffShift Shift { get; init; }

    public List<BreakState> Breaks { get; init; } = new();

    public bool HasPendingMove { get; init; }

    public string? PendingRoomId { get; init; }
}

public sealed class BreakState
{
    public DateTime Start { get; init; }

    public int Minutes { get; init; }

    public DateTime? TakenAt { get; init; }

    public bool Done { get; init; }

    public bool Requested { get; init; }
}

public sealed record DayAccountsState(DateTime Date, Dictionary<MoneyCategory, long> Amounts);

/// <summary>Everyone in the school and the running totals: staff, candidates, money, notifications and stats.</summary>
public sealed class PeopleState
{
    public List<StaffState> Staff { get; init; } = new();

    public List<DayAccountsState> Accounts { get; init; } = new();

    public double UnpaidWages { get; init; }

    /// <summary>The game was started with unlimited money (spending can go into the red).</summary>
    public bool UnlimitedMoney { get; init; }

    public double UnpaidUpkeep { get; init; }

    public List<Notification> Notifications { get; init; } = new();

    public int NextNotificationId { get; init; }

    public int NextStaffId { get; init; }

    public List<JobCandidate> Candidates { get; init; } = new();

    public DateTime? CandidatesDate { get; init; }

    public int NextCandidateId { get; init; }

    /// <summary>Departments paid for.</summary>
    public List<string>? UnlockedDepartments { get; init; }

    /// <summary>Each room's floor and wall finish.</summary>
    public Dictionary<string, RoomDecor>? Decor { get; init; }

    /// <summary>The look the player chose for new rooms, by room template.</summary>
    public Dictionary<string, RoomDecor>? DecorDefaults { get; init; }

    /// <summary>The Stats screen's figures.</summary>
    public StatsHistoryState? History { get; init; }
}

/// <summary>Saving and loading everyone in the school.</summary>
public sealed partial class SchoolSimulation
{
    public PeopleState CapturePeople() => new()
    {
        Staff = _staff.Select(SaveStaff).ToList(),
        Accounts = _days.Select(d => new DayAccountsState(d.Date, new Dictionary<MoneyCategory, long>(d.Amounts))).ToList(),
        UnpaidWages = _unpaidWages,
        UnlimitedMoney = UnlimitedMoney,
        UnpaidUpkeep = _unpaidUpkeep,
        Notifications = Notifications.Recent.Take(60).ToList(),
        NextNotificationId = Notifications.NextId,
        NextStaffId = _nextStaffId,
        Candidates = _candidates.ToList(),
        CandidatesDate = _candidatesDate,
        NextCandidateId = _nextCandidateId,
        UnlockedDepartments = _unlockedDepartments.OrderBy(d => d, StringComparer.Ordinal).ToList(),
        Decor = CaptureDecor(),
        DecorDefaults = _decorDefaults.OrderBy(d => d.Key, StringComparer.Ordinal).ToDictionary(d => d.Key, d => d.Value),
        History = HistoryStore.Capture(),
    };

    /// <summary>
    /// Puts back everyone from a save. Call after <see cref="RestoreLayout"/> and before the game runs.
    /// Anything that refers to a room that no longer exists is dropped.
    /// </summary>
    public void RestorePeople(PeopleState state)
    {
        _staff.Clear();
        foreach (var saved in state.Staff.Where(s => Enum.IsDefined(s.Role)))
        {
            _staff.Add(LoadStaff(saved));
        }

        _days.Clear();
        foreach (var day in state.Accounts)
        {
            var accounts = new DayAccounts { Date = day.Date };
            foreach (var (category, amount) in day.Amounts)
            {
                accounts.Add(category, amount);
            }

            _days.Add(accounts);
        }

        _unpaidWages = state.UnpaidWages;
        UnlimitedMoney = state.UnlimitedMoney;
        _unpaidUpkeep = state.UnpaidUpkeep;
        Notifications.Restore(state.Notifications, state.NextNotificationId);
        _nextStaffId = Math.Max(state.NextStaffId, _staff.Count == 0 ? 1 : _staff.Max(s => s.Id) + 1);
        _candidates.Clear();
        _candidates.AddRange(state.Candidates.Where(c => Enum.IsDefined(c.Role)));
        _candidatesDate = state.CandidatesDate;
        _nextCandidateId = Math.Max(state.NextCandidateId, _candidates.Count == 0 ? 1 : _candidates.Max(c => c.Id) + 1);
        if (state.UnlockedDepartments is { } unlocked)
        {
            _unlockedDepartments.Clear();
            _unlockedDepartments.UnionWith(unlocked.Where(Database.Departments.ContainsKey));

            // Departments that are always open.
            UnlockStartingDepartments();
        }

        RestoreDecor(state.Decor, state.DecorDefaults);
        RestoreHistory(state.History);

        // A saved game can't carry on the old random sequence, so start a new one.
        Random.Reseed(HashCode.Combine(Random.Seed, Now.Ticks));

        SyncRoomStaff();
        Resume(state.Staff.Select(s => (s.Id, s.Position)), id => FindStaff(id));
    }

    // ------------------------------------------------------------------ staff

    private static StaffState SaveStaff(StaffMember m) => new()
    {
        Id = m.Id, Name = m.Name, Role = m.Role, Grade = m.Grade, DepartmentId = m.DepartmentId, BaseRoomId = m.BaseRoomId, Age = m.Age,
        YearsExperience = m.YearsExperience, Skill = m.Skill, Speed = m.Speed, Salary = m.Salary, Fatigue = m.Fatigue, Morale = m.Morale,
        Activity = m.Activity, BreakEndsAt = m.BreakEndsAt, StatusText = m.StatusText, Position = PositionOf(m),
        Shift = m.Shift, HasPendingMove = m.HasPendingMove, PendingRoomId = m.PendingRoomId,
        Breaks = m.Breaks.Select(b => new BreakState { Start = b.Start, Minutes = b.Minutes, TakenAt = b.TakenAt, Done = b.Done, Requested = b.Requested }).ToList(),
    };

    private static StaffMember LoadStaff(StaffState s)
    {
        var m = new StaffMember
        {
            Id = s.Id, Name = s.Name, Role = s.Role, Grade = s.Grade, DepartmentId = s.DepartmentId, BaseRoomId = s.BaseRoomId, Age = s.Age,
            YearsExperience = s.YearsExperience, Skill = s.Skill, Speed = s.Speed, Salary = s.Salary,
        };
        m.Fatigue = s.Fatigue;
        m.Morale = s.Morale;
        m.Activity = s.Activity;
        m.BreakEndsAt = s.BreakEndsAt;
        m.StatusText = s.StatusText;
        m.Shift = s.Shift;
        m.HasPendingMove = s.HasPendingMove;
        m.PendingRoomId = s.PendingRoomId;
        m.Breaks.AddRange(s.Breaks.Select(b => new PlannedBreak { Start = b.Start, Minutes = b.Minutes, TakenAt = b.TakenAt, Done = b.Done, Requested = b.Requested }));
        m.PlaceAt(new GridCoord(s.Position.X, s.Position.Y));
        return m;
    }

    // ------------------------------------------------------------------ positions

    private static PositionState PositionOf(Agent agent) =>
        new(agent.Tile.X, agent.Tile.Y, agent.Destination?.X, agent.Destination?.Y);

    /// <summary>Sets walkers off again towards where they were going.</summary>
    private void Resume(IEnumerable<(int Id, PositionState Position)> saved, Func<int, Agent?> find)
    {
        foreach (var (id, position) in saved)
        {
            if (find(id) is not { } agent || position.ToX is not { } x || position.ToY is not { } y)
            {
                continue;
            }

            var path = PathFor(agent, new GridCoord(x, y));
            if (path.Count > 0)
            {
                agent.FollowPath(path);
            }
            else
            {
                agent.PlaceAt(new GridCoord(x, y));
            }
        }
    }
}
