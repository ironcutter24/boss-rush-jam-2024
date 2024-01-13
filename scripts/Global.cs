using Godot;
using System;

public partial class Global : Node
{
    public static Global Instance { get; private set; }

    public SceneTree Tree { get; private set; }

    public override void _EnterTree()
    {
        Tree = GetTree();
        Instance = this;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("quit"))
        {
            GetTree().Quit();
        }
    }
}
