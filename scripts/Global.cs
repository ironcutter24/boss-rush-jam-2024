using Godot;
using System;

public partial class Global : Node
{
    public static Global Instance { get; private set; }

    public SceneTree Tree => GetTree();

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("quit"))
        {
            GetTree().Quit();
        }
    }
}
