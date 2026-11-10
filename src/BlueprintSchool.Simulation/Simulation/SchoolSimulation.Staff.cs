using BlueprintSchool.Simulation.Hospital;
using BlueprintSchool.Simulation.Staff;

namespace BlueprintSchool.Simulation.Simulation;

/// <summary>
/// Staff AI. For now staff come in for their shift, wait at their base, take their breaks and go home.
/// Lessons and the rest of the school day come with a later milestone.
/// </summary>
public sealed partial class SchoolSimulation
{
    private const double FatigueWhenWorkingPerHour = 0.10;
    private const double FatigueWhenIdlePerHour = 0.02;
    private const double BreakThreshold = 0.75;
    private static readonly TimeSpan BreakLength = TimeSpan.FromMinutes(20);

    /// <summary>Shifts change at 08:00 and 20:00: the outgoing team go home (see the shifts file).</summary>
    private static readonly int[] ShiftChangeHours = { DayShiftStartHour, NightShiftStartHour };

    private void HandleShiftChange()
    {
        if (Now.Minute != 0 || Now.Second != 0 || !ShiftChangeHours.Contains(Now.Hour))
        {
            return;
        }

        var coming = _staff.Count(s => s.Shift == CurrentShift);
        if (coming == 0)
        {
            return;
        }

        Notifications.Add(Now, NotificationLevel.Info, Now.Hour == DayShiftStartHour
            ? $"Day shift has started: {coming} staff are coming in."
            : $"Night shift has started: {coming} staff are coming in.");
    }

    private void UpdateStaff(double dt)
    {
        HandleShiftChange();
        foreach (var member in _staff)
        {
            if (UpdateDuty(member))
            {
                continue;
            }

            var working = member.Activity == StaffActivity.Working;
            member.Fatigue = Math.Clamp(member.Fatigue + ((working ? FatigueWhenWorkingPerHour : FatigueWhenIdlePerHour) * dt / 3600.0), 0, 1);

            switch (member.Activity)
            {
                case StaffActivity.WalkingToBreak when !member.IsMoving:
                    member.Activity = StaffActivity.OnBreak;
                    member.StatusText = $"On a break · back at {member.BreakEndsAt:HH:mm}";
                    continue;

                case StaffActivity.WalkingToBreak:
                    continue;

                case StaffActivity.OnBreak:
                    member.Fatigue = Math.Max(0.1, member.Fatigue - (dt / BreakLength.TotalSeconds * 0.7));
                    if (Now >= member.BreakEndsAt)
                    {
                        FinishBreak(member);
                        member.Activity = StaffActivity.Idle;
                        member.StatusText = "Available";
                        member.BreakEndsAt = null;
                    }

                    continue;
            }

            if (DueBreak(member) is { } due && CanTakeBreak(member, due))
            {
                TakeBreak(member, due);
                continue;
            }

            if (member.Role != StaffRole.Receptionist && member.Fatigue >= BreakThreshold)
            {
                TakeBreak(member);
                continue;
            }

            member.Activity = StaffActivity.Idle;
            member.StatusText = "Available";
            WalkTo(member, HomeTile(member));
        }
    }

    /// <summary>Goes to the staff room for a planned break, or (with none given) an extra break because they're worn out.</summary>
    private void TakeBreak(StaffMember member, PlannedBreak? planned = null)
    {
        if (planned is null)
        {
            planned = new PlannedBreak { Start = Now, Minutes = (int)BreakLength.TotalMinutes + RotaRandom.Range(0, 5) };
            member.Breaks.Add(planned);
            member.Breaks.Sort((a, b) => a.Start.CompareTo(b.Start));
        }

        planned.TakenAt = Now;
        var seats = SpacesOfKind(CareSpaceKind.StaffRoom).SelectMany(s => s.Seats).ToList();
        var seat = seats.Count > 0 ? seats[(member.Id - 1) % seats.Count] : HomeTile(member);
        member.Activity = StaffActivity.WalkingToBreak;
        member.BreakEndsAt = Now + TimeSpan.FromMinutes(planned.Minutes);
        member.StatusText = planned.IsMeal ? $"Meal break · back at {member.BreakEndsAt:HH:mm}" : $"Tea break · back at {member.BreakEndsAt:HH:mm}";
        WalkTo(member, seat);
    }
}
