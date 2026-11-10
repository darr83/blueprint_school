using BlueprintSchool.Simulation.Core;
using BlueprintSchool.Simulation.Data;
using BlueprintSchool.Simulation.Simulation;
using BlueprintSchool.Simulation.Staff;
using BlueprintSchool.Simulation.World;

// Runs a school with no windows: the starter reception and corridors, a staff room and a small team,
// then prints what the staff did and where the money went.
// Usage: dotnet run --project tools/SimulationRunner -- [hours=24] [seed=42]
var hours = args.Length > 0 ? double.Parse(args[0]) : 24;
var seed = args.Length > 1 ? int.Parse(args[1]) : 42;

var dataDir = Path.Combine(FindRepoRoot(), "game", "Data");
var db = GameDatabase.Load(new FileSystemDataSource(dataDir));
var events = new EventBus();
var clock = new SimulationClock(events, new DateTime(2027, 1, 4, 0, 0, 0));
var sim = new SchoolSimulation(db, clock, events, StarterSite.Create(), new SimulationOptions { Seed = seed });
clock.Ticked += _ => sim.Tick();

sim.BuildStarterWaitingRoom();
var b = StarterSite.MainBuildingFootprint;
var staffRoom = sim.BuildRoom(sim.AutoPlan("staff_room", new GridRect(b.X + 30, b.Y, 12, 9)));
Console.WriteLine(staffRoom.CanBuild ? "Built a staff room." : "No staff room: " + string.Join("; ", staffRoom.Problems));
foreach (var shift in new[] { StaffShift.Day, StaffShift.Night })
{
    foreach (var role in Enum.GetValues<StaffRole>())
    {
        sim.HireStaff(role, shift: shift);
    }
}

var ticks = (long)(hours * 3600);
for (var i = 0; i < ticks; i++)
{
    clock.Step();
    if (i % 3600 == 0)
    {
        Console.WriteLine($"{clock.Now:ddd HH:mm}  on duty {sim.OnDutyStaff.Count(),2}  on a break {sim.Staff.Count(s => s.IsOnBreak),2}  at the toilets {sim.ToiletTrips.Count}  balance £{sim.Balance:N0}");
    }
}

Console.WriteLine();
Console.WriteLine($"Balance £{sim.Balance:N0} (started £{SchoolSimulation.StartingBalance:N0}). Staff £{sim.StaffCostPerDay:N0}/day, upkeep £{sim.UpkeepPerDay:N0}/day.");
foreach (var day in sim.Accounts.Reverse())
{
    Console.WriteLine($"  {day.Date:ddd}: in £{day.Income:N0}, out £{-day.Spending:N0}, net £{day.Net:N0}  ({string.Join(", ", day.Amounts.Select(a => $"{a.Key} {a.Value:N0}"))})");
}

foreach (var s in sim.Staff)
{
    Console.WriteLine($"  {s.Grade,-24} {s.Name,-24} {s.Shift,-5} breaks {s.Breaks.Count(x => x.Done)}  fatigue {s.Fatigue:0.00}  {s.StatusText}");
}

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "game", "Data")))
    {
        dir = dir.Parent;
    }

    return dir?.FullName ?? throw new DirectoryNotFoundException("Run from inside the repository.");
}
