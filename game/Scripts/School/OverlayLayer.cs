using System;
using System.Linq;
using BlueprintSchool.Simulation.Rooms;
using BlueprintSchool.Simulation.Staff;
using Godot;

namespace BlueprintSchool.School;

/// <summary>
/// Labels drawn above the world: small bubbles saying what each busy member of staff is doing.
/// </summary>
public partial class OverlayLayer : Node2D
{
    /// <summary>Below this zoom, staff bubbles are hidden to keep the view readable.</summary>
    private const float BubbleZoom = 0.5f;

    private WorldLayer _world = null!;
    private Font _font = null!;

    public void Initialise(WorldLayer world)
    {
        _world = world;
        _font = ThemeDB.FallbackFont;
    }

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        if (_world is null)
        {
            return;
        }

        var sim = _world.Simulation;
        var zoom = GetViewport().GetCanvasTransform().X.X;
        foreach (var member in sim.Staff)
        {
            if (member.Activity == StaffActivity.Working && _world.OnViewFloor(member) && (zoom >= BubbleZoom || ReferenceEquals(_world.Selected, member)))
            {
                StaffBubble(member);
            }
        }
    }

    private void StaffBubble(StaffMember member)
    {
        var text = member.StatusText;
        const int size = 14;
        var width = _font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X + 12;
        var foot = _world.FootPosition(member);
        var top = foot + new Vector2(0, _world.IsSeated(member) ? -78 : -84);
        var box = new Rect2(top - new Vector2(width / 2, 13), new Vector2(width, 21));

        DrawRect(box, new Color(1, 1, 1, 0.95f));
        DrawRect(new Rect2(box.Position, new Vector2(4, box.Size.Y)), new Color("#2BB3A3"));
        DrawColoredPolygon(new[] { top + new Vector2(-5, 8), top + new Vector2(5, 8), top + new Vector2(0, 14) }, new Color(1, 1, 1, 0.95f));
        DrawString(_font, box.Position + new Vector2(9, 16), text, HorizontalAlignment.Left, -1, size, new Color("#1D2B36"));
    }
}
