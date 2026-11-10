using System;
using BlueprintSchool.Simulation.Core;
using BlueprintSchool.Simulation.Data;
using BlueprintSchool.Simulation.Simulation;
using BlueprintSchool.Simulation.World;

namespace BlueprintSchool.Core;

/// <summary>One game in progress: its clock, event bus, school site and the simulation running on them.</summary>
public sealed class GameSession
{
    private GameSession(GameDatabase database, string schoolName, SaveGame? save, bool unlimitedMoney = false)
    {
        Database = database;
        SchoolName = schoolName;
        Events = new EventBus();
        Clock = new SimulationClock(Events);
        Site = StarterSite.Create();

        if (save is not null)
        {
            Clock.RestoreState(save.Clock);
            InitialCamera = save.Camera;
        }

        // Each clock tick advances the school by one game second.
        Simulation = new SchoolSimulation(database, Clock, Events, Site, new SimulationOptions { Seed = Environment.TickCount, UnlimitedMoney = unlimitedMoney });
        if (save?.Layout is { } layout)
        {
            Simulation.RestoreLayout(layout);
        }

        if (save?.People is { } people)
        {
            Simulation.RestorePeople(people);
        }

        Clock.Ticked += _ => Simulation.Tick();
    }

    public GameDatabase Database { get; }

    public string SchoolName { get; }

    public EventBus Events { get; }

    public SimulationClock Clock { get; }

    public SiteMap Site { get; }

    /// <summary>Staff and everything else happening in the school.</summary>
    public SchoolSimulation Simulation { get; }

    /// <summary>Camera to restore when the school scene opens, if the game was loaded.</summary>
    public CameraState? InitialCamera { get; }

    public static GameSession CreateNew(GameDatabase database, string schoolName, bool unlimitedMoney = false) =>
        new(database, string.IsNullOrWhiteSpace(schoolName) ? "Blueprint Academy" : schoolName.Trim(), null, unlimitedMoney);

    public static GameSession FromSave(GameDatabase database, SaveGame save) => new(database, save.SchoolName, save);

    public SaveGame CreateSave(CameraState camera) => new()
    {
        SchoolName = SchoolName,
        SavedAtUtc = DateTime.UtcNow,
        Clock = Clock.CaptureState(),
        Camera = camera,
        Layout = Simulation.CaptureLayout(),
        People = Simulation.CapturePeople(),
    };
}
