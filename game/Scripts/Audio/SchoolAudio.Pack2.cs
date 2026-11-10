using System;
using System.Collections.Generic;
using System.Linq;
using BlueprintSchool.School;
using BlueprintSchool.Simulation.Rooms;
using BlueprintSchool.Simulation.Simulation;
using BlueprintSchool.Simulation.Staff;
using Godot;

namespace BlueprintSchool.Audio;

/// <summary>
/// Sound pack 2: the staff room on a break and corridor life (lifts and fire doors), plus tab and slider clicks.
/// </summary>
public partial class SchoolAudio
{
    private readonly Dictionary<string, double> _nextRoomSound = new();
    private double _nextCorridorSound = 20;

    private bool IsRainyDay()
    {
        var date = _session.Clock.Now.Date;
        return ((date.DayOfYear * 7919) + date.Year) % 10 < 3;
    }

    private void UpdatePack2(float zoom)
    {
        var sim = _session.Simulation;
        foreach (var space in sim.Spaces)
        {
            var r = space.Rect;
            var centre = IsoDraw.ToScreen(r.X + (r.Width / 2.0), r.Y + (r.Length / 2.0));
            var staffHere = sim.Staff.Count(s => !s.IsMoving && r.Contains(s.Tile));
            if (space.Kind == RoomKind.StaffRoom && sim.Staff.Any(s => s.Activity == StaffActivity.OnBreak && r.Contains(s.Tile)))
            {
                Want($"staffroom:{space.Id}", "amb_staff_room", centre, Spatial(centre, zoom, 0.6, 1.3) * 0.5, AudioSettings.AmbienceBus);
            }

            if (space.Kind == RoomKind.StaffRoom && staffHere >= 2)
            {
                Everyso(space.Id + ":chat", 12, 30, () => PlayAt(_random.Next(2) == 0 ? "staff_chat_1" : "staff_chat_2", centre, Spatial(centre, zoom, 0.9, 1.5) * 0.5, AudioSettings.EffectsBus));
            }
        }

        UpdateCorridors(zoom);
    }

    /// <summary>Lifts and fire doors somewhere along the corridors, when zoomed in.</summary>
    private void UpdateCorridors(float zoom)
    {
        if (_time < _nextCorridorSound)
        {
            return;
        }

        _nextCorridorSound = _time + 35 + (_random.NextDouble() * 55);
        var corridors = _session.Simulation.Spaces.Where(s => s.Kind == RoomKind.Corridor).ToList();
        if (corridors.Count == 0 || zoom < 0.9f || _session.Clock.IsPaused)
        {
            return;
        }

        var c = corridors[_random.Next(corridors.Count)].Rect;
        var pos = IsoDraw.ToScreen(c.X + (_random.NextDouble() * c.Width), c.Y + (_random.NextDouble() * c.Length));
        PlayAt(_random.Next(3) == 0 ? "lift_ding" : "fire_door", pos, Spatial(pos, zoom, 0.9, 1.5) * 0.45, AudioSettings.EffectsBus);
    }

    /// <summary>Runs <paramref name="play"/> at random intervals between the two limits (real seconds).</summary>
    private void Everyso(string key, double min, double max, Action play)
    {
        if (!_nextRoomSound.TryGetValue(key, out var next) || next < _time - 5)
        {
            // First sight of this room, or it has been quiet for a while: wait a random part of the interval.
            _nextRoomSound[key] = _time + (_random.NextDouble() * max);
            return;
        }

        if (_time >= next && !_session.Clock.IsPaused)
        {
            play();
            _nextRoomSound[key] = _time + min + (_random.NextDouble() * (max - min));
        }
    }

    private void HookControl(Node node)
    {
        if (node.HasMeta("click_sound"))
        {
            return;
        }

        node.SetMeta("click_sound", true);
        switch (node)
        {
            case TabBar bar:
                bar.TabChanged += _ => PlayTab();
                break;
            case TabContainer tabs:
                tabs.TabChanged += _ => PlayTab();
                break;
            case Godot.Range range and (Slider or SpinBox):
                range.ValueChanged += _ =>
                {
                    if (IsInstanceValid(this) && OffCooldown("tick", 0.06))
                    {
                        PlayUi("ui_tick", 0.5);
                    }
                };
                break;
        }
    }

    private void PlayTab()
    {
        if (IsInstanceValid(this) && OffCooldown("tab", 0.05))
        {
            PlayUi("ui_tab", 0.6);
        }
    }
}
