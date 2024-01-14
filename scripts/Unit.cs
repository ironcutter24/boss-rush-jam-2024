using Godot;
using System;
using System.Threading.Tasks;

public abstract partial class Unit : CharacterBody3D
{
    private AnimationTree animTree;

    [Export] public int MoveDistance { get; private set; } = 3;
    [Export] public int MaxHealth { get; private set; } = 3;
    public int Health { get; private set; }
    public bool IsSelected { get; private set; } = false;
    private bool _hasMovement = true;
    public bool HasMovement
    {
        get { return _hasMovement && HasAttack; }
        set { _hasMovement = value; }
    }
    public bool HasAttack { get; set; } = true;
    public Vector2I GridPosition => new Vector2I((int)Position.X, (int)Position.Z);
    public int GridId => LevelData.GetId(GridPosition.X, GridPosition.Y);

    public override void _Ready()
    {
        animTree = GetNode<AnimationTree>("AnimationTree");
        GD.Print(GridId);
    }

    public void SetSelected(bool state)
    {
        IsSelected = state;
        var mat = IsSelected ? GD.Load<Material>("res://materials/red_mat.tres") : null;
        GetNode<MeshInstance3D>("MeshInstance3D").MaterialOverride = mat;
    }

    public void ResetTurn()
    {
        HasMovement = HasAttack = true;
    }

    public abstract Task Attack();
    public abstract Task Special();
    public abstract Task Reaction();

    public async Task FollowPath(Vector3[] path)
    {
        const float moveSpeed = 1 / 4f;
        for (int i = 1; i < path.Length; i++)
        {
            Tween tween = CreateTween();
            float duration = GridDistance(path[i - 1], path[i]) * moveSpeed;
            tween.TweenProperty(this, "position", path[i], duration);
            await GDTask.DelaySeconds(duration);
        }

        Position = path[path.Length - 1];
        await GDTask.NextFrame();  // Moving after await statements causes bug where unit position is not updated in LevelData
        return;
    }

    private int GridDistance(Vector3 a, Vector3 b)
    {
        return Mathf.RoundToInt(Mathf.Max(Mathf.Abs(a.X - b.X), Mathf.Abs(a.Z - b.Z)));
    }
}
