using System;
using Godot;

namespace BlueprintSchool.UI;

public static class HudStyle
{
    /// <summary>The card every in-game panel sits on (kept under its old name).</summary>
    public static StyleBoxFlat DarkPanel() => UiStyle.Card(16);

    public static StyleBoxFlat RowBox(Color colour) => new()
    {
        BgColor = colour,
        CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
        ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 6, ContentMarginBottom = 6,
    };

    /// <summary>A flat, left-aligned button for clickable list rows.</summary>
    public static Button RowButton(string text, Action onPressed)
    {
        var button = new Button
        {
            Text = text,
            Alignment = HorizontalAlignment.Left,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            FocusMode = Control.FocusModeEnum.None,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
        };
        button.AddThemeStyleboxOverride("normal", RowBox(new Color("#F7F9FB")));
        button.AddThemeStyleboxOverride("hover", RowBox(UiStyle.PrimaryTint));
        button.AddThemeStyleboxOverride("pressed", RowBox(new Color("#CFE5D8")));
        foreach (var state in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color" })
        {
            button.AddThemeColorOverride(state, UiStyle.TextDark);
        }

        button.AddThemeFontSizeOverride("font_size", 14);
        button.Pressed += onPressed;
        return button;
    }
}
