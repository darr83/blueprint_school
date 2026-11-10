namespace BlueprintSchool.Simulation.Simulation;

/// <summary>What money came in or went out for.</summary>
public enum MoneyCategory
{
    /// <summary>Funding for each pupil on roll (comes with the pupils).</summary>
    Funding,

    /// <summary>Wages for every member of staff.</summary>
    Staff,

    /// <summary>Cleaning, heating, lighting and repairs, by floor area.</summary>
    Upkeep,

    /// <summary>Building, moving and demolishing rooms and extending the building.</summary>
    Construction,

    /// <summary>The school's share of what is spent in its canteen and vending machines.</summary>
    Catering,

    /// <summary>Money added from the cheat console.</summary>
    Cheats,
}

/// <summary>Money in and out on one day, by category. Spending is negative.</summary>
public sealed class DayAccounts
{
    public required DateTime Date { get; init; }

    public Dictionary<MoneyCategory, long> Amounts { get; } = new();

    public long Income => Amounts.Values.Where(v => v > 0).Sum();

    public long Spending => Amounts.Values.Where(v => v < 0).Sum();

    public long Net => Amounts.Values.Sum();

    public long this[MoneyCategory category] => Amounts.GetValueOrDefault(category);

    internal void Add(MoneyCategory category, long amount) => Amounts[category] = Amounts.GetValueOrDefault(category) + amount;
}

/// <summary>
/// The school's money: for now just the running costs of staff and the building, and what building
/// work costs. Funding per pupil comes with the pupils.
/// </summary>
public sealed partial class SchoolSimulation
{
    /// <summary>Each hire is one post, paid every day of the year.</summary>
    public const double StaffPerPost = 1.0;

    /// <summary>Upkeep per square metre of building per day.</summary>
    public const double UpkeepPerSquareMetrePerDay = 0.6;

    /// <summary>Days of accounts kept for the finance panel.</summary>
    private const int DaysKept = 14;

    private readonly List<DayAccounts> _days = new();
    private double _unpaidWages;
    private double _unpaidUpkeep;
    private bool _overdrawnWarned;

    /// <summary>Recent days' accounts, today first.</summary>
    public IReadOnlyList<DayAccounts> Accounts => _days;

    public DayAccounts Today => Day(Now.Date);

    /// <summary>What the staff cost a day in wages.</summary>
    public long StaffCostPerDay => (long)Math.Round(_staff.Sum(s => s.Salary) * StaffPerPost / 365.0);

    public long UpkeepPerDay => (long)Math.Round(MainBuilding.AreaSquareMetres * UpkeepPerSquareMetrePerDay);

    private DayAccounts Day(DateTime date)
    {
        if (_days.Count > 0 && _days[0].Date == date)
        {
            return _days[0];
        }

        var day = new DayAccounts { Date = date };
        _days.Insert(0, day);
        if (_days.Count > DaysKept)
        {
            _days.RemoveAt(_days.Count - 1);
        }

        return day;
    }

    /// <summary>Records money in (positive) or out (negative) and updates the balance.</summary>
    /// <summary>
    /// Unlimited money: building, unlocking and hiring never stop for lack of money, so the balance can
    /// go into the red. Wages, income and the accounts are still counted as normal.
    /// </summary>
    public bool UnlimitedMoney { get; set; }

    /// <summary>Whether something costing <paramref name="cost"/> can be paid for now.</summary>
    public bool CanAfford(long cost) => UnlimitedMoney || cost <= 0 || Balance >= cost;

    internal void Book(MoneyCategory category, long amount)
    {
        if (amount == 0)
        {
            return;
        }

        Balance += amount;
        Day(Now.Date).Add(category, amount);
        Tally(StatKeys.Money(category), amount);

        if (Balance < 0 && !_overdrawnWarned && !UnlimitedMoney)
        {
            _overdrawnWarned = true;
            Notifications.Add(Now, NotificationLevel.Critical, $"The school is overdrawn (£{Balance:N0}). Building is stopped until money comes in. Look at Finances to see where it is going.");
        }
        else if (Balance > 0)
        {
            _overdrawnWarned = false;
        }
    }

    /// <summary>Wages and upkeep, charged a little every tick.</summary>
    private void ChargeRunningCosts(double dt)
    {
        _unpaidWages += StaffCostPerDay * dt / 86_400.0;
        _unpaidUpkeep += UpkeepPerDay * dt / 86_400.0;
        if (_unpaidWages >= 10)
        {
            var due = (long)Math.Floor(_unpaidWages);
            _unpaidWages -= due;
            Book(MoneyCategory.Staff, -due);
        }

        if (_unpaidUpkeep >= 10)
        {
            var due = (long)Math.Floor(_unpaidUpkeep);
            _unpaidUpkeep -= due;
            Book(MoneyCategory.Upkeep, -due);
        }
    }
}
