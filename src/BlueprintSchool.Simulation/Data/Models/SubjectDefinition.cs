namespace BlueprintSchool.Simulation.Data.Models;

/// <summary>
/// A subject on the curriculum, e.g. Maths or French. Loaded from Data/Subjects. Each subject belongs to a
/// teaching department, has its own colour (door signs, timetable cells, build bar) and may need a
/// specialist room: Science in a lab, PE in the sports hall. Any subject can be taught in a general classroom,
/// but a practical subject taught out of its room makes a weaker lesson.
/// </summary>
public sealed class SubjectDefinition : IDataRecord
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    /// <summary>Two or three letters for timetable cells and badges, e.g. "Ma" or "DT".</summary>
    public string ShortName { get; init; } = string.Empty;

    public string Department { get; init; } = string.Empty;

    /// <summary>The subject's colour, as #RRGGBB.</summary>
    public string Colour { get; init; } = string.Empty;

    /// <summary>English, Maths and Science: every pupil takes them all the way to GCSE.</summary>
    public bool Core { get; init; }

    /// <summary>Room templates made for the subject, best first. Empty if a general classroom is all it needs.</summary>
    public IReadOnlyList<string> Rooms { get; init; } = Array.Empty<string>();

    /// <summary>True if lessons out of one of <see cref="Rooms"/> are weaker (practical subjects).</summary>
    public bool NeedsSpecialistRoom { get; init; }

    /// <summary>Lessons a week in Key Stage 3 (Years 7 to 9), out of the 25 in a week. Zero if taught on a carousel with another subject.</summary>
    public int Ks3PeriodsPerWeek { get; init; }

    public void Validate(ValidationContext context)
    {
        context.RequireId("id", Id);
        context.RequireText("name", Name);
        context.RequireText("short_name", ShortName);
        if (ShortName.Length > 3)
        {
            context.Error("short_name", "must be at most 3 letters.");
        }

        context.RequireId("department", Department);
        context.RequireHexColour("colour", Colour);
        context.RequireIds("rooms", Rooms);
        context.RequireRange("ks3_periods_per_week", Ks3PeriodsPerWeek, 0, 10);
        if (NeedsSpecialistRoom && Rooms.Count == 0)
        {
            context.Error("rooms", "must list at least one room when needs_specialist_room is true.");
        }
    }
}
