using System;
using System.Collections.Generic;
using System.Linq;
using BlueprintSchool.Simulation.Simulation;
using Godot;

namespace BlueprintSchool.UI;

/// <summary>
/// The Stats screen: every figure the school keeps, for the last 24 hours, 7 days, 28 days or since
/// opening. Headline tiles along the top, two charts, then a card per area (staff and money for now;
/// pupils, lessons and results come with later milestones).
/// </summary>
public partial class StatsPanel : PanelContainer
{
    private static readonly Color Hair = new("#E6ECF1");
    private static readonly Color Positive = new("#2E7D32");

    private readonly Dictionary<StatsPeriod, Button> _periodButtons = new();
    private readonly Dictionary<string, Label> _values = new();
    private readonly Dictionary<string, Label> _notes = new();
    private readonly List<(PanelContainer Tile, Label Value, Label Note)> _tiles = new();
    private readonly Label _caption = UiStyle.Caption(string.Empty, 13);
    private readonly Label _arrivalsTitle = HudWidgets.Section(string.Empty);
    private readonly Label _moneyTitle = HudWidgets.Section(string.Empty);
    private readonly StatsBarChart _arrivalsChart = new(UiStyle.Primary, UiStyle.Red);
    private readonly StatsBarChart _moneyChart = new(Positive, UiStyle.Red);
    private SchoolSimulation _sim = null!;
    private StatsPeriod _period = StatsPeriod.Last24Hours;
    private double _refresh;

    public StatsPeriod Period => _period;

    public void Initialise(SchoolSimulation simulation)
    {
        _sim = simulation;
        Theme = UiStyle.Theme;
        MouseFilter = MouseFilterEnum.Stop;
        AddThemeStyleboxOverride("panel", UiStyle.Card(20));

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 14);
        AddChild(column);
        column.AddChild(BuildHeader());

        var tiles = new HBoxContainer();
        tiles.AddThemeConstantOverride("separation", 10);
        foreach (var (caption, tip) in new[]
        {
            ("Staff on shift", "Average number of staff at work"),
            ("Hired", "Staff taken on"),
            ("Left", "Staff who left or were let go"),
            ("Money in less out", "Income less spending, cheats left out"),
        })
        {
            tiles.AddChild(Tile(caption, tip));
        }

