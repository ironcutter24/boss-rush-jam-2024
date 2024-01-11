using Godot;
using System;

public partial class Global : Node
{
    public static Global Instance { get; private set; }

    public SceneTree Tree { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        Tree = GetTree();
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("quit"))
        {
            GetTree().Quit();
        }
    }
}
