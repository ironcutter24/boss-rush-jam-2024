using Godot;
using System;
using System.Threading.Tasks;

public abstract partial class Unit : CharacterBody3D
{
    [Export] public int MaxHealth { get; private set; }
    public int Health { get; private set; }

    [Export]
    public int MoveDistance { get; private set; }

    AnimationTree animTree;

    public override void _Ready()
    {
        animTree = GetNode<AnimationTree>("AnimationTree");
    }

    public abstract Task Attack();
    public abstract Task Special();
    public abstract Task Reaction();

    public async Task MoveTo(int i, int j)
    {
        // TODO: perform grid movement
        await GDTask.NextFrame();
        return;
    }
}
