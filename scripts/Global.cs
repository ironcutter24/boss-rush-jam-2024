using Godot;
using System;

public partial class Global : Node
{
    private uint levelIndex = 0;

    [Export] private string[] levels;

    public static Global Instance { get; private set; }

    public SceneTree Tree => GetTree();

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Process(double delta)
    {
        // Editor debug tools
        if (OS.HasFeature("editor"))
        {
            ProcessDebugInputs();
        }

        if (Input.IsActionJustPressed("quit"))
        {
            GetTree().Quit();
        }
    }

    public void LoadNextScene()
    {
        if (levelIndex >= levels.Length)
        {
            GD.PrintErr("Try to load a non-existing scene.");
        }
        else
        {
            levelIndex++;
            GetTree().ChangeSceneToFile(levels[levelIndex]);
        }
    }

    private void ProcessDebugInputs()
    {
        if (Input.IsKeyPressed(Key.N))
        {
            LoadNextScene();
        }
    }
}
