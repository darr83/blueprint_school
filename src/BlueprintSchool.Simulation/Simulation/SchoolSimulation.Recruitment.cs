using BlueprintSchool.Simulation.Core;
using BlueprintSchool.Simulation.Hospital;
using BlueprintSchool.Simulation.Staff;

namespace BlueprintSchool.Simulation.Simulation;

/// <summary>Where a member of staff can be based: the whole school (no room) or a room.</summary>
public sealed record StaffPosting(string? RoomId, string Name);

/// <summary>How many of one kind of staff a place needs to run well, and how many it has.</summary>
public sealed record StaffingNeed(string Place, string? RoomId, StaffRole Role, int Recommended, int Have, StaffShift Shift = StaffShift.Day)
{
    public int Short => Math.Max(0, Recommended - Have);
}

/// <summary>
/// Hiring and letting staff go. The school starts with nobody: the player hires everyone from a list of
/// candidates that is refreshed every few hours. A recommended headcount is a guide only. For now everyone
/// works across the whole school; teaching posts by subject and department come with the staffing milestone.
/// </summary>
public sealed partial class SchoolSimulation
{
    /// <summary>A fresh list of candidates arrives every this many game hours (00:00, 04:00, 08:00 ...).</summary>
    public const int CandidatesEveryHours = 4;

    /// <summary>Extra candidates on top of one of each kind of staff, for the kinds the school is short of.</summary>
    public const int ExtraCandidatesForShortages = 4;

    /// <summary>Every kind of staff that can be recruited. Each list of candidates has at least one of each.</summary>
    public static IReadOnlyList<StaffRole> RecruitableRoles { get; } =
        Enum.GetValues<StaffRole>().Where(r => StaffGrades.For(r).Any()).ToArray();

    private readonly List<JobCandidate> _candidates = new();
    private int _nextCandidateId = 1;
    private DateTime? _candidatesDate;
    private GameRandom? _recruitRandom;

    /// <summary>Candidates have their own dice, so a new list every few hours doesn't change anything else.</summary>
    private GameRandom RecruitRandom => _recruitRandom ??= new GameRandom(_options.Seed + 104_729);

    public IReadOnlyList<JobCandidate> Candidates => _candidates;

    /// <summary>When the current list of candidates arrived.</summary>
    public DateTime? CandidatesDate => _candidatesDate;

    /// <summary>Whether someone works in the room.</summary>
    public bool IsStaffed(CareSpace room) => _staff.Any(s => s.BaseRoomId == room.Id);

    /// <summary>Where someone in this role could be based. For now, everyone works across the whole school.</summary>
    public IReadOnlyList<StaffPosting> PostingsFor(StaffRole role) => new[] { new StaffPosting(null, "Whole school") };

    /// <summary>A guide to the staff the school needs. The player is free to ignore it.</summary>
    public IReadOnlyList<StaffingNeed> RecommendedStaffing()
    {
        int Have(StaffRole role) => _staff.Count(s => s.Role == role);

        return new List<StaffingNeed>
        {
            new("Whole school", null, StaffRole.Receptionist, 1, Have(StaffRole.Receptionist)),
            new("Whole school", null, StaffRole.SiteStaff, 1, Have(StaffRole.SiteStaff)),
        };
    }

    // ---------------------------------------------------------------- candidates

    /// <summary>
    /// A fresh list of candidates arrives every <see cref="CandidatesEveryHours"/> game hours; anyone not
    /// hired by then has gone elsewhere.
    /// </summary>
    private void RefreshCandidatesIfDue()
    {
        var slot = Now.Date.AddHours(Now.Hour / CandidatesEveryHours * CandidatesEveryHours);
        if (_candidatesDate is { } date && date >= slot)
        {
            return;
        }

        RefreshCandidates(slot);
        if (Now == slot)
        {
            Notifications.Add(Now, NotificationLevel.Info, $"{_candidates.Count} new job candidates are waiting in Recruitment.");
        }
    }

    private void RefreshCandidates(DateTime arrived)
    {
        _candidates.Clear();
        _candidatesDate = arrived;

        // One of every kind of staff, whether or not there is a room for them yet.
        foreach (var role in RecruitableRoles)
        {
            _candidates.Add(MakeCandidate(PickGrade(StaffGrades.For(role).ToList())));
        }

        // Then a second choice for the kinds of staff the hospital is short of.
        var short_ = RecommendedStaffing().Where(n => n.Short > 0).Select(n => n.Role).Distinct();
        foreach (var role in short_.Take(ExtraCandidatesForShortages))
        {
            _candidates.Add(MakeCandidate(PickGrade(StaffGrades.For(role).ToList())));
        }
    }

    private StaffGrades.Grade PickGrade(IReadOnlyList<StaffGrades.Grade> grades)
    {
        var roll = RecruitRandom.Range(0.0, grades.Sum(g => g.Weight));
        foreach (var grade in grades)
        {
            roll -= grade.Weight;
            if (roll <= 0)
            {
                return grade;
            }
        }

        return grades[^1];
    }

