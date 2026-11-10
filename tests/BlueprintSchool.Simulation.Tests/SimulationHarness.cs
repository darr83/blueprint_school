using BlueprintSchool.Simulation.Core;
using BlueprintSchool.Simulation.Data;
using BlueprintSchool.Simulation.Simulation;
using BlueprintSchool.Simulation.World;

namespace BlueprintSchool.Simulation.Tests;

/// <summary>Builds a full school simulation on the real game data for integration tests.</summary>
internal sealed class SimulationHarness
{
    private static GameDatabase? _database;

    public SimulationHarness(int seed = 1234, DateTime? start = null, bool waitingRoom = false, bool mainCorridor = false)
    {
        Database = _database ??= GameDatabase.Load(new FileSystemDataSource(TestData.GameDataDirectory()));
        Events = new EventBus();
        Clock = new SimulationClock(Events, start ?? new DateTime(2027, 1, 4, 10, 0, 0));
        Sim = new SchoolSimulation(Database, Clock, Events, StarterSite.Create(), new SimulationOptions { Seed = seed });
        Clock.Ticked += _ => Sim.Tick();

        // A new game is an empty building; many tests want the main corridor, which joins the side entrance.
        if (mainCorridor)
        {
            var b = StarterSite.MainBuildingFootprint;
            var corridor = Sim.BuildRoom(new RoomPlan("corridor", new GridRect(b.X + 4, b.Y + 9, 56, 8), WallSide.South));
            if (!corridor.CanBuild)
            {
                throw new InvalidOperationException(string.Join("; ", corridor.Problems));
            }
        }

        // A new game has no reception; some tests want one, fitted out with desk, doors and seats.
        if (waitingRoom)
        {
            Sim.BuildStarterWaitingRoom();
        }
    }

    public GameDatabase Database { get; }

    public EventBus Events { get; }

    public SimulationClock Clock { get; }

    public SchoolSimulation Sim { get; }

    public void RunFor(TimeSpan time)
    {
        var ticks = (long)(time.TotalSeconds / SimulationClock.TickLength.TotalSeconds);
        for (var i = 0; i < ticks; i++)
        {
            Clock.Step();
        }
    }

    /// <summary>Runs until the condition holds, failing the test if it doesn't within <paramref name="limit"/>.</summary>
    public void RunUntil(Func<bool> condition, TimeSpan limit, string what)
    {
        var end = Clock.Now + limit;
        while (!condition())
        {
            if (Clock.Now >= end)
            {
                throw new Xunit.Sdk.XunitException($"Timed out after {limit} waiting for: {what}");
            }

            Clock.Step();
        }
    }

    /// <summary>Hires everyone the recommended staffing says is missing, the way a player following it would.</summary>
    public void StaffToRecommended()
    {
        foreach (var need in Sim.RecommendedStaffing())
        {
            for (var i = 0; i < need.Short; i++)
            {
                Sim.HireStaff(need.Role, need.RoomId, skill: 0.7, shift: need.Shift);
            }
        }
    }
}
