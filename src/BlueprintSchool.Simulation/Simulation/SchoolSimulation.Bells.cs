using BlueprintSchool.Simulation.SchoolDay;

namespace BlueprintSchool.Simulation.Simulation;

/// <summary>The school bell: rings at the start of each period, as the bell schedule says.</summary>
public sealed partial class SchoolSimulation
{
    private DateTime? _lastBellCheck;

    /// <summary>Where the school day is now.</summary>
    public BellPosition Bell => Database.Bells.Position(Now);

    private void RingBells()
    {
        var before = _lastBellCheck ?? Now;
        _lastBellCheck = Now;
        if (Database.Bells.BellBetween(before, Now) is { } period)
        {
            Events.Publish(new BellRang(Now, period));
        }
    }
}