    private JobCandidate MakeCandidate(StaffGrades.Grade grade)
    {
        var (first, surname) = PickName();
        var skill = Math.Round(RecruitRandom.Range(grade.MinSkill, grade.MaxSkill), 2);
        var years = RecruitRandom.Range(grade.MinYears, grade.MaxYears + 1);
        var speed = Math.Round(RecruitRandom.Range(0.85, 1.15), 2);

        // Better people ask for a little more.
        var range = Math.Max(0.01, grade.MaxSkill - grade.MinSkill);
        var salary = (int)(Math.Round(grade.Salary * (0.95 + (0.1 * (skill - grade.MinSkill) / range)) / 100) * 100);
        return new JobCandidate
        {
            Id = _nextCandidateId++,
            Name = $"{first} {surname}",
            Role = grade.Role,
            Grade = grade.Title,
            Skill = skill,
            Speed = speed,
            YearsExperience = years,
            Age = 22 + years + RecruitRandom.Range(0, 8),
            Salary = salary,
        };
    }

    private (string First, string Surname) PickName()
    {
        var names = Database.Names.Values.First();
        var female = RecruitRandom.Chance(0.6);
        return (RecruitRandom.Pick(female ? names.FemaleFirstNames : names.MaleFirstNames), RecruitRandom.Pick(names.Surnames));
    }

    // ---------------------------------------------------------------- hiring and letting go

    /// <summary>
    /// Takes on a candidate, based at <paramref name="roomId"/> (null for the whole school), on
    /// <paramref name="shift"/> (by default whichever shift has fewer of their kind there).
    /// </summary>
    public StaffMember? Hire(int candidateId, string? roomId, StaffShift? shift = null)
    {
        if (_candidates.FirstOrDefault(c => c.Id == candidateId) is not { } candidate)
        {
            return null;
        }

        if (!PostingsFor(candidate.Role).Any(p => p.RoomId == roomId))
        {
            roomId = PostingsFor(candidate.Role)[0].RoomId;
        }

        _candidates.Remove(candidate);
        var member = new StaffMember
        {
            Id = _nextStaffId++,
            Name = candidate.Name,
            Role = candidate.Role,
            Grade = candidate.Grade,
            DepartmentId = SchoolDepartmentId,
            BaseRoomId = roomId,
            Skill = candidate.Skill,
            Speed = candidate.Speed,
            YearsExperience = candidate.YearsExperience,
            Age = candidate.Age,
            Salary = candidate.Salary,
            Fatigue = Random.Range(0.0, 0.1),
            Shift = shift ?? StaffShift.Day,
        };
        Join(member);
        Tally(StatKeys.Hired);
        var place = roomId is { } id && FindSpace(id) is { } room ? room.Name : PostingsFor(member.Role)[0].Name;
        var starts = member.Shift == CurrentShift ? "starting now" : $"starting at {NextStart(member.Shift):HH:mm}";
        Notifications.Add(Now, NotificationLevel.Info, $"{member.Name} ({member.Grade}) has joined the {ShiftName(member.Shift)} team: {place}, {starts}.");
        return member;
    }

    /// <summary>Takes on a new member of staff straight away, without a candidate (starting teams, scenarios and tests).</summary>
    public StaffMember HireStaff(StaffRole role, string? roomId = null, double? skill = null, StaffShift? shift = null)
    {
        var grades = StaffGrades.For(role).ToList();
        var grade = skill is { } s ? StaffGrades.Of(role, s) : grades[0];
        var candidate = MakeCandidate(grade);
        var member = new StaffMember
        {
            Id = _nextStaffId++,
            Name = candidate.Name,
            Role = role,
            Grade = grade.Title,
            DepartmentId = SchoolDepartmentId,
            BaseRoomId = roomId,
            Skill = skill ?? candidate.Skill,
            Speed = 1.0,
            YearsExperience = candidate.YearsExperience,
            Age = candidate.Age,
            Salary = grade.Salary,
            Fatigue = Random.Range(0.0, 0.15),
            Shift = shift ?? CurrentShift,
        };
        Join(member);
        return member;
    }

    private void Join(StaffMember member)
    {
        _staff.Add(member);
        member.PlaceAt(HomeTile(member));
        if (member.Shift == CurrentShift)
        {
            PlanBreaks(member, CurrentShiftStart);
        }
        else
        {
            member.Activity = StaffActivity.OffDuty;
            member.StatusText = OffDutyText(member);
        }
    }

    /// <summary>Lets a member of staff go.</summary>
    public bool Fire(int staffId)
    {
        if (FindStaff(staffId) is not { } member)
        {
            return false;
        }

        _staff.Remove(member);
        Tally(StatKeys.Left);
        Notifications.Add(Now, NotificationLevel.Info, $"{member.Name} ({member.Grade}) has left the school.");
        return true;
    }

    // ---------------------------------------------------------------- rooms coming and going

    /// <summary>Keeps everyone based somewhere that exists: when a room is demolished its staff work across the whole school.</summary>
    private void SyncRoomStaff()
    {
        foreach (var member in _staff.Where(s => s.BaseRoomId is { } id && FindSpace(id) is null))
        {
            member.BaseRoomId = null;
        }
    }
}
