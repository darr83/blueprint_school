using System;
using System.Collections.Generic;
using System.Linq;
using BlueprintSchool.Core;
using BlueprintSchool.School;
using BlueprintSchool.Simulation.Agents;
using BlueprintSchool.Simulation.Audio;
using BlueprintSchool.Simulation.Core;
using BlueprintSchool.Simulation.Rooms;
using BlueprintSchool.Simulation.Simulation;
using BlueprintSchool.UI;
using BlueprintSchool.Simulation.Data.Models;
using BlueprintSchool.Simulation.SchoolDay;
using Godot;

namespace BlueprintSchool.Audio;

/// <summary>
/// All the school's sound. It watches the simulation and the camera each frame and plays:
/// background ambience; chatter and footsteps that get louder as you zoom in near them;
/// building, hiring and money cues; and interface sounds. It only reads the game, so it can be
/// added or removed without changing anything else.
/// </summary>
public partial class SchoolAudio : Node2D
{
    private const int MaxOneShots = 14;
    private const double FadeSpeed = 3.0;

    private static readonly string[] Gibberish =
    {
        "gibberish_question", "gibberish_complaint", "gibberish_thanks", "gibberish_laugh",
        "gibberish_angry", "gibberish_scared", "gibberish_relieved", "sigh",
    };

    private readonly Dictionary<string, Loop> _loops = new();
    private readonly List<AudioStreamPlayer2D> _oneShots = new();
    private readonly Dictionary<string, double> _cooldowns = new();
    private readonly List<IDisposable> _subscriptions = new();
    private readonly Random _random = new();

    /// <summary>Prints every sound as it starts, when the game is run with -- --log-sounds.</summary>
    private static readonly bool LogSounds = OS.GetCmdlineUserArgs().Contains("--log-sounds");

    private GameSession _session = null!;
    private WorldLayer _world = null!;
    private double _time;
    private double _nextChatter = 3;
    private int _roomCount = -1;
    private int _furnitureCount = -1;
    private int _staffCount = -1;
    private long _lastBalance = long.MinValue;

    public void Initialise(GameSession session, WorldLayer world)
    {
        _session = session;
        _world = world;
        AudioSettings.Current.Apply();

        var announcer = new Announcer();
        AddChild(announcer);
        _subscriptions.Add(session.Events.Subscribe<Announcement>(announcer.Enqueue));
        _subscriptions.Add(session.Events.Subscribe<Notification>(OnNotification));
        _subscriptions.Add(session.Events.Subscribe<NewDayStarted>(_ => PlayUi("new_day", 0.6)));

        // The school bell: a full ring for lessons and the end of the day, a short one for tutor time, break and lunch.
        _subscriptions.Add(session.Events.Subscribe<BellRang>(b => PlayFlat(
            b.Period.Kind is BellPeriodKind.Lesson or BellPeriodKind.AfterSchool ? "school_bell" : "school_bell_short", 0.8, AudioSettings.EffectsBus)));

        // Every button in the game clicks, and every panel whooshes open.
        GetTree().NodeAdded += OnNodeAdded;
        HookButtons(GetTree().Root);
    }

