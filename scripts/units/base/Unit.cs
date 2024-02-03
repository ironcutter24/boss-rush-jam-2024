using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public abstract partial class Unit : CharacterBody3D
{
    private bool _hasMovement = true;
    private AnimationPlayer animFX;
    private HealthBar3D healthBar;
    private Node3D graphics;
    private GpuParticles3D selectionVFXInstance;
    protected AnimationTree animTree;

    [Export] private PackedScene SelectionVFX { get; set; }
    [Export] public int MaxHealth { get; private set; } = 3;

    [ExportGroup("Unit parameters")]
    [Export] private int _attackDamage = 1;
    [Export] private int _attackDistance = 1;
    [Export] private int _moveDistance = 3;

    public virtual int AttackDamage => _attackDamage;
    public virtual int AttackDistance => _attackDistance;
    public virtual int MoveDistance => _moveDistance;


    public abstract FactionType Faction { get; }
    public int Health { get; private set; }
    public bool IsSelected { get; private set; } = false;
    public bool HasMovement => _hasMovement && HasAction;
    public bool HasAction { get; private set; } = true;

    public int GridId => LevelData.GetId(GridPosition.X, GridPosition.Y);
    public Vector2I GridPosition => new Vector2I((int)Position.X, (int)Position.Z);

    public override void _EnterTree()
    {
        animFX = GetNode<AnimationPlayer>("AnimationPlayer");
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
        if (IsSelected && selectionVFXInstance == null)
        {
            selectionVFXInstance = SelectionVFX.Instantiate<GpuParticles3D>();
            selectionVFXInstance.Position = Vector3.Zero;
            AddChild(selectionVFXInstance);
        }
        else
        {
            selectionVFXInstance?.Free();
            selectionVFXInstance = null;
        }

        // Test color
        //var mat = IsSelected ? GD.Load<Material>("res://graphics/materials/red_mat.tres") : null;
        //GetNode<MeshInstance3D>("Graphics/MeshInstance3D").MaterialOverride = mat;
    }

    public void ApplyDamage(int value)
    {
        AddToHealth(-Mathf.Abs(value));
        if (Health <= 0)
        {
            Free();
        }
        else
        {
            _ = SetAnimationTrigger("hurt");
            animFX.Play("hurt_spring");
        }
    }

    public void ApplyHealing(int value)
    {
        AddToHealth(Mathf.Abs(value));
    }

    private void AddToHealth(int value)
    {
        Health = Mathf.Max(0, Health + value);
        healthBar.SetHealth(Health, MaxHealth);
    }

    public void ConsumeMovement()
    {
        _hasMovement = false;
    }

    public void ConsumeAction()
    {
        HasAction = false;
    }

    public virtual void ResetTurn()
    {
        _hasMovement = HasAction = true;
    }

    #region Attack methods

    protected async Task SimpleAttack(Unit target)
    {
        const float moveTime = .1f;
        const float attackTime = .1f;

        FaceTowards(target);

        Tween tween = CreateTween();
        var targetPos = (target.GlobalPosition - graphics.GlobalPosition).Normalized() * .5f;
        tween.TweenProperty(graphics, "position", targetPos, moveTime);
        tween.TweenProperty(graphics, "position", Vector3.Zero, moveTime).SetDelay(attackTime + moveTime);

        await GDTask.DelaySeconds(moveTime);  // Wait for go duration

        _ = SetAnimationTrigger("attack");
        target.ApplyDamage(AttackDamage);
        await GDTask.DelaySeconds(attackTime + moveTime);  // Wait for attack + return duration
    }

    protected async Task AnimatedAttack(Unit target)
    {
        const float moveTime = .1f;
        const float attackTime = 1f;

        FaceTowards(target);

        Tween tween = CreateTween();
        var targetPos = (target.GlobalPosition - graphics.GlobalPosition).Normalized() * .5f;
        tween.TweenProperty(graphics, "position", targetPos, moveTime);
        tween.TweenProperty(graphics, "position", Vector3.Zero, moveTime).SetDelay(attackTime + moveTime);

        await GDTask.DelaySeconds(moveTime);  // Wait for go duration

        _ = SetAnimationTrigger("attack");
        await GDTask.DelaySeconds(attackTime * .5f);
        target.ApplyDamage(AttackDamage);
        await GDTask.DelaySeconds(attackTime * .5f);
        await GDTask.DelaySeconds(moveTime);  // Wait for return duration
    }

    #endregion

    #region Helpers

    public void FaceTowards(Unit unit)
    {
        graphics.LookAt(unit.GlobalPosition);
    }

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

    protected async Task SetAnimationTrigger(string condition)
    {
        var path = $"parameters/conditions/{condition}";
        animTree.Set(path, true);
        await Task.Delay(200);
        animTree.Set(path, false);
    }

}
