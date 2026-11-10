using BlueprintSchool.Simulation.Agents;
using BlueprintSchool.Simulation.Core;
using BlueprintSchool.Simulation.Hospital;
using BlueprintSchool.Simulation.World;

namespace BlueprintSchool.Simulation.Simulation;

/// <summary>Where someone is on a trip for something to eat or drink.</summary>
public enum RefreshmentStage
{
    /// <summary>Walking to the counter or a vending machine.</summary>
    GoingToBuy,

    /// <summary>At the counter or machine, paying.</summary>
    Buying,

    /// <summary>Sitting at a cafe table with their food and drink.</summary>
    Eating,
}

/// <summary>A trip to a cafeteria or vending point.</summary>
public sealed class RefreshmentTrip
{
    public required Agent Person { get; init; }

    public required string RoomId { get; init; }

    /// <summary>The counter queue tile or vending machine they buy from.</summary>
    public required GridCoord BuyAt { get; init; }

    public RefreshmentStage Stage { get; set; } = RefreshmentStage.GoingToBuy;

    /// <summary>The cafe chair they sit on to eat, if any.</summary>
    public GridCoord? Table { get; set; }

    /// <summary>When the current stage (paying, eating) finishes.</summary>
    public DateTime? Until { get; set; }
}

/// <summary>
/// Cafeterias and vending points. They can be built, but nobody uses them yet: pupils and staff buying
/// food at break and lunch come with a later milestone.
/// </summary>
public sealed partial class SchoolSimulation
{
    private readonly Dictionary<Agent, RefreshmentTrip> _trips = new();

    /// <summary>Cafeterias and vending points.</summary>
    public IEnumerable<CareSpace> RefreshmentRooms => _spaces.Values.Where(s => s.IsRefreshments);

    /// <summary>People out getting something to eat or drink right now.</summary>
    public IReadOnlyCollection<RefreshmentTrip> RefreshmentTrips => _trips.Values;

    /// <summary>The trip a person is on, if they are away getting refreshments.</summary>
    public RefreshmentTrip? TripOf(Agent person) => _trips.GetValueOrDefault(person);

    private static int Distance(GridCoord a, GridCoord b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
}