        column.AddChild(tiles);

        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, SizeFlagsVertical = SizeFlags.ExpandFill };
        var body = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 14);
        scroll.AddChild(body);
        column.AddChild(scroll);

        var charts = new HBoxContainer();
        charts.AddThemeConstantOverride("separation", 14);
        charts.AddChild(ChartCard(_arrivalsTitle, _arrivalsChart));
        charts.AddChild(ChartCard(_moneyTitle, _moneyChart));
        body.AddChild(charts);

        var grid = new GridContainer { Columns = 3, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation", 14);
        grid.AddThemeConstantOverride("v_separation", 14);
        body.AddChild(grid);

        grid.AddChild(Card("Staff", new[]
        {
            ("on_shift", "Staff on shift"), ("hired", "Hired"), ("left_staff", "Left"),
        }));
        grid.AddChild(Card("Money", Enum.GetValues<MoneyCategory>().Select(c => ($"money_{c}", FinancePanel.CategoryName(c)))
            .Append(("income", "Money in")).Append(("spending", "Money out")).Append(("net", "Money in less out")).ToArray()));

        Refresh();
    }

    /// <summary>Shows a period (the buttons along the top, and the smoke test).</summary>
    public void ShowPeriod(StatsPeriod period)
    {
        _period = period;
        Refresh();
    }

    public override void _Process(double delta)
    {
        if (!Visible)
        {
            return;
        }

        _refresh -= delta;
        if (_refresh <= 0)
        {
            _refresh = 1;
            Refresh();
        }
    }

    // ---------------------------------------------------------------- layout

    private Control BuildHeader()
    {
        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 12);
        var titles = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        titles.AddThemeConstantOverride("separation", 0);
        var title = new Label { Text = "School stats" };
        title.AddThemeFontSizeOverride("font_size", 22);
        titles.AddChild(title);
        titles.AddChild(_caption);
        header.AddChild(titles);

        var periods = new HBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        periods.AddThemeConstantOverride("separation", 6);
        foreach (var (period, text) in new[]
        {
            (StatsPeriod.Last24Hours, "Last 24 hours"), (StatsPeriod.Last7Days, "7 days"),
            (StatsPeriod.Last28Days, "28 days"), (StatsPeriod.AllTime, "All time"),
        })
        {
            var button = UiStyle.SecondaryButton(text, () => ShowPeriod(period));
            button.ToggleMode = true;
            button.FocusMode = FocusModeEnum.None;
            _periodButtons[period] = button;
            periods.AddChild(button);
        }

        header.AddChild(periods);
        var close = UiStyle.SecondaryButton("✕", () => Visible = false);
        close.TooltipText = "Close (Esc)";
        close.CustomMinimumSize = new Vector2(40, 40);
        header.AddChild(close);
        return header;
    }

    private Control Tile(string caption, string tooltip)
    {
        var tile = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, TooltipText = tooltip, MouseFilter = MouseFilterEnum.Stop };
        var column = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        column.AddThemeConstantOverride("separation", 0);
        column.AddChild(UiStyle.Caption(caption, 12));
        var value = new Label { MouseFilter = MouseFilterEnum.Ignore };
        value.AddThemeFontSizeOverride("font_size", 24);
        column.AddChild(value);
        var note = UiStyle.Caption(string.Empty, 11);
        column.AddChild(note);
        tile.AddChild(column);
        _tiles.Add((tile, value, note));
        SetTone(_tiles[^1], Tone.Normal);
        return tile;
    }

    private enum Tone
    {
        Normal,
        Good,
        Warning,
        Danger,
    }

    private static void SetTone((PanelContainer Tile, Label Value, Label Note) tile, Tone tone)
    {
        var (background, border, text) = tone switch
        {
            Tone.Good => (new Color("#EDF7EE"), new Color("#A9D3AE"), Positive),
            Tone.Warning => (UiStyle.AmberTint, UiStyle.AmberBorder, UiStyle.AmberText),
            Tone.Danger => (new Color("#FDECEC"), new Color("#EBA5A8"), UiStyle.Red),
            _ => (UiStyle.Surface, Hair, UiStyle.TextDark),
        };
        var box = UiStyle.Box(background, 12, border);
        box.ContentMarginLeft = box.ContentMarginRight = 14;
        box.ContentMarginTop = box.ContentMarginBottom = 9;
        tile.Tile.AddThemeStyleboxOverride("panel", box);
        tile.Value.AddThemeColorOverride("font_color", text);
    }

    private static Control ChartCard(Label title, StatsBarChart chart)
    {
        var card = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        card.AddThemeStyleboxOverride("panel", UiStyle.Box(UiStyle.Surface, 12, Hair, 14));
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 8);
        column.AddChild(title);
        chart.CustomMinimumSize = new Vector2(0, 150);
        chart.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        column.AddChild(chart);
        card.AddChild(column);
        return card;
    }

    private PanelContainer Card(string title, (string Key, string Label)[] rows)
    {
        var card = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.Fill };
        card.AddThemeStyleboxOverride("panel", UiStyle.Box(UiStyle.Surface, 12, Hair, 14));
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 6);
        var heading = HudWidgets.Section(title);
        heading.AddThemeFontSizeOverride("font_size", 15);
        column.AddChild(heading);
        column.AddChild(HudWidgets.Divider());
        foreach (var (key, label) in rows)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);
            var name = new Label { Text = label, SizeFlagsHorizontal = SizeFlags.ExpandFill, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis };
            name.AddThemeFontSizeOverride("font_size", 14);
            name.AddThemeColorOverride("font_color", UiStyle.TextMuted);
            row.AddChild(name);
            var note = UiStyle.Caption(string.Empty, 12);
            row.AddChild(note);
            var value = new Label { HorizontalAlignment = HorizontalAlignment.Right, CustomMinimumSize = new Vector2(64, 0) };
            value.AddThemeFontSizeOverride("font_size", 15);
            value.AddThemeColorOverride("font_color", UiStyle.TextDark);
            row.AddChild(value);
            column.AddChild(row);
            _values[key] = value;
            _notes[key] = note;
        }

        card.AddChild(column);
        return card;
    }

    // ---------------------------------------------------------------- figures

    private void Refresh()
    {
        foreach (var (period, button) in _periodButtons)
        {
            button.SetPressedNoSignal(period == _period);
        }

        var history = _sim.History;
        var now = _sim.Now;
        double T(string key) => history.Total(_period, now, key);
        GaugeReadings G(string key) => history.Gauge(_period, now, key);

        var covered = history.Covered(_period, now);
        var name = _period switch
        {
            StatsPeriod.Last24Hours => "Last 24 hours",
            StatsPeriod.Last7Days => "Last 7 days",
            StatsPeriod.Last28Days => "Last 28 days",
            _ => "All time",
        };
        var full = _period == StatsPeriod.AllTime || covered >= (_period == StatsPeriod.Last24Hours ? TimeSpan.FromHours(24) : TimeSpan.FromDays(StatsHistory.DaysIn(_period)));
        _caption.Text = _period == StatsPeriod.AllTime
            ? $"All time · since {history.Since:ddd d MMM yyyy, HH:mm} ({Span(covered)})"
            : full ? $"{name} · up to {now:ddd d MMM, HH:mm}" : $"{name} · only {Span(covered)} so far";

        // Staff.
        var onShift = G(StatKeys.StaffOnDuty);
        Gauge("on_shift", onShift, v => $"{v:0.#}", v => $"{v:0}");
        var hired = T(StatKeys.Hired);
        var left = T(StatKeys.Left);
        SetValue("hired", Count(hired));
        SetValue("left_staff", Count(left), null, left > 0 ? UiStyle.AmberText : null);

        // Money.
        double income = 0, spending = 0;
        foreach (var category in Enum.GetValues<MoneyCategory>())
        {
            var amount = T(StatKeys.Money(category));
            SetValue($"money_{category}", Money(amount), null, amount > 0 ? Positive : amount < 0 ? UiStyle.Red : UiStyle.TextMuted);
            if (category == MoneyCategory.Cheats)
            {
                continue;
            }

            if (amount > 0)
            {
                income += amount;
            }
            else
            {
                spending += amount;
            }
        }

        SetValue("income", Money(income), null, Positive);
        SetValue("spending", Money(spending), null, spending < 0 ? UiStyle.Red : UiStyle.TextMuted);
        var net = income + spending;
        SetValue("net", Money(net), null, net >= 0 ? Positive : UiStyle.Red);

        // Headline tiles.
        SetTile(0, onShift.Count > 0 ? $"{onShift.Average:0.#}" : "–", onShift.Count > 0 ? $"busiest {onShift.Peak:0}" : string.Empty, Tone.Normal);
        SetTile(1, Count(hired), string.Empty, Tone.Normal);
        SetTile(2, Count(left), string.Empty, left > 0 ? Tone.Warning : Tone.Normal);
        SetTile(3, Money(net), $"{Money(income)} in · {Money(-spending)} out", net >= 0 ? Tone.Good : Tone.Danger);

        // Charts: hour by hour for the last day, day by day otherwise (all time shows the last 28 days).
        var byHour = _period == StatsPeriod.Last24Hours;
        var span = byHour ? "hour by hour" : _period == StatsPeriod.AllTime ? "day by day, last 28 days" : "day by day";
        _arrivalsTitle.Text = $"Staff hired, {span}";
        _moneyTitle.Text = $"Money in less out, {span}";
        string Label(DateTime start) => byHour ? start.ToString("HH:00") : start.ToString("d MMM");
        _arrivalsChart.SetData(history.Series(_period, now, StatKeys.Hired).Select(x => (Label(x.Start), x.Value)).ToList(), v => $"{v:0}");
        var moneyKeys = Enum.GetValues<MoneyCategory>().Where(c => c != MoneyCategory.Cheats).Select(StatKeys.Money).ToList();
        var moneySeries = history.Series(_period, now, moneyKeys[0]).Select(x => x.Start).ToList();
        var netSeries = moneyKeys.Select(k => history.Series(_period, now, k).Select(x => x.Value).ToList()).ToList();
        _moneyChart.SetData(moneySeries.Select((start, i) => (Label(start), netSeries.Sum(s => s[i]))).ToList(), v => Money(v));
    }

    private void SetTile(int index, string value, string note, Tone tone)
    {
        var tile = _tiles[index];
        tile.Value.Text = value;
        tile.Note.Text = note;
        SetTone(tile, tone);
    }

    private void SetValue(string key, string value, string? note = null, Color? colour = null)
    {
        var label = _values[key];
        label.Text = value;
        label.AddThemeColorOverride("font_color", colour ?? UiStyle.TextDark);
        _notes[key].Text = note ?? string.Empty;
    }

    /// <summary>A gauge: its average, with the busiest reading beside it.</summary>
    private void Gauge(string key, GaugeReadings readings, Func<double, string> average, Func<double, string> peak) =>
        SetValue(key, readings.Count > 0 ? average(readings.Average) : "–", readings.Count > 0 ? $"average · peak {peak(readings.Peak)}" : string.Empty);

    private static string Count(double value) => $"{value:N0}";

    private static string Money(double amount) => amount < 0 ? $"−£{-amount:N0}" : $"£{amount:N0}";

    private static string Span(TimeSpan span) => span.TotalDays >= 1
        ? $"{(int)span.TotalDays} day{((int)span.TotalDays == 1 ? string.Empty : "s")} {span.Hours} h"
        : span.TotalHours >= 1 ? $"{(int)span.TotalHours} h {span.Minutes:00} m" : $"{Math.Max(0, (int)span.TotalMinutes)} m";
}

