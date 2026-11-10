using System;
using System.Linq;
using BlueprintSchool.Hospital;
using BlueprintSchool.Simulation.Core;
using BlueprintSchool.Simulation.Hospital;
using BlueprintSchool.Simulation.Simulation;
using BlueprintSchool.Simulation.Staff;
using BlueprintSchool.Simulation.World;
using BlueprintSchool.UI;
using Godot;

namespace BlueprintSchool.Core;

/// <summary>
/// Automated launch check, run with: godot --headless --path game -- --smoke-test
/// Starts a new school in an empty building, lays corridors and fits out a reception and staff room
/// with the build tools, hires staff, opens every panel, adds toilets, a canteen and a first floor,
/// lets the clock run, saves, reloads and compares, then exits with code 0 on success or 1 on failure.
/// Used to verify each build actually runs.
/// </summary>
public partial class SmokeTest : Node
{
    private const string SlotName = "smoke_test";
    private const string SchoolName = "Smoke Test Academy";
    private const long CheatMoney = 123_456;

    private static readonly GridRect B = StarterSite.MainBuildingFootprint;

    private int _frame;
    private Stage _stage = Stage.WaitForMenu;
    private DateTime _savedTime;
    private CameraState? _savedCamera;
    private string? _screenshotDirectory;
    private int _savedRooms;
    private long _savedBalance;
    private int _savedStaff;
    private int _savedChairs;
    private int _savedFloors;

    private enum Stage
    {
        WaitForMenu,
        NewGameChoice,
        WaitForSchool,
        RunSimulation,
        Notifications,
        Reception,
        ReceptionTurned,
        StaffPanel,
        BuildPreview,
        RoomPanel,
        BuildItem,
        BuildPopup,
        Decorate,
        Decorated,
        Recruitment,
        Rota,
        RotaBreaks,
        Stats,
        StatsWeek,
        Canteen,
        Toilets,
        UpperFloor,
        Rotate,
        Finances,
        WaitForLoad,
        Done,
    }

    public static void Begin(Node menu)
    {
        var manager = GameManager.Instance;
        if (manager.GetNodeOrNull("SmokeTest") is not null)
        {
            return;
        }

        var test = new SmokeTest { Name = "SmokeTest", ProcessMode = ProcessModeEnum.Always };
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (arg.StartsWith("--screenshots=", StringComparison.Ordinal))
            {
                test._screenshotDirectory = arg["--screenshots=".Length..];
            }
        }

