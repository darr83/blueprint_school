using System;
using BlueprintSchool.Simulation.Core;
using BlueprintSchool.Simulation.Data;
using BlueprintSchool.Simulation.Simulation;
using BlueprintSchool.Simulation.World;

namespace BlueprintSchool.Core;

/// <summary>One game in progress: its clock, event bus, hospital site and the simulation running on them.</summary>
public sealed class GameSession
{
    private GameSession(GameDatabase database, string hospitalName, SaveGame? save, StartingHospital start = StartingHospital.SmallEmergency, bool unlimitedMoney = false)
    {
        Database = database;
        HospitalName = hospitalName;
        Events = new EventBus();
        Clock = new SimulationClock(Events);
        Site = StarterSite.Create();

        if (save is not null)
        {
            Clock.RestoreState(save.Clock);
            InitialCamera = save.Camera;
        }

        // Each clock tick advances the hospital by one game second.
        Simulation = new SchoolSimulation(database, Clock, Events, Site, new SimulationOptions { Seed = Environment.TickCount, Start = start, UnlimitedMoney = unlimitedMoney });
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

    public string HospitalName { get; }

    public EventBus Events { get; }

    public SimulationClock Clock { get; }

    public SiteMap Site { get; }

    /// <summary>Patients, staff and everything else happening in the hospital.</summary>
    public SchoolSimulation Simulation { get; }

    /// <summary>Camera to restore when the hospital scene opens, if the game was loaded.</summary>
    public CameraState? InitialCamera { get; }

    public static GameSession CreateNew(GameDatabase database, string hospitalName, StartingHospital start = StartingHospital.SmallEmergency, bool unlimitedMoney = false) =>
        new(database, string.IsNullOrWhiteSpace(hospitalName) ? "Blueprint General Hospital" : hospitalName.Trim(), null, start, unlimitedMoney);

    public static GameSession FromSave(GameDatabase database, SaveGame save) => new(database, save.HospitalName, save);

    public SaveGame CreateSave(CameraState camera) => new()
    {
        HospitalName = HospitalName,
        SavedAtUtc = DateTime.UtcNow,
        Clock = Clock.CaptureState(),
        Camera = camera,
        Layout = Simulation.CaptureLayout(),
        People = Simulation.CapturePeople(),
    };
}