    public override void _ExitTree()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        if (IsInstanceValid(this) && GetTree() is { } tree)
        {
            tree.NodeAdded -= OnNodeAdded;
        }
    }

    public override void _Process(double delta)
    {
        if (_session is null)
        {
            return;
        }

        _time += delta;
        var zoom = GetViewport().GetCanvasTransform().X.X;

        UpdateAmbience(zoom);
        UpdateRooms(zoom);
        UpdateMovement(zoom);
        UpdateRandomVoices(zoom);
        UpdateBuildingAndMoney();
        UpdatePack2(zoom);
        UpdateLoops(delta);
        _oneShots.RemoveAll(p => !IsInstanceValid(p));
    }

    // ---------------------------------------------------------------- ambience

    private void UpdateAmbience(float zoom)
    {
        var hour = _session.Clock.Now.Hour;
        var night = hour >= 22 || hour < 7;

        // Inside: steady, a little louder as you get closer; quieter overnight.
        var inside = (0.55 + (0.45 * SoundFalloff.ZoomGain(zoom, 0.3, 1.0))) * (night ? 0.55 : 1.0);
        // Overnight, the proper night loop takes over from the muffled day one when it's there.
        var nightLoop = night && SoundLibrary.Has("amb_hospital_night");
        WantFlat(nightLoop ? "amb_hospital_night" : "amb_hospital_day", nightLoop ? inside / 0.55 * 0.8 : inside, AudioSettings.AmbienceBus);
        if (IsRainyDay())
        {
            WantFlat("amb_rain", 0.35, AudioSettings.AmbienceBus);
        }

        // Outside: the car park, mainly when you're zoomed right out.
        var outside = 0.6 * (1 - SoundFalloff.ZoomGain(zoom, 0.25, 0.7));
        WantFlat("amb_outside", outside, AudioSettings.AmbienceBus);
    }

    private void UpdateRooms(float zoom)
    {
        var sim = _session.Simulation;
        foreach (var space in sim.Spaces)
        {
            var r = space.Rect;
            var centre = IsoDraw.ToScreen(r.X + (r.Width / 2.0), r.Y + (r.Length / 2.0));
            if (space.Kind == RoomKind.Waiting && sim.Staff.Any(m => !m.IsMoving && r.Contains(m.Tile)))
            {
                Want($"chat:{space.Id}", "chatter_reception", centre, Spatial(centre, zoom, 0.8, 1.4) * 0.6, AudioSettings.EffectsBus);
            }
        }
    }

    // ---------------------------------------------------------------- people moving

    private void UpdateMovement(float zoom)
    {
        if (zoom < 1.0f)
        {
            return;
        }

        var sim = _session.Simulation;
        var walker = sim.Staff
            .Where(s => s.IsMoving)
            .Select(s => (Staff: s, Pos: _world.FootPosition(s)))
            .Select(x => (x.Staff, x.Pos, Gain: Spatial(x.Pos, zoom, 1.3, 2.0)))
            .Where(x => x.Gain > 0.05)
            .OrderByDescending(x => x.Gain)
            .FirstOrDefault();
        if (walker.Staff is not null)
        {
            Want("footsteps", "footsteps", walker.Pos, walker.Gain * 0.35, AudioSettings.EffectsBus);
        }
    }

    private void UpdateRandomVoices(float zoom)
    {
        if (_time < _nextChatter)
        {
            return;
        }

        _nextChatter = _time + 3 + (_random.NextDouble() * 5);
        if (zoom < 0.9f || _session.Clock.IsPaused)
        {
            return;
        }

        var nearby = _session.Simulation.Staff
            .Where(m => m.IsInBuilding && _world.OnViewFloor(m))
            .Select(m => (Pos: _world.FootPosition(m), Gain: Spatial(_world.FootPosition(m), zoom, 0.9, 1.5)))
            .Where(x => x.Gain > 0.15)
            .ToList();
        if (nearby.Count == 0 || _random.NextDouble() < 0.5)
        {
            return;
        }

        var (pos, gain) = nearby[_random.Next(nearby.Count)];
        PlayAt(Gibberish[_random.Next(Gibberish.Length)], pos, gain * 0.5, AudioSettings.EffectsBus);
    }

    // ---------------------------------------------------------------- building, staff, money

    private void UpdateBuildingAndMoney()
    {
        var sim = _session.Simulation;
        var rooms = sim.Spaces.Count(s => s.Kind != RoomKind.Corridor);
        var furniture = sim.Spaces.Sum(s => s.Furniture.Count);
        var staff = sim.Staff.Count;

        if (_roomCount >= 0)
        {
            if (rooms > _roomCount)
            {
                PlayUi("room_built", 0.8);
            }
            else if (rooms < _roomCount)
            {
                PlayUi("demolish", 0.8);
            }
            else if (furniture > _furnitureCount)
            {
                PlayUi("furniture_placed", 0.8);
            }

            if (staff > _staffCount)
            {
                PlayUi("staff_hired", 0.7);
            }
            else if (staff < _staffCount)
            {
                PlayUi("staff_let_go", 0.7);
            }
        }

        (_roomCount, _furnitureCount, _staffCount) = (rooms, furniture, staff);

        var balance = sim.Balance;
        if (_lastBalance != long.MinValue && !sim.UnlimitedMoney)
        {
            if (balance - _lastBalance >= 1000 && OffCooldown("money_in", 4))
            {
                PlayUi("money_in", 0.4);
            }

            const long lowMoney = 50_000;
            if (balance < lowMoney && _lastBalance >= lowMoney)
            {
                PlayUi("money_low", 0.8);
            }
        }

        _lastBalance = balance;
    }

    // ---------------------------------------------------------------- events

    private void OnNotification(Notification n)
    {
        if (n.Text.StartsWith("Unlocked ", StringComparison.Ordinal))
        {
            PlayUi("department_unlocked", 0.8);
            return;
        }

        if (n.Level == NotificationLevel.Critical && OffCooldown("notify_urgent", 3))
        {
            PlayUi("notify_urgent", 0.7);
        }
        else if (n.Level == NotificationLevel.Warning && OffCooldown("notify", 2))
        {
            PlayUi("notify", 0.5);
        }
    }

    private void OnNodeAdded(Node node)
    {
        if (node is BaseButton button)
        {
            HookButton(button);
        }
        else if (node is TabBar or TabContainer or Godot.Range)
        {
            HookControl(node);
        }
        else if (node is ModalPanel && OffCooldown("panel", 0.3))
        {
            PlayUi("ui_panel_open", 0.6);
        }
    }

    private void HookButtons(Node node)
    {
        if (node is BaseButton button)
        {
            HookButton(button);
        }
        else if (node is TabBar or TabContainer or Godot.Range)
        {
            HookControl(node);
        }

        foreach (var child in node.GetChildren())
        {
            HookButtons(child);
        }
    }

    private void HookButton(BaseButton button)
    {
        if (button.HasMeta("click_sound"))
        {
            return;
        }

        button.SetMeta("click_sound", true);
        button.Pressed += () =>
        {
            if (IsInstanceValid(this) && OffCooldown("click", 0.05))
            {
                PlayUi("ui_click", 0.6);
            }
        };
    }

    // ---------------------------------------------------------------- playback helpers

    /// <summary>Loudness for a sound at a world position: zoom-in level times distance from the middle of the screen.</summary>
    private double Spatial(Vector2 worldPos, float zoom, double silentAt, double fullAt, double floor = 0)
    {
        var viewport = GetViewportRect().Size;
        var screen = GetViewport().GetCanvasTransform() * worldPos;
        var distance = screen.DistanceTo(viewport / 2);
        var radius = viewport.Length() / 2;
        return SoundFalloff.ZoomGain(zoom, silentAt, fullAt, floor) * SoundFalloff.DistanceGain(distance, radius * 0.45, radius * 1.3);
    }

    private bool OffCooldown(string key, double seconds)
    {
        if (_cooldowns.TryGetValue(key, out var until) && _time < until)
        {
            return false;
        }

        _cooldowns[key] = _time + seconds;
        return true;
    }

    private void PlayUi(string sound, double gain) => PlayFlat(sound, gain, AudioSettings.InterfaceBus);

    private void PlayFlat(string sound, double gain, string bus, double delay = 0) =>
        PlayAt(sound, null, gain, bus, delay);

    /// <summary>Plays a sound once, panned to <paramref name="worldPos"/> (or centred when null).</summary>
    private void PlayAt(string sound, Vector2? worldPos, double gain, string bus, double delay = 0, double maxSeconds = 0)
    {
        if (gain <= 0.02 || _oneShots.Count >= MaxOneShots || SoundLibrary.Get(sound) is not { } stream)
        {
            return;
        }

        if (LogSounds)
        {
            GD.Print($"[sound] {sound} gain {gain:0.00}");
        }

        var player = MakePlayer(stream, worldPos, bus);
        player.VolumeDb = (float)SoundFalloff.ToDecibels(gain);
        player.Finished += player.QueueFree;
        _oneShots.Add(player);

        if (delay > 0)
        {
            GetTree().CreateTimer(delay).Timeout += () =>
            {
                if (IsInstanceValid(player))
                {
                    player.Play();
                }
            };
        }
        else
        {
            player.Play();
        }

        if (maxSeconds > 0)
        {
            GetTree().CreateTimer(delay + maxSeconds).Timeout += () =>
            {
                if (IsInstanceValid(player))
                {
                    player.QueueFree();
                }
            };
        }
    }

    private AudioStreamPlayer2D MakePlayer(AudioStream stream, Vector2? worldPos, string bus)
    {
        // Distance fading is done here (from zoom and screen position), so the engine only pans.
        var player = new AudioStreamPlayer2D
        {
            Stream = stream,
            Bus = bus,
            Attenuation = 0.0001f,
            MaxDistance = 1_000_000,
            PanningStrength = worldPos is null ? 0 : 0.6f,
        };
        AddChild(player);
        player.GlobalPosition = worldPos ?? (GetViewport().GetCanvasTransform().AffineInverse() * (GetViewportRect().Size / 2));
        return player;
    }

    /// <summary>Asks for a looping sound this frame; loops not asked for fade out and stop.</summary>
    private void Want(string key, string sound, Vector2 worldPos, double gain, string bus)
    {
        if (gain <= 0.02 && !_loops.ContainsKey(key))
        {
            return;
        }

        if (!_loops.TryGetValue(key, out var loop))
        {
            if (SoundLibrary.Get(sound, loop: true) is not { } stream)
            {
                return;
            }

            if (LogSounds)
            {
                GD.Print($"[sound] loop {sound} ({key}) gain {gain:0.00}");
            }

            loop = new Loop(MakePlayer(stream, worldPos, bus), !key.StartsWith("flat:", StringComparison.Ordinal));
            loop.Player.VolumeDb = -80;
            loop.Player.Play(_random.NextSingle() * (float)stream.GetLength());
            _loops[key] = loop;
        }

        loop.Target = gain;
        loop.Wanted = true;
        if (loop.Positional)
        {
            loop.Player.GlobalPosition = worldPos;
        }
    }

    private void WantFlat(string sound, double gain, string bus)
    {
        var centre = GetViewport().GetCanvasTransform().AffineInverse() * (GetViewportRect().Size / 2);
        Want("flat:" + sound, sound, centre, gain, bus);
        if (_loops.TryGetValue("flat:" + sound, out var loop))
        {
            loop.Player.PanningStrength = 0;
            loop.Player.GlobalPosition = centre;
        }
    }

    private void UpdateLoops(double delta)
    {
        foreach (var (key, loop) in _loops.ToList())
        {
            var target = loop.Wanted ? loop.Target : 0;
            loop.Gain += (target - loop.Gain) * Math.Min(1, delta * FadeSpeed);
            loop.Player.VolumeDb = (float)SoundFalloff.ToDecibels(loop.Gain);
            if (!loop.Wanted && loop.Gain < 0.01)
            {
                loop.Player.QueueFree();
                _loops.Remove(key);
            }

            loop.Wanted = false;
        }
    }

    private sealed class Loop(AudioStreamPlayer2D player, bool positional)
    {
        public AudioStreamPlayer2D Player { get; } = player;

        public bool Positional { get; } = positional;

        public double Gain { get; set; }

        public double Target { get; set; }

        public bool Wanted { get; set; }
    }
}