        manager.AddChild(test);
        GD.Print("[smoke] Main menu loaded.");
    }

    public override void _Process(double delta)
    {
        _frame++;
        try
        {
            Step();
        }
        catch (Exception ex)
        {
            Fail(ex.ToString());
        }

        if (_frame > 2000)
        {
            Fail($"Timed out in stage {_stage}.");
        }
    }

    private void Next(Stage stage)
    {
        _frame = 0;
        _stage = stage;
    }

    private void Step()
    {
        var manager = GameManager.Instance;
        var scene = GetTree().CurrentScene;

        switch (_stage)
        {
            case Stage.WaitForMenu when _frame >= 10:
                Screenshot("01_main_menu");
                foreach (var panel in new ModalPanel[] { new SettingsPanel(), new LoadGamePanel() })
                {
                    scene.AddChild(panel);
                    panel.Close();
                }

                GD.Print("[smoke] Settings and Load Game panels open and close.");
                Check(manager.Database is not null, "Game data failed to load: " + manager.DataLoadError);
                ((MainMenu)scene).ShowNewGame();
                Next(Stage.NewGameChoice);
                break;

            case Stage.NewGameChoice when _frame >= 5:
                Screenshot("01b_new_game");
                manager.StartNewGame(SchoolName);
                Next(Stage.WaitForSchool);
                break;

            case Stage.WaitForSchool when scene is HospitalScene school && school.IsNodeReady() && _frame >= 15:
            {
                Screenshot("01c_empty_building");
                var sim = school.Session.Simulation;
                Check(school.Session.Site.Buildings.Count == 1, "Starter building missing.");
                Check(school.Camera.IsCurrent(), "Camera is not active.");
                Check(!sim.Spaces.Any(), "A new school should have no rooms, not even a corridor.");
                Check(!sim.Staff.Any(), "A new school should have no staff.");
                GD.Print($"[smoke] Empty building: no rooms, no staff, £{sim.Balance:N0}.");
                LayCorridors(school);
                FitOutReception(school);
                BuildStaffRoom(school);
                HireTeam(school);
                school.Session.Clock.SetSpeed(SimulationSpeed.VeryFast);
                Next(Stage.RunSimulation);
                break;
            }

            case Stage.RunSimulation when scene is HospitalScene school && _frame >= 30:
                Screenshot("02_school");
                Check(school.Session.Clock.TickCount > 0, "Simulation clock did not advance.");
                Check(school.Hud.Feed.GetGlobalRect().End.Y <= MainHUD.TopBarHeight, "The notifications box is not in the top bar.");
                school.Hud.Feed.PinnedOpen = true;
                Next(Stage.Notifications);
                break;

            case Stage.Notifications when scene is HospitalScene school && _frame >= 5:
                Screenshot("02b_notifications");
                Check(school.Hud.Feed.Dropdown.Visible, "Hovering the notifications box did not show the recent events.");
                Check(school.Hud.Feed.RowsShown == Math.Min(NotificationFeed.Shown, school.Session.Simulation.Notifications.Count), $"The notifications list shows {school.Hud.Feed.RowsShown} events, not up to {NotificationFeed.Shown}.");
                school.Hud.Feed.PinnedOpen = false;
                GD.Print($"[smoke] Notifications: latest in the top bar, {school.Hud.Feed.RowsShown} on hover.");
                LookAtReception(school);
                Next(Stage.Reception);
                break;

            case Stage.Reception when scene is HospitalScene school && _frame >= 5:
                Screenshot("03a_reception");
                school.RotateView(1);
                LookAtReception(school);
                Next(Stage.ReceptionTurned);
                break;

            case Stage.ReceptionTurned when scene is HospitalScene school && _frame >= 5:
            {
                Screenshot("03b_reception_turned");
                school.RotateView(-1);
                var member = school.Session.Simulation.Staff.First(m => m.IsInBuilding);
                school.SelectStaff(member, centre: true);
                Check(school.Hud.StaffInfo.Visible, "Selecting a member of staff did not open their panel.");
                CheckStaffPanelsFit(school);
                school.SelectStaff(member, centre: true);
                Next(Stage.StaffPanel);
                break;
            }

            case Stage.StaffPanel when scene is HospitalScene school && _frame >= 20:
                Screenshot("04_staff_panel");
                school.ClearSelectionForTest();
                Check(!school.Hud.StaffInfo.Visible, "Staff panel did not close.");
                StartBuilding(school);
                Next(Stage.BuildPreview);
                break;

            case Stage.BuildPreview when scene is HospitalScene school && _frame >= 5:
            {
                Screenshot("05_build_preview");
                var sim = school.Session.Simulation;
                var built = sim.Spaces.Count();
                school.BuildTool.Confirm();
                Check(sim.Spaces.Count() == built + 1, "The previewed staff room was not built.");
                school.BuildTool.Cancel();
                var newRoom = sim.Spaces.Where(s => s.TemplateId == "staff_room").OrderBy(s => s.Rect.X).First();
                ClickRoomFloor(school, newRoom);
                GD.Print($"[smoke] Built {newRoom.Name}; balance £{sim.Balance:N0}.");
                Next(Stage.RoomPanel);
                break;
            }

            case Stage.RoomPanel when scene is HospitalScene school && _frame >= 5:
                Screenshot("06_room_panel");
                ClickBuildItem(school);
                Next(Stage.BuildItem);
                break;

            case Stage.BuildItem when scene is HospitalScene school && _frame >= 5:
                Screenshot("06a_build_item");
                school.Hud.HideRightPanels();
                school.SetMode(GameMode.Build);
                CheckBuildBar(school);
                school.Hud.Build.Open("school");
                Next(Stage.BuildPopup);
                break;

            case Stage.BuildPopup when scene is HospitalScene school && _frame == 5:
                CheckBuildPopup(school);
                school.Hud.Build.ShowHover("staff_room");
                break;

            case Stage.BuildPopup when scene is HospitalScene school && _frame >= 8:
                Screenshot("06b_build_popup");
                school.Hud.Build.ClosePopup();
                StartDecorating(school);
                Next(Stage.Decorate);
                break;

            case Stage.Decorate when scene is HospitalScene school && _frame >= 5:
                Screenshot("06c_decorate");
                FinishDecorating(school);
                Next(Stage.Decorated);
                break;

            case Stage.Decorated when scene is HospitalScene school && _frame >= 5:
                Screenshot("06d_decorated");
                school.SetMode(GameMode.Play);
                school.Hud.ToggleRecruitment();
                Check(school.Hud.Recruitment.Visible, "Recruitment did not open.");
                Next(Stage.Recruitment);
                break;

            case Stage.Recruitment when scene is HospitalScene school && _frame >= 10:
            {
                Screenshot("07_recruitment");
                var recruitment = school.Hud.Recruitment;
                Check(recruitment.GetCombinedMinimumSize().X <= recruitment.Size.X + 1
                      && recruitment.GetGlobalRect().End.X <= recruitment.GetViewportRect().Size.X + 1,
                    $"Recruitment is wider than its dock ({recruitment.GetCombinedMinimumSize().X:0} > {recruitment.Size.X:0}).");
                school.Hud.ToggleRecruitment();
                school.Hud.ToggleRoster();
                Check(school.Hud.Roster.Visible, "The staff list did not open.");
                school.Hud.ToggleRoster();
                OpenRota(school);
                Next(Stage.Rota);
                break;
            }

            case Stage.Rota when scene is HospitalScene school && _frame >= 10:
                Screenshot("07b_rota");
                school.Hud.Rota.ShowBreaksForTest();
                Next(Stage.RotaBreaks);
                break;

            case Stage.RotaBreaks when scene is HospitalScene school && _frame >= 10:
                Screenshot("07c_rota_breaks");
                school.Hud.ToggleRota();
                Check(!school.Hud.Rota.Visible, "The rota did not close.");
                school.Hud.ToggleStats();
                Check(school.Hud.StatsScreen.Visible, "The Stats screen did not open.");
                school.Hud.StatsScreen.ShowPeriod(StatsPeriod.Last24Hours);
                Next(Stage.Stats);
                break;

            case Stage.Stats when scene is HospitalScene school && _frame >= 10:
            {
                Screenshot("07d_stats");
                var stats = school.Hud.StatsScreen;
                Check(stats.GetCombinedMinimumSize().X <= stats.Size.X + 1, $"The Stats screen is wider than the screen ({stats.GetCombinedMinimumSize().X:0} > {stats.Size.X:0}).");
                var sim = school.Session.Simulation;
                var hired = sim.History.Total(StatsPeriod.AllTime, sim.Now, StatKeys.Hired);
                Check(hired == 1, $"Stats all-time hires {hired} should count the one hired from the shortlist.");
                GD.Print($"[smoke] Stats: {hired} hired all time.");
                stats.ShowPeriod(StatsPeriod.Last7Days);
                Next(Stage.StatsWeek);
                break;
            }

            case Stage.StatsWeek when scene is HospitalScene school && _frame >= 10:
                Screenshot("07e_stats_week");
                school.Hud.ToggleStats();
                Check(!school.Hud.StatsScreen.Visible, "The Stats screen did not close.");
                BuildCanteen(school);
                Next(Stage.Canteen);
                break;

            case Stage.Canteen when scene is HospitalScene school && _frame >= 6:
                Screenshot("08_canteen");
                BuildToilets(school);
                Next(Stage.Toilets);
                break;

            case Stage.Toilets when scene is HospitalScene school && _frame >= 6:
                Screenshot("08b_toilets");
                BuildUpperFloor(school);
                Next(Stage.UpperFloor);
                break;

            case Stage.UpperFloor when scene is HospitalScene school && _frame >= 6:
                Screenshot("09_first_floor");
                school.ShowFloor(0);
                Check(IsoDraw.ViewFloor == 0, "Did not go back to the ground floor.");
                school.ToggleRoutes();
                Next(Stage.Rotate);
                break;

            case Stage.Rotate when scene is HospitalScene school && _frame >= 6:
                Screenshot($"10_view_{IsoDraw.Rotation}");
                school.RotateView(1);
                _frame = 0;
                if (IsoDraw.Rotation != 0)
                {
                    break;
                }

                school.ToggleRoutes();
                GD.Print("[smoke] Rotated the view through all four angles with routes shown.");
                CheckCheatConsole(school);
                school.Hud.ToggleFinances();
                Check(school.Hud.Finances.Visible, "Finances did not open.");
                Check(school.Session.Simulation.Accounts.Any(d => d[MoneyCategory.Construction] < 0), "Building work was not booked.");
                Next(Stage.Finances);
                break;

            case Stage.Finances when scene is HospitalScene school && _frame >= 10:
            {
                Screenshot("11_finances");
                PressCtrlC(school);
                Check(!school.Hud.Cheats.Visible, "Ctrl+C did not close the cheat console.");
                school.Hud.ToggleFinances();
                var clock = school.Session.Clock;
                var sim = school.Session.Simulation;
                GD.Print($"[smoke] Clock advanced {clock.TickCount} ticks to {clock.Now:yyyy-MM-dd HH:mm:ss}.");
                clock.SetSpeed(SimulationSpeed.Paused);
                _savedTime = clock.Now;
                _savedCamera = school.CaptureCamera();
                _savedRooms = sim.Spaces.Count();
                _savedBalance = sim.Balance;
                _savedStaff = sim.Staff.Count;
                _savedChairs = sim.Chairs.Count;
                _savedFloors = sim.FloorCount;
                manager.SaveGame(SlotName, _savedCamera);
                GD.Print("[smoke] Game saved.");
                manager.LoadGame(SlotName);
                _stage = Stage.WaitForLoad;
                break;
            }

            case Stage.WaitForLoad when scene is HospitalScene school && school.IsNodeReady() && school.Session.Clock.Now == _savedTime:
            {
                var sim = school.Session.Simulation;
                Check(school.Session.SchoolName == SchoolName, "School name not restored.");
                Check(school.CaptureCamera() == _savedCamera, "Camera not restored.");
                Check(sim.Spaces.Count() == _savedRooms, "Built rooms not restored.");
                Check(sim.Balance == _savedBalance, "Balance not restored.");
                Check(sim.Accounts.Take(2).Sum(d => d[MoneyCategory.Cheats]) == CheatMoney, "The cheat money line was not restored.");
                Check(sim.Staff.Count == _savedStaff, $"Staff not restored ({sim.Staff.Count} of {_savedStaff}).");
                Check(sim.Chairs.Count == _savedChairs, "Waiting chairs not restored.");
                Check(sim.FloorCount == _savedFloors && _savedFloors > 1, $"Floors not restored ({sim.FloorCount} of {_savedFloors}).");
                Check(sim.Nav.Links.Any(), "Lifts and stairs not linked after loading.");
                SaveManager.Delete(SlotName);
                GD.Print("[smoke] Save reloaded and verified.");
                GD.Print("[smoke] PASSED");
                _stage = Stage.Done;
                GetTree().Quit(0);
                break;
            }
        }
    }

    private static GridCoord At(int x, int y) => new(B.X + x, B.Y + y);

    /// <summary>Uses the build tool like a player: the check must pass, then it is confirmed.</summary>
    private static void Do(HospitalScene school, string what)
    {
        var tool = school.BuildTool;
        Check(tool.Check is { CanBuild: true }, $"{what} refused: " + string.Join("; ", tool.Check?.Problems ?? Array.Empty<string>()));
        tool.Confirm();
    }

    /// <summary>The main corridor from the side entrance along the building, and a short link south towards the front.</summary>
    private static void LayCorridors(HospitalScene school)
    {
        var sim = school.Session.Simulation;
        var tool = school.BuildTool;
        tool.StartRoom(sim.Database.RoomTemplates["corridor"]);
        tool.PreviewDrag(At(4, 9), At(59, 16));
        Do(school, "The main corridor");
        tool.StartRoom(sim.Database.RoomTemplates["corridor"]);
        tool.PreviewDrag(At(22, 17), At(29, 25));
        Do(school, "The corridor down to reception");
        tool.Cancel();
        Check(sim.SpacesOfKind(CareSpaceKind.Corridor).Count() == 2, "The corridors were not laid.");
        GD.Print($"[smoke] Corridors laid; balance £{sim.Balance:N0}.");
    }

    /// <summary>
    /// Builds reception with the build tools, like a player: drag out the room, place the desk, the
    /// entrance on the front path, a door onto the corridor, then rows of chairs.
    /// </summary>
    private static void FitOutReception(HospitalScene school)
    {
        var sim = school.Session.Simulation;
        var tool = school.BuildTool;
        tool.StartRoom(sim.Database.RoomTemplates["waiting_room"]);
        tool.PreviewDrag(At(15, 26), At(36, 39));
        Do(school, "Reception");
        var room = sim.SpacesOfKind(CareSpaceKind.Waiting).Single();
        Check(room.Doors.Count == 0 && sim.DeskOf(room) is null, "A new reception should be empty.");

        tool.StartDesk();
        tool.PreviewAt(At(30, 32));
        Do(school, "The reception desk");
        tool.StartDoors();
        tool.PreviewAt(At(26, 39));
        Do(school, "The entrance door");
        tool.PreviewAt(At(26, 26));
        Do(school, "The door onto the corridor");

        tool.StartChairs(ChairColours.All[1].Hex);
        tool.FaceChairs(WallSide.East);
        foreach (var x in new[] { 18, 21 })
        {
            tool.PreviewDrag(At(x, 28), At(x, 36));
            Do(school, "A row of chairs");
        }

        tool.Cancel();
        tool.FollowMouse = true;
        Check(sim.WaitingRoomProblems(sim.FindSpace(room.Id)!).Count == 0, "Reception is not ready: " + string.Join(", ", sim.WaitingRoomProblems(sim.FindSpace(room.Id)!)));
        GD.Print($"[smoke] Reception fitted out: desk, 2 doors, {sim.Chairs.Count} chairs; balance £{sim.Balance:N0}.");
    }

    /// <summary>A staff room north of the main corridor.</summary>
    private static void BuildStaffRoom(HospitalScene school)
    {
        var sim = school.Session.Simulation;
        var tool = school.BuildTool;
        tool.StartRoom(sim.Database.RoomTemplates["staff_room"]);
        tool.PreviewDrag(At(30, 0), At(41, 8));
        Do(school, "The staff room");
        tool.Cancel();
        tool.FollowMouse = true;
        Check(sim.SpacesOfKind(CareSpaceKind.StaffRoom).Count() == 1, "The staff room was not built.");
    }

    /// <summary>One of each kind of staff on the day team, and a teacher on nights.</summary>
    private static void HireTeam(HospitalScene school)
    {
        var sim = school.Session.Simulation;
        foreach (var role in Enum.GetValues<StaffRole>())
        {
            sim.HireStaff(role, shift: StaffShift.Day);
        }

        sim.HireStaff(StaffRole.Teacher, shift: StaffShift.Night);

        // And one from the recruitment shortlist, as a player would.
        var candidate = sim.Candidates.FirstOrDefault(c => c.Role == StaffRole.Teacher) ?? sim.Candidates.First();
        Check(sim.Hire(candidate.Id, null, StaffShift.Day) is not null, "Could not hire from the shortlist.");
        var clock = school.Session.Clock;
        for (var i = 0; i < 2 * 3600; i++)
        {
            clock.Step();
        }

        var receptionist = sim.Staff.First(s => s.Role == StaffRole.Receptionist);
        var staffRoom = sim.SpacesOfKind(CareSpaceKind.StaffRoom).Single();
        Check(sim.MainWaitingRoom!.Rect.Contains(receptionist.Tile) || receptionist.IsOnBreak, $"The receptionist is not at reception ({receptionist.Tile}, {receptionist.StatusText}).");
        Check(sim.Staff.Any(s => s.Role == StaffRole.Teacher && staffRoom.Rect.Contains(s.Tile)), "No free teacher is waiting in the staff room.");
        Check(sim.Staff.Where(s => s.Shift == StaffShift.Night).All(s => !s.IsInBuilding), "The night team should be at home in the day.");
        GD.Print($"[smoke] Hired {sim.Staff.Count} staff; {sim.OnDutyStaff.Count()} on duty at {clock.Now:HH:mm}.");
    }

    /// <summary>Centres the camera on reception.</summary>
    private static void LookAtReception(HospitalScene school)
    {
        var room = school.Session.Simulation.MainWaitingRoom!.Rect;
        school.Camera.SnapTo(IsoDraw.ToScreen(room.X + (room.Width / 2.0), room.Y + (room.Length / 2.0)), 0.95f);
    }

    /// <summary>
    /// Every kind of staff member's panel must fit inside its dock and the screen.
    /// </summary>
    private static void CheckStaffPanelsFit(HospitalScene school)
    {
        var panel = school.Hud.StaffInfo;
        foreach (var member in school.Session.Simulation.Staff.GroupBy(m => m.Role).Select(g => g.First()))
        {
            school.Hud.ShowStaff(member);
            var width = panel.GetCombinedMinimumSize().X;
            GD.Print($"[smoke] Staff panel for {member.Role}: needs {width:0}, has {panel.Size.X:0}");
            Check(width <= panel.Size.X + 1 && panel.GetGlobalRect().End.X <= panel.GetViewportRect().Size.X + 1,
                $"The {member.Role} staff panel is wider than its dock ({width:0} > {panel.Size.X:0}).");
        }
    }

    /// <summary>Extends the building west, lays a corridor and previews a second staff room in the build tool.</summary>
    private static void StartBuilding(HospitalScene school)
    {
        var sim = school.Session.Simulation;
        var extension = sim.ExtendBuilding(new GridRect(B.X - 30, B.Y, 30, 34));
        Check(extension.CanBuild, "Extension refused: " + string.Join("; ", extension.Problems));
        var corridor = sim.BuildRoom(new RoomPlan("corridor", new GridRect(B.X - 30, B.Y + 9, 34, 8), WallSide.South));
        Check(corridor.CanBuild, "Corridor refused: " + string.Join("; ", corridor.Problems));

        school.SetMode(GameMode.Build);
        Check(school.Mode == GameMode.Build && school.Hud.Build.Visible, "Build mode did not open the build menu.");
        school.BuildTool.StartRoom(sim.Database.RoomTemplates["staff_room"]);
        school.BuildTool.PreviewDrag(new GridCoord(B.X - 12, B.Y), new GridCoord(B.X - 1, B.Y + 8));
        Check(school.BuildTool.Check is { CanBuild: true }, "Preview should be buildable: " + string.Join("; ", school.BuildTool.Check?.Problems ?? Array.Empty<string>()));
        school.Camera.SnapTo(IsoDraw.ToScreen(B.X - 2, B.Y + 8), 0.9f);
        GD.Print("[smoke] Building extended, corridor laid, staff room previewed.");
    }

    /// <summary>Build mode: clicking bare floor in the new room opens it, and clicking a member of staff does nothing.</summary>
    private static void ClickRoomFloor(HospitalScene school, CareSpace room)
    {
        Check(school.Mode == GameMode.Build, "Should still be in build mode.");
        var floor = Enumerable.Range(0, room.Rect.Width * room.Rect.Length)
            .Select(i => IsoDraw.ToScreen(room.Rect.X + (i % room.Rect.Width) + 0.5, room.Rect.Y + (i / room.Rect.Width) + 0.5))
            .First(p => school.ItemAt(p, bedsOnly: false) is null);
        school.ClickAt(floor);
        Check(school.Hud.RoomInfo.Visible && school.Hud.RoomInfo.RoomId == room.Id, "Clicking the room in build mode did not open it.");

        var member = school.Session.Simulation.Staff.First(m => m.IsInBuilding);
        school.ClickAt(school.World.FootPosition(member) + new Vector2(0, -30));
        Check(school.World.Selected is null && !school.Hud.StaffInfo.Visible, "Build mode should not select staff.");
        school.ClickAt(floor);
    }

    /// <summary>Build mode: clicking a piece of furniture in the new staff room opens it as an item.</summary>
    private static void ClickBuildItem(HospitalScene school)
    {
        var room = school.Session.Simulation.Spaces.Where(s => s.TemplateId == "staff_room").OrderBy(s => s.Rect.X).First();
        foreach (var item in room.Furniture)
        {
            var (x, y, w, l) = school.Footprint(room, item);
            school.ClickAt(IsoDraw.ToScreen(x + (w / 2), y + (l / 2)));
            if (school.Hud.ItemInfo.Visible && school.Hud.ItemInfo.Item == item)
            {
                return;
            }
        }

        Check(false, "Clicking furniture in build mode did not open it as an item.");
    }

    private void CheckBuildBar(HospitalScene school)
    {
        var bar = school.Hud.Build;
        Check(bar.Visible, "Build mode did not show the build bar.");
        var screen = GetViewport().GetVisibleRect().Size;
        var rect = bar.BarRect;
        Check(rect.Position.X >= MainHUD.NavWidth && rect.End.X <= screen.X && rect.End.Y <= screen.Y, $"The build bar does not fit on screen: {rect} on {screen}.");
        var listed = bar.Menu.Icons.SelectMany(i => i.Items).ToHashSet();
        foreach (var id in school.Session.Database.RoomTemplates.Keys.Where(id => !listed.Contains(id)))
        {
            Check(false, $"Room '{id}' cannot be built from the build bar.");
        }

        Check(bar.Menu.Icons.Any(i => i.Id == "school" && i.Items.Contains("staff_room")), "The build bar has no School icon with a staff room.");
        Check(bar.Menu.Icons.Any(i => i.Id == "facilities" && i.Items.Contains("public_toilets")), "The Facilities icon has no toilets.");
    }

    /// <summary>The open pop-up sits above the bar and inside the screen.</summary>
    private void CheckBuildPopup(HospitalScene school)
    {
        var bar = school.Hud.Build;
        var screen = GetViewport().GetVisibleRect().Size;
        var popup = bar.PopupRect;
        Check(bar.PopupOpen, "The build pop-up did not open.");
        Check(popup.Position.X >= 0 && popup.End.X <= screen.X && popup.End.Y <= bar.BarRect.Position.Y, $"The {bar.OpenIcon} pop-up does not fit: {popup} on {screen}.");
    }

    /// <summary>The Decorate tool: clicking the staff room opens its panel, and picks show in the world before they are paid for.</summary>
    private static void StartDecorating(HospitalScene school)
    {
        var sim = school.Session.Simulation;
        school.Hud.Build.Open(Simulation.Data.BuildMenu.Decorate);
        Check(school.Hud.Build.Decorating, "The Decorate icon did not open the Decorate tool.");
        var room = sim.Spaces.First(s => s.Kind == CareSpaceKind.StaffRoom);
        var (cx, cy) = (room.Rect.X + (room.Rect.Width / 2.0), room.Rect.Y + (room.Rect.Length / 2.0));
        school.Camera.SnapTo(IsoDraw.ToScreen(cx, cy), 1.4f);
        school.ClickAt(IsoDraw.ToScreen(cx, cy));
        Check(school.Hud.Decorate.Visible && school.Hud.Decorate.RoomId == room.Id, "Clicking a room with the Decorate tool did not open its Decorate panel.");
        Check(!school.Hud.RoomInfo.Visible, "The Decorate tool should open the Decorate panel, not the room panel.");
        var before = sim.DecorOf(room);
        school.Hud.Decorate.PickFloor(FloorFinish.CarpetTiles, "#8FA7B8");
        school.Hud.Decorate.PickWalls(WallFinish.Stripe, "#DCEBF5");
        Check(sim.DecorOf(room) == before, "Picking a finish should not pay for it before Apply.");
        Check(DecorArt.Preview is { } preview && preview.RoomId == room.Id, "Picking a finish should show it in the world.");
    }

    private static void FinishDecorating(HospitalScene school)
    {
        var sim = school.Session.Simulation;
        var room = sim.FindSpace(school.Hud.Decorate.RoomId!)!;
        var balance = sim.Balance;
        var cost = sim.DecorateCost(room, school.Hud.Decorate.Draft);
        school.Hud.Decorate.Apply();
        Check(sim.DecorOf(room).Floor == FloorFinish.CarpetTiles && sim.DecorOf(room).Walls == WallFinish.Stripe, "Apply did not redecorate the room.");
        Check(cost > 0 && sim.Balance == balance - cost, $"Redecorating should cost £{cost:N0}.");
        Check(DecorArt.Preview is null, "After Apply nothing should be left on trial.");
        school.Hud.Build.ClosePopup();
    }

    private static void OpenRota(HospitalScene school)
    {
        var sim = school.Session.Simulation;
        Check(sim.Staff.Any(s => s.Shift != sim.CurrentShift && !s.IsInBuilding), "The other shift's team should be at home.");
        var staffRoom = sim.SpacesOfKind(CareSpaceKind.StaffRoom).First();
        var receptionist = sim.OnDutyStaff.First(s => s.Role == StaffRole.Receptionist);
        Check(!sim.MoveStaff(receptionist.Id, staffRoom.Id), "A receptionist can only work for the whole school.");
        foreach (var member in sim.OnDutyStaff.Take(3))
        {
            member.Breaks.Insert(0, new PlannedBreak { Start = sim.Now.AddMinutes(-40), Minutes = 15, TakenAt = sim.Now.AddMinutes(-40), Done = true });
        }

        school.Hud.ToggleRota();
        Check(school.Hud.Rota.Visible, "The rota did not open.");
        school.Hud.Rota.Select(sim.OnDutyStaff.FirstOrDefault(s => s.Role == StaffRole.Teacher) ?? sim.Staff.First());
        GD.Print($"[smoke] Rota: {sim.OnDutyStaff.Count()} on shift, {sim.Staff.Count(s => !s.IsOnDuty)} off duty.");
    }

    /// <summary>Ctrl+C opens the cheat console; "money" adds to the balance under a Cheats line.</summary>
    private static void CheckCheatConsole(HospitalScene school)
    {
        PressCtrlC(school);
        Check(school.Hud.Cheats.Visible, "Ctrl+C did not open the cheat console.");
        var sim = school.Session.Simulation;
        var before = sim.Balance;
        var result = school.Hud.Cheats.Run($"money {CheatMoney}");
        Check(result.Succeeded && sim.Balance == before + CheatMoney, "The money cheat did not add to the balance: " + result.Message);
        Check(sim.Today[MoneyCategory.Cheats] == CheatMoney, "The money cheat was not booked as a Cheats line.");
        Check(!school.Hud.Cheats.Run("money lots").Succeeded && sim.Balance == before + CheatMoney, "A bad money command should change nothing.");
        school.Hud.Cheats.Run($"money {CheatMoney}");
        school.Hud.Cheats.Run($"money -{CheatMoney}");
        Check(sim.Balance == before + CheatMoney, "Taking money away with the cheat did not work.");
    }

    private static void PressCtrlC(HospitalScene school)
    {
        var press = new InputEventKey { Keycode = Key.C, PhysicalKeycode = Key.C, CtrlPressed = true, Pressed = true };
        school.GetViewport().PushInput(press);
        var release = new InputEventKey { Keycode = Key.C, PhysicalKeycode = Key.C, CtrlPressed = true, Pressed = false };
        school.GetViewport().PushInput(release);
    }

    /// <summary>
    /// A small wing west of the first extension, with a toilet block and an accessible toilet either side of
    /// a corridor off the canteen corridor. Leaves a wet patch on the corridor floor to check it draws.
    /// </summary>
    private static void BuildToilets(HospitalScene school)
    {
        var sim = school.Session.Simulation;
        school.Hud.HideRightPanels();
        sim.AdjustBalance(500_000);
        var wing = sim.ExtendBuilding(new GridRect(B.X - 40, B.Y + 20, 10, 24));
        Check(wing.CanBuild, "Toilet wing refused: " + string.Join("; ", wing.Problems));
        var corridor = sim.BuildRoom(new RoomPlan("corridor", new GridRect(B.X - 40, B.Y + 32, 10, 4), WallSide.South));
        Check(corridor.CanBuild, "Toilet corridor refused: " + string.Join("; ", corridor.Problems));

        foreach (var (template, rect) in new[]
        {
            ("public_toilets", new GridRect(B.X - 40, B.Y + 36, 10, 8)),
            ("accessible_toilet", new GridRect(B.X - 40, B.Y + 27, 5, 5)),
        })
        {
            var built = sim.BuildRoom(sim.AutoPlan(template, rect));
            Check(built.CanBuild, $"{template} refused: " + string.Join("; ", built.Problems));
        }

        var block = sim.SpacesOfKind(CareSpaceKind.Toilets).Single();
        Check(block.ToiletSpots.Count >= 2, $"The toilet block has only {block.ToiletSpots.Count} cubicles.");
        Check(block.SinkSpots.Count >= 1, "The toilet block has no basins.");
        Check(sim.SpacesOfKind(CareSpaceKind.AccessibleToilet).Single().ToiletSpots.Count == 1, "The accessible toilet has no toilet.");
        Check(sim.HasPublicToilets, "The school should have toilets now.");
        sim.SpillForTest(new GridCoord(B.X - 37, B.Y + 33));
        Check(sim.FloorAccidents.Count == 1, "The wet patch wasn't left on the floor.");
        GD.Print($"[smoke] Toilets: {block.ToiletSpots.Count} cubicles, {block.SinkSpots.Count} basins.");
        school.SetMode(GameMode.Play);
        school.ClearSelectionForTest();
        school.Camera.SnapTo(IsoDraw.ToScreen(block.Rect.X + 5, block.Rect.Y + 1), 1.15f);
    }

    /// <summary>A canteen and a vending point in a wing south of the first extension, reached by a corridor down from it.</summary>
    private static void BuildCanteen(HospitalScene school)
    {
        var sim = school.Session.Simulation;
        sim.AdjustBalance(1_000_000);
        Check(sim.IsUnlocked("facilities"), "Facilities should be open from the start.");
        var wing = sim.ExtendBuilding(new GridRect(B.X - 30, B.Y + 34, 26, 16));
        Check(wing.CanBuild, "Canteen wing refused: " + string.Join("; ", wing.Problems));
        foreach (var rect in new[] { new GridRect(B.X - 30, B.Y + 17, 4, 15), new GridRect(B.X - 30, B.Y + 32, 26, 4) })
        {
            var corridor = sim.BuildRoom(new RoomPlan("corridor", rect, WallSide.South));
            Check(corridor.CanBuild, $"Canteen corridor {rect} refused: " + string.Join("; ", corridor.Problems));
        }
        foreach (var (template, rect) in new[]
        {
            ("cafeteria", new GridRect(B.X - 30, B.Y + 36, 16, 14)),
            ("vending_point", new GridRect(B.X - 13, B.Y + 36, 8, 6)),
        })
        {
            var built = sim.BuildRoom(sim.AutoPlan(template, rect));
            Check(built.CanBuild, $"{template} refused: " + string.Join("; ", built.Problems));
        }

        var canteen = sim.SpacesOfKind(CareSpaceKind.Cafeteria).Single();
        Check(canteen.Seats.Count >= 8, $"The canteen only has {canteen.Seats.Count} chairs.");
        Check(sim.SpacesOfKind(CareSpaceKind.VendingPoint).Single().VendingSpots.Count >= 2, "The vending point has fewer than two machines.");
        GD.Print($"[smoke] Canteen: {canteen.Seats.Count} seats.");
        school.Camera.SnapTo(IsoDraw.ToScreen(canteen.Rect.X + (canteen.Rect.Width / 2.0), canteen.Rect.Y + (canteen.Rect.Length / 2.0)), 1.2f);
    }

    /// <summary>
    /// Adds a first floor with stairs and a lift off the main corridor, a corridor and a staff room upstairs,
    /// and runs the clock until a member of staff has walked up there.
    /// </summary>
    private static void BuildUpperFloor(HospitalScene school)
    {
        var sim = school.Session.Simulation;
        sim.AdjustBalance(2_000_000);
        var floor = sim.AddFloor();
        Check(floor.CanBuild, "First floor refused: " + string.Join("; ", floor.Problems));
        Check(sim.FloorCount == 2, "The building should have two floors.");

        foreach (var (template, rect) in new[] { ("lift", new GridRect(B.X + 48, B.Y + 17, 5, 6)), ("stairs", new GridRect(B.X + 54, B.Y + 17, 5, 8)) })
        {
            var built = sim.BuildRoom(sim.AutoPlan(template, rect));
            Check(built.CanBuild, $"{template} refused: " + string.Join("; ", built.Problems));
        }

        var upCorridor = sim.Site.OnFloor(new GridRect(B.X + 4, B.Y + 9, 56, 8), 1);
        var corridor = sim.BuildRoom(new RoomPlan("corridor", upCorridor, WallSide.South));
        Check(corridor.CanBuild, "Upstairs corridor refused: " + string.Join("; ", corridor.Problems));
        var room = sim.BuildRoom(sim.AutoPlan("staff_room", sim.Site.OnFloor(new GridRect(B.X + 20, B.Y + 17, 14, 10), 1)));
        Check(room.CanBuild, "Upstairs staff room refused: " + string.Join("; ", room.Problems));

        var clock = school.Session.Clock;
        StaffMember? climber = null;
        for (var i = 0; i < 2 * 3600 && climber is null; i++)
        {
            clock.Step();
            climber = sim.Staff.FirstOrDefault(m => m.IsInBuilding && sim.FloorOf(m) == 1);
        }

        Check(climber is not null, "No member of staff made it up to the first floor.");
        GD.Print($"[smoke] {climber!.Name} went up to the first floor at {clock.Now:HH:mm}.");
        clock.SetSpeed(SimulationSpeed.Paused);
        school.ShowFloor(1);
        Check(IsoDraw.ViewFloor == 1, "Did not show the first floor.");
        school.Camera.SnapTo(school.World.FootPosition(climber), 0.9f);
    }

    /// <summary>Saves the current frame when run with "--screenshots=DIR" (needs a real display, not --headless).</summary>
    private void Screenshot(string name)
    {
        if (_screenshotDirectory is null || DisplayServer.GetName() == "headless")
        {
            return;
        }

        DirAccess.MakeDirRecursiveAbsolute(_screenshotDirectory);
        var image = GetViewport().GetTexture().GetImage();
        var path = $"{_screenshotDirectory}/{name}.png";
        image.SavePng(path);
        GD.Print($"[smoke] Screenshot saved to {path}");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private void Fail(string message)
    {
        if (_stage == Stage.Done)
        {
            return;
        }

        _stage = Stage.Done;
        GD.PrintErr("[smoke] FAILED: " + message);
        GetTree().Quit(1);
    }
}
