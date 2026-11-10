using System;
using BlueprintSchool.Core;
using BlueprintSchool.Simulation.Core;

namespace BlueprintSchool.UI;

/// <summary>In-game menu. The simulation is paused while it is open.</summary>
public partial class PauseMenu : ModalPanel
{
    private Func<CameraState> _captureCamera = () => new CameraState(0, 0, 1);

    public static PauseMenu Create(Func<CameraState> captureCamera, string schoolName)
    {
        var menu = new PauseMenu { _captureCamera = captureCamera };
        menu.Build(schoolName);
        return menu;
    }

    private void Build(string schoolName)
    {
        Content.AddChild(UiStyle.Heading("Paused"));
        Content.AddChild(UiStyle.Button("Resume", Close));
        Content.AddChild(UiStyle.SecondaryButton("Save Game", () => AddChild(SaveGamePanel.Create(_captureCamera, schoolName))));
        Content.AddChild(UiStyle.SecondaryButton("Load Game", () => AddChild(new LoadGamePanel())));
        Content.AddChild(UiStyle.SecondaryButton("Settings", () => AddChild(new SettingsPanel())));
        Content.AddChild(UiStyle.SecondaryButton("Exit to Main Menu", () =>
        {
            GetTree().Paused = false;
            GameManager.Instance.ReturnToMainMenu();
        }));
        Content.AddChild(UiStyle.SecondaryButton("Quit to Desktop", () => GetTree().Quit()));
    }

    public override void _Ready()
    {
        GetTree().Paused = true;
        Closed += () => GetTree().Paused = false;
    }
}