/// <summary>A simple bar chart: one bar per hour or day, below the line in red when negative.</summary>
public partial class StatsBarChart : Control
{
    private readonly Color _positive;
    private readonly Color _negative;
    private List<(string Label, double Value)> _data = new();
    private Func<double, string> _format = v => $"{v:0}";
    private int _hover = -1;

    public StatsBarChart(Color positive, Color negative)
    {
        _positive = positive;
        _negative = negative;
        MouseFilter = MouseFilterEnum.Pass;
    }

    public StatsBarChart()
        : this(UiStyle.Primary, UiStyle.Red)
    {
    }

    public void SetData(List<(string Label, double Value)> data, Func<double, string> format)
    {
        _data = data;
        _format = format;
        QueueRedraw();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && _data.Count > 0)
        {
            var plot = Plot();
            var index = (int)((motion.Position.X - plot.Position.X) / (plot.Size.X / _data.Count));
            index = index >= 0 && index < _data.Count && plot.HasPoint(new Vector2(motion.Position.X, plot.Position.Y + 1)) ? index : -1;
            if (index != _hover)
            {
                _hover = index;
                TooltipText = index < 0 ? string.Empty : $"{_data[index].Label}: {_format(_data[index].Value)}";
                QueueRedraw();
            }
        }
    }

    /// <summary>Short axis figures: 950, 12k, 1.5m (money keeps its pound sign).</summary>
    private string Compact(double v)
    {
        var sample = _format(1);
        var pound = sample.Contains('£');
        var a = Math.Abs(v);
        var text = a >= 1_000_000 ? $"{a / 1_000_000:0.#}m" : a >= 10_000 ? $"{a / 1_000:0}k" : a >= 1_000 ? $"{a / 1_000:0.#}k" : $"{a:0.#}";
        return (v < 0 ? "−" : string.Empty) + (pound ? "£" : string.Empty) + text;
    }

    private Rect2 Plot() => new(new Vector2(56, 6), new Vector2(Mathf.Max(10, Size.X - 62), Mathf.Max(10, Size.Y - 28)));

    public override void _Draw()
    {
        var font = ThemeDB.FallbackFont;
        const int size = 11;
        var muted = UiStyle.TextMuted;
        var plot = Plot();
        if (_data.Count == 0)
        {
            return;
        }

        var max = Math.Max(0, _data.Max(d => d.Value));
        var min = Math.Min(0, _data.Min(d => d.Value));
        if (max - min < 1e-9)
        {
            max = 1;
        }

        float Y(double v) => plot.End.Y - (float)((v - min) / (max - min)) * plot.Size.Y;
        var hair = new Color("#E6ECF1");
        var drawn = new List<float>();
        void AxisLabel(double v)
        {
            var y = Y(v);
            if (drawn.Any(d => Math.Abs(d - y) < 14))
            {
                return;
            }

            drawn.Add(y);
            DrawString(font, new Vector2(0, y + 4), Compact(v), HorizontalAlignment.Right, plot.Position.X - 6, size, muted);
        }

        var zero = Y(0);
        DrawLine(new Vector2(plot.Position.X, zero), new Vector2(plot.End.X, zero), new Color("#C9D3DC"), 1);
        AxisLabel(0);
        foreach (var v in new[] { max, min < 0 ? min : (max + min) / 2 })
        {
            if (v != 0)
            {
                DrawLine(new Vector2(plot.Position.X, Y(v)), new Vector2(plot.End.X, Y(v)), hair, 1);
                AxisLabel(v);
            }
        }

        var slot = plot.Size.X / _data.Count;
        var bar = Mathf.Max(2, slot * 0.7f);
        var every = Math.Max(1, (int)Math.Ceiling(_data.Count / Math.Max(1, plot.Size.X / 56)));
        for (var i = 0; i < _data.Count; i++)
        {
            var (label, value) = _data[i];
            var x = plot.Position.X + (i * slot) + ((slot - bar) / 2);
            var top = Y(Math.Max(0, value));
            var bottom = Y(Math.Min(0, value));
            if (bottom - top > 0.5f)
            {
                var colour = value >= 0 ? _positive : _negative;
                if (i == _hover)
                {
                    colour = colour.Lightened(0.2f);
                }

                var box = UiStyle.Box(colour, (int)Mathf.Min(3, bar / 2));
                box.CornerRadiusBottomLeft = box.CornerRadiusBottomRight = value >= 0 ? 0 : box.CornerRadiusTopLeft;
                if (value < 0)
                {
                    box.CornerRadiusTopLeft = box.CornerRadiusTopRight = 0;
                }

                DrawStyleBox(box, new Rect2(x, top, bar, bottom - top));
            }

            if ((_data.Count - 1 - i) % every == 0)
            {
                DrawString(font, new Vector2(plot.Position.X + (i * slot) - 20 + (slot / 2), plot.End.Y + 16), label, HorizontalAlignment.Center, 40, size, muted);
            }
        }
    }
}
