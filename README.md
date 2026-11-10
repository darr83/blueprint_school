# Blueprint School

A UK secondary school management simulation (ages 11 to 16) for Windows, built with
**Godot 4.7 (.NET)** and **C#**. It is the sister game to Blueprint Hospital and grows out of the
same engine: players build a school, hire the staff and, in later milestones, run the school day
from the first bell to the last, with Year 7 to Year 11 pupils, lessons, behaviour and results.

> The game follows real UK school structures (year groups, Key Stages, GCSEs, pastoral care and
> safeguarding) but uses a made-up inspectorate and no real exam board names.

## Status

| Milestone | State |
|---|---|
| 1. Foundation: the hospital engine copied over, renamed, and patients, triage and NEWS2 taken out | ✅ Done |
| 2. School look: colours, floors, bell timeline, figures, subject and room data | ✅ This branch |
| 3. One playable school day, Year 7 only | Planned |
| 4. Headteacher assistant and staffing | Planned |

What works now: an empty school building and money in the bank; corridors, reception, staff rooms,
toilets, a canteen and vending, extensions, more floors with lifts and stairs; every school room
(classrooms by subject, science labs and prep rooms, computer suites, art, music, drama, D&T, food,
a sports hall with court lines, changing rooms, a library, a SEND base, offices, a medical room)
laid out with desks, benches and kit; decorating; the school's green and gold look; a bell timeline
in the top bar, the school bell at the start of each period and PA announcements through the day;
pupils in uniform (bottle-green blazer, house tie, sized by year) and staff lanyards by role;
hiring teachers, teaching assistants, a receptionist and site staff; finances, stats,
notifications, the cheat console, and full save and load.

The data for the day lives in `game/Data`: `Subjects/` (15 subjects with Key Stage 3 lessons a
week, adding up to 25), `BellSchedule/` (the periods of the day) and `RoomTemplates/`.

Not yet: pupils in the building, the timetable and lessons (milestone 3). Still from the hospital:
the day and night staff teams, which become the school day in milestone 3. New PA voice lines and
school ambience need an ElevenLabs key (see `tools/SoundBaker/README.md`); until they're recorded
those announcements are silent.

## Repository layout

```
game/                          Godot project (open this folder in Godot)
  project.godot
  BlueprintSchool.csproj       Game scripts; references the simulation library
  Data/                        All data-driven content (JSON): departments, subjects, bell schedule, rooms, furniture, names, build menu
  Scenes/                      Main menu and school scenes
  Scripts/
    Audio/                     Ambience, interface sounds and the PA announcer
    Core/                      GameManager (autoload), session, settings, saves, smoke test
    School/                    Site, rooms and people drawing, isometric camera, build tool
    UI/                        Main menu, HUD, build bar, staff, rota, recruitment, stats and finance panels
src/BlueprintSchool.Simulation/   Engine-independent game rules (no Godot references)
  Core/                        Simulation clock, event bus, save format
  Data/                        JSON loading and validation, data models
  World/                       Grid, metres to tiles, isometric projection, site map, A* pathfinding
  Staff/                       Staff members, roles and grades
  Rooms/                       Rooms, room layouts (teaching, hall, library...), decor
  SchoolDay/                   The bell schedule
  Simulation/                  SchoolSimulation: building, staff, shifts, breaks, toilets, money, saves
tests/BlueprintSchool.Simulation.Tests/  xUnit tests
tools/SimulationRunner/        Console tool: runs a small school for a while and prints the results
tools/SoundBaker/              Scripts that make the sound effects and PA lines
```

The rules of the game live in `src/` as plain C#, so they can be tested quickly without
starting Godot. The `game/` project only draws the world and handles input.

## Opening and running the game

You need:

- **Godot 4.7.2 .NET** (the "mono" download), e.g. `Godot_v4.7.2-stable_mono_win64.exe`.
- **.NET 8 SDK**: https://dotnet.microsoft.com/download/dotnet/8.0

Then:

1. Start Godot, choose **Import**, and select `game/project.godot`.
2. Press **Build** (hammer icon, top right) once, then **Run Project** (F5).

A new game starts with an empty building. Lay a corridor in from the main entrance (south) or the
side entrance (east, where the main corridor line meets the wall), then build rooms off it.

### Controls

| Action | Control |
|---|---|
| Move camera | Middle mouse drag, or WASD / arrow keys |
| Zoom | Mouse wheel, or the + and − buttons bottom right |
| Pause / resume | Space |
| Speed 1x / 2x / 4x | 1 / 2 / 3 |
| Play or Build mode | The Play / Build switch in the top bar, or B |
| Staff rota | T |
| Recruitment | H |
| Finances | F |
| Stats | I |
| Toggle building grid | G |
| Show walking routes | R |
| Rotate the view a quarter turn | Q / E |
| Look at the floor above / below | Page Up / Page Down |
| Cheat console | Ctrl+C |
| Close a panel, leave Build mode, or open the pause menu | Esc |

## Checks

```
dotnet test tests/BlueprintSchool.Simulation.Tests
dotnet build game/BlueprintSchool.csproj
godot --headless --path game --import
godot --headless --path game -- --smoke-test
```

The smoke test starts a new school, lays corridors, fits out reception and a staff room with the
build tools, hires staff, opens every panel, adds a canteen, toilets and a first floor, saves,
reloads and compares. Add `--screenshots=DIR` (with a display, not `--headless`) to save a
screenshot at each step. CI runs all four on every pull request.

To run the simulation without Godot:

```
dotnet run --project tools/SimulationRunner -- 24 42   # hours, seed
```
