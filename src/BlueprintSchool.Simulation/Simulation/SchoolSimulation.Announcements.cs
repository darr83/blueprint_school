using BlueprintSchool.Simulation.Core;

namespace BlueprintSchool.Simulation.Simulation;

public enum AnnouncementKind
{
    /// <summary>Atmosphere: fire alarm tests and the like.</summary>
    General,
}

/// <summary>
/// A PA announcement. <see cref="Clips"/> are the recorded lines to play in order (after the chime);
/// <see cref="Text"/> is what is said, for the notification feed and subtitles.
/// </summary>
public sealed record Announcement(AnnouncementKind Kind, string Text, IReadOnlyList<string> Clips) : IGameEvent;

/// <summary>PA announcements. For now just the weekly fire alarm test; the bell and school announcements come later.</summary>
public sealed partial class SchoolSimulation
{
    private DateTime _lastGeneralAnnouncement = DateTime.MinValue;

    /// <summary>Flavour announcements at set times of day.</summary>
    private void UpdateGeneralAnnouncements()
    {
        if (Now.Second != 0 || Now - _lastGeneralAnnouncement < TimeSpan.FromMinutes(30))
        {
            return;
        }

        (string Clip, string Text)? line = (Now.Hour, Now.Minute) switch
        {
            (10, 0) when Now.DayOfWeek == DayOfWeek.Wednesday => ("gen_fire_test", "This is a test of the fire alarm system. No action is required."),
            _ => null,
        };

        if (line is { } l)
        {
            _lastGeneralAnnouncement = Now;
            Events.Publish(new Announcement(AnnouncementKind.General, l.Text, new[] { l.Clip }));
        }
    }
}
