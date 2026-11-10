namespace BlueprintSchool.Simulation.Simulation;

/// <summary>Names of the figures kept in <see cref="SchoolSimulation.History"/>.</summary>
public static class StatKeys
{
    public const string Hired = "staff.hired";
    public const string Left = "staff.left";

    // Gauges, read every quarter of an hour.
    public const string StaffOnDuty = "staff.on_duty";
    public const string Balance = "money.balance";

    public static string Money(MoneyCategory category) => $"money.{category.ToString().ToLowerInvariant()}";
}

/// <summary>
/// The figures behind the Stats screen. Money and staff are counted where they happen, and the gauges
/// are read every 15 minutes.
/// </summary>
public sealed partial class SchoolSimulation
{
    private static readonly TimeSpan GaugeInterval = TimeSpan.FromMinutes(15);
    private DateTime _nextGaugeReading;
    private StatsHistory? _history;

    /// <summary>The school's figures over the last 24 hours, 7 days, 28 days and since opening.</summary>
    public StatsHistory History
    {
        get
        {
            UpdateHistory();
            return _history!;
        }
    }

    private StatsHistory HistoryStore => _history ??= new StatsHistory(Now);

    /// <summary>Counts something into the history now.</summary>
    internal void Tally(string key, double amount = 1) => HistoryStore.Add(Now, key, amount);

    /// <summary>Reads the gauges when due.</summary>
    private void UpdateHistory()
    {
        var history = HistoryStore;
        if (Now < _nextGaugeReading)
        {
            return;
        }

        // Readings fall on the quarter hour.
        var minute = Now.Minute / 15 * 15;
        _nextGaugeReading = new DateTime(Now.Year, Now.Month, Now.Day, Now.Hour, minute, 0, Now.Kind) + GaugeInterval;
        history.Sample(Now, StatKeys.StaffOnDuty, OnDutyStaff.Count());
        history.Sample(Now, StatKeys.Balance, Balance);
    }

    /// <summary>After loading: puts back the saved history (or starts it afresh).</summary>
    private void RestoreHistory(StatsHistoryState? state)
    {
        if (state is not null)
        {
            HistoryStore.Restore(state);
        }
        else
        {
            HistoryStore.Reset(Now);
        }

        // The last reading was taken before the save; the next falls on the coming quarter hour.
        _nextGaugeReading = new DateTime(Now.Year, Now.Month, Now.Day, Now.Hour, Now.Minute / 15 * 15, 0, Now.Kind) + GaugeInterval;
    }
}
