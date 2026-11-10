using BlueprintSchool.Simulation.Data.Models;
using BlueprintSchool.Simulation.Rooms;

namespace BlueprintSchool.Simulation.Data;

/// <summary>
/// All data-driven game content, loaded and validated once at startup.
/// New content types are added here as each milestone introduces them.
/// </summary>
public sealed class GameDatabase
{
    public const string DepartmentsFolder = "Departments";
    public const string NamesFolder = "Names";
    public const string FurnitureFolder = "Furniture";
    public const string RoomTemplatesFolder = "RoomTemplates";
    public const string SubjectsFolder = "Subjects";
    public const string BellScheduleFolder = "BellSchedule";

    private GameDatabase()
    {
    }

    public IReadOnlyDictionary<string, DepartmentDefinition> Departments { get; private init; } = null!;

    public IReadOnlyDictionary<string, NamePool> Names { get; private init; } = null!;

    public IReadOnlyDictionary<string, FurnitureDefinition> Furniture { get; private init; } = null!;

    public IReadOnlyDictionary<string, RoomTemplate> RoomTemplates { get; private init; } = null!;

    public IReadOnlyDictionary<string, SubjectDefinition> Subjects { get; private init; } = null!;

    /// <summary>The parts of the school day, by id. <see cref="Bells"/> puts them in order.</summary>
    public IReadOnlyDictionary<string, BellPeriodDefinition> BellPeriods { get; private init; } = null!;

    /// <summary>The school day, period by period.</summary>
    public SchoolDay.BellSchedule Bells => _bells ??= new SchoolDay.BellSchedule(BellPeriods.Values);

    private SchoolDay.BellSchedule? _bells;

    /// <summary>The department every new school starts with.</summary>
    public DepartmentDefinition StartingDepartment => Departments.Values.Single(d => d.StartsUnlocked);

    /// <summary>Loads every data folder. Throws <see cref="DataLoadException"/> listing every problem if anything is invalid.</summary>
    public static GameDatabase Load(IDataSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var errors = new List<DataValidationError>();

        var db = new GameDatabase
        {
            Departments = RecordLoader.LoadFolder<DepartmentDefinition>(source, DepartmentsFolder, errors),
            Names = RecordLoader.LoadFolder<NamePool>(source, NamesFolder, errors),
            Furniture = RecordLoader.LoadFolder<FurnitureDefinition>(source, FurnitureFolder, errors),
            RoomTemplates = RecordLoader.LoadFolder<RoomTemplate>(source, RoomTemplatesFolder, errors),
            Subjects = RecordLoader.LoadFolder<SubjectDefinition>(source, SubjectsFolder, errors),
            BellPeriods = RecordLoader.LoadFolder<BellPeriodDefinition>(source, BellScheduleFolder, errors),
        };

        db.CheckDatabaseRules(errors);

        if (errors.Count > 0)
        {
            throw new DataLoadException(errors);
        }

        return db;
    }

    private void CheckDatabaseRules(List<DataValidationError> errors)
    {
        // Skip if any department failed to load: the record that was rejected may be the starting one.
        if (!errors.Any(e => e.File.StartsWith(DepartmentsFolder + "/", StringComparison.Ordinal)))
        {
            var starting = Departments.Values.Where(d => d.StartsUnlocked).Select(d => d.Id).ToArray();
            if (starting.Length != 1)
            {
                errors.Add(new DataValidationError(
                    DepartmentsFolder,
                    null,
                    "starts_unlocked",
                    starting.Length == 0
                        ? "exactly one department must have starts_unlocked set to true; none do."
                        : $"exactly one department must have starts_unlocked set to true; found {string.Join(", ", starting)}."));
            }
        }

        foreach (var template in RoomTemplates.Values)
        {
            var file = $"{RoomTemplatesFolder}/";
            if (!Departments.ContainsKey(template.Department))
            {
                errors.Add(new DataValidationError(file, template.Id, "department", $"refers to unknown department '{template.Department}'."));
            }

            foreach (var item in template.Furniture.Where(f => !Furniture.ContainsKey(f)))
            {
                errors.Add(new DataValidationError(file, template.Id, "furniture", $"refers to unknown furniture '{item}'."));
            }

            foreach (var item in Rooms.RoomLayoutEngine.RequiredFurniture(template.Layout).Where(f => !template.Furniture.Contains(f)))
            {
                errors.Add(new DataValidationError(file, template.Id, "furniture", $"must include '{item}', which the {template.Layout} layout needs."));
            }
        }

        foreach (var subject in Subjects.Values)
        {
            var file = $"{SubjectsFolder}/";
            if (!Departments.ContainsKey(subject.Department))
            {
                errors.Add(new DataValidationError(file, subject.Id, "department", $"refers to unknown department '{subject.Department}'."));
            }

            foreach (var room in subject.Rooms.Where(r => !RoomTemplates.ContainsKey(r)))
            {
                errors.Add(new DataValidationError(file, subject.Id, "rooms", $"refers to unknown room '{room}'."));
            }
        }

        if (!errors.Any(e => e.File.StartsWith(BellScheduleFolder + "/", StringComparison.Ordinal)))
        {
            foreach (var problem in Bells.Problems())
            {
                errors.Add(new DataValidationError(BellScheduleFolder, null, "(schedule)", problem));
            }
        }
    }

    /// <summary>Creates a database directly from records. For tests.</summary>
    public static GameDatabase FromRecords(
        IEnumerable<DepartmentDefinition>? departments = null,
        IEnumerable<NamePool>? names = null,
        IEnumerable<FurnitureDefinition>? furniture = null,
        IEnumerable<RoomTemplate>? roomTemplates = null,
        IEnumerable<SubjectDefinition>? subjects = null,
        IEnumerable<BellPeriodDefinition>? bellPeriods = null) => new()
    {
        Departments = (departments ?? Array.Empty<DepartmentDefinition>()).ToDictionary(r => r.Id),
        Names = (names ?? Array.Empty<NamePool>()).ToDictionary(r => r.Id),
        Furniture = (furniture ?? Array.Empty<FurnitureDefinition>()).ToDictionary(r => r.Id),
        RoomTemplates = (roomTemplates ?? Array.Empty<RoomTemplate>()).ToDictionary(r => r.Id),
        Subjects = (subjects ?? Array.Empty<SubjectDefinition>()).ToDictionary(r => r.Id),
        BellPeriods = (bellPeriods ?? Array.Empty<BellPeriodDefinition>()).ToDictionary(r => r.Id),
    };
}
