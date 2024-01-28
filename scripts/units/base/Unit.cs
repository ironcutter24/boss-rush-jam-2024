using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public abstract partial class Unit : CharacterBody3D
{
    private bool _hasMovement = true;
    private AnimationTree animTree;
    private HealthBar3D healthBar;
    private Node3D graphics;

    [Export] public int AttackDistance { get; private set; } = 1;
    [Export] public int MoveDistance { get; private set; } = 3;
    [Export] public int MaxHealth { get; private set; } = 3;

    public abstract FactionType Faction { get; }
    public int Health { get; private set; }
    public bool IsSelected { get; private set; } = false;
    public bool HasMovement => _hasMovement && HasAttack;
    public bool HasAttack { get; private set; } = true;

    public int GridId => LevelData.GetId(GridPosition.X, GridPosition.Y);
    public Vector2I GridPosition => new Vector2I((int)Position.X, (int)Position.Z);

    public override void _EnterTree()
    {
        animTree = GetNode<AnimationTree>("AnimationTree");
        healthBar = GetNode<HealthBar3D>("Graphics/HealthBar3D");
        graphics = GetNode<Node3D>("Graphics");

        AddToGroup(GetGroupFrom(Faction));
    }

    public override void _Ready()
    {
        Health = MaxHealth;
        healthBar.SetHealth(Health, MaxHealth);

        GD.Print(GridId);
    }

    public abstract Task Attack(Unit target);


    public async Task FollowPathTo(Vector2I pos)
    {
        Vector3[] path = LevelData.Instance.GetPath(this, pos);

        const float moveSpeed = 1 / 4f;
        for (int i = 1; i < path.Length; i++)
        {
            graphics.LookAt(path[i]);

            Tween tween = CreateTween();
            float duration = GridDistance(path[i - 1], path[i]) * moveSpeed;
            tween.TweenProperty(this, "position", path[i], duration);
            await GDTask.DelaySeconds(duration);
        }

        Position = path[path.Length - 1];
        await GDTask.NextFrame();  // Moving after await statements causes bug where unit position is not updated in LevelData
        return;
    }

    public void SetSelected(bool state)
    {
        IsSelected = state;
        var mat = IsSelected ? GD.Load<Material>("res://graphics/materials/red_mat.tres") : null;
        GetNode<MeshInstance3D>("Graphics/MeshInstance3D").MaterialOverride = mat;
    }

    public void ApplyDamage(int value)
    {
        Health = Mathf.Max(0, Health - value);
        healthBar.SetHealth(Health, MaxHealth);
        if (Health <= 0)
        {
            Free();
        }
    }

    public void ConsumeMovement()
    {
        _hasMovement = false;
    }

    public void ConsumeAttack()
    {
        HasAttack = false;
    }

    public void ResetTurn()
    {
        _hasMovement = HasAttack = true;
    }

    #region Attack methods

    protected async Task SimpleAttack(Unit target)
    {
        const float duration = .1f;

        graphics.LookAt(target.GlobalPosition);

        Tween tween = CreateTween();
        var targetPos = (target.GlobalPosition - graphics.GlobalPosition).Normalized() * .5f;
        tween.TweenProperty(graphics, "position", targetPos, duration);
        tween.TweenProperty(graphics, "position", Vector3.Zero, duration).SetDelay(duration);

        await GDTask.DelaySeconds(duration);  // Wait for go duration
        target.ApplyDamage(1);
        await GDTask.DelaySeconds(duration * 2);  // Wait for delay + return duration

        return;
    }

    #endregion

    #region Helpers

    private static int GridDistance(Vector3 a, Vector3 b)
    {
        return Mathf.RoundToInt(Mathf.Max(Mathf.Abs(a.X - b.X), Mathf.Abs(a.Z - b.Z)));
    }

    public static void ResetTurn(FactionType faction)
    {
        Global.Instance.Tree.CallGroup(Unit.GetGroupFrom(faction), "ResetTurn");
    }

    public static List<Unit> GetUnits(FactionType faction)
    {
        return Global.Instance.Tree
            .GetNodesInGroup(GetGroupFrom(faction))
            .OfType<Unit>().ToList();
    }

    public static StringName GetGroupFrom(FactionType faction)
    {
        return $"{faction.ToString().ToLower()}_units";
    }

    #endregion

}
