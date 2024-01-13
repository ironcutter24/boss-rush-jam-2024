using Godot;
using System;
using System.Threading.Tasks;

public abstract partial class Unit : CharacterBody3D
{
    private AnimationTree animTree;

    [Export] public int MoveDistance { get; private set; }
    [Export] public int MaxHealth { get; private set; }
    public int Health { get; private set; }
    public bool HasMovement { get; set; } = true;
    public Vector2I GridPosition => new Vector2I((int)Position.X, (int)Position.Z);
    public int GridId => LevelData.GetId(GridPosition.X, GridPosition.Y);

    public override void _Ready()
    {
        animTree = GetNode<AnimationTree>("AnimationTree");
        GD.Print(GridId);
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
