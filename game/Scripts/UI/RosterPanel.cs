using System;
using System.Linq;
using BlueprintSchool.Simulation.Simulation;
using BlueprintSchool.Simulation.Staff;
using Godot;

namespace BlueprintSchool.UI;

/// <summary>
/// A list of everyone on the staff, with what they are doing and how tired they are.
/// Clicking a row selects that person.
/// </summary>
public partial class RosterPanel : PanelContainer
{
    private readonly VBoxContainer _list = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private SchoolSimulation _sim = null!;
    private double _refresh;
    private string _signature = string.Empty;

    public event Action<StaffMember>? StaffClicked;

    public void Initialise(SchoolSimulation simulation)
    {
        _sim = simulation;
        Theme = UiStyle.Theme;
        MouseFilter = MouseFilterEnum.Stop;
        AddThemeStyleboxOverride("panel", HudStyle.DarkPanel());
        CustomMinimumSize = new Vector2(340, 0);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 6);
        AddChild(column);
        column.AddChild(UiStyle.Heading("Staff", 18));

        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        _list.AddThemeConstantOverride("separation", 3);
        scroll.AddChild(_list);
        column.AddChild(scroll);
    }

    public override void _Process(double delta)
    {
        if (!Visible)
        {
            return;
        }

        _refresh -= delta;
        if (_refresh > 0)
        {
            return;
        }

        _refresh = 1.0;
        BuildStaff();
    }

    /// <summary>Stripe colour for each kind of staff.</summary>
    public static Color RoleColour(StaffRole role) => role switch
    {
        StaffRole.Teacher => new Color("#1E6B47"),
        StaffRole.TeachingAssistant => new Color("#1F5FBF"),
        StaffRole.Receptionist => new Color("#6E5A8C"),
        _ => new Color("#3C6E47"),
    };

    private void BuildStaff()
    {
        var rows = _sim.Staff.Select(m =>
        {
            var tired = m.Fatigue >= 0.75 ? "exhausted" : m.Fatigue >= 0.5 ? "tired" : "fresh";
            return (Member: m, Text: $"{m.Name}  ·  {m.Grade}\n{m.StatusText}  ·  {tired} ({m.Fatigue:P0})");
        }).ToList();

        Rebuild(string.Join("|", rows.Select(r => r.Text)), () =>
        {
            if (rows.Count == 0)
            {
                AddNote("No staff yet. Hire some from Recruitment.");
            }

            foreach (var (member, text) in rows)
            {
                AddRow(RoleColour(member.Role), text, () => StaffClicked?.Invoke(member));
            }
        });
    }
    private void Rebuild(string signature, Action build)
    {
        if (signature == _signature)
        {
            return;
        }

        _signature = signature;
        foreach (var child in _list.GetChildren())
        {
            child.QueueFree();
        }

        build();
    }

    private void AddRow(Color stripeColour, string text, Action onPressed)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);
        row.AddChild(new ColorRect { CustomMinimumSize = new Vector2(6, 0), Color = stripeColour });
        row.AddChild(HudStyle.RowButton(text, onPressed));
        _list.AddChild(row);
    }

    private void AddNote(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeColorOverride("font_color", new Color("#5B6B79"));
        _list.AddChild(label);
    }
}
