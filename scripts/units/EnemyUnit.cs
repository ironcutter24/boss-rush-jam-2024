using Godot;
using System;
using System.Threading.Tasks;

public partial class EnemyUnit : Unit
{
    private float startHealthBarOffset;

    [Export] private bool useSimpleAttack = false;
    [Export] private Node3D[] showWhilePossessed;
    [Export] private Node3D[] hideWhilePossessed;
    [Export] protected GpuParticles3D PossessionVFX { get; private set; }

    [ExportGroup("Boss parameters")]
    [Export] private int _bossAttackDamage = 2;
    [Export] private int _bossAttackDistance = 1;
    [Export] private int _bossMoveDistance = 0;
    [Export] private bool _bossIsAttackable = true;
    [Export] private float _bossHealthBarOffset = 0f;

    public bool IsPossessed { get; private set; } = false;
    public override int AttackDamage => IsPossessed ? _bossAttackDamage : base.AttackDamage;
    public override int AttackDistance => IsPossessed ? _bossAttackDistance : base.AttackDistance;
    public override int MoveDistance => IsPossessed ? _bossMoveDistance : base.MoveDistance;
    public override bool IsAttackable => IsPossessed ? _bossIsAttackable : base.IsAttackable;

    public override FactionType Faction => FactionType.Enemy;


    public event Action Possessed;
    public event Action Unpossessed;
    public event Action<int> BossDamaged;


    public override void _Ready()
    {
        base._Ready();
        RefreshVisibility();
        animFX.Play("spawn_fall");

        startHealthBarOffset = healthBar.Position.Y;

        Damaged += ForwardBossDamage;
        Attacking += x => AudioManager.Instance.PlayEnemyAttack();
    }

    public override async Task Attack(Unit target)
    {
        if (!IsPossessed && useSimpleAttack)
            await SimpleAttack(target);
        else
            await AnimatedAttack(target);
    }

    public async Task SetPossessed(bool state)
    {
        IsPossessed = state;

        if (IsPossessed)
        {
            Possessed?.Invoke();
            SetHealthBarOffset(_bossHealthBarOffset);
            AudioManager.Instance.PlayEnemyTransform();
        }
        else
        {
            Unpossessed?.Invoke();
            SetHealthBarOffset(startHealthBarOffset);
        }

        Tween tween = CreateTween();
        tween.TweenProperty(PossessionVFX, "scale", Vector3.Zero, .6f);

        animTree.Set("parameters/conditions/possessed", IsPossessed);
        animTree.Set("parameters/conditions/not_possessed", !IsPossessed);

        if (IsPossessed) RefreshVisibility();
        await Task.Delay(1000);
        PossessionVFX.Emitting = false;
        PossessionVFX.GetNode<GpuParticles3D>("GpuParticles3D").Emitting = false;
        if (!IsPossessed) RefreshVisibility();
    }

    public void SetNextPossessed()
    {
        PossessionVFX.Scale = Vector3.One;
        PossessionVFX.Emitting = true;
        PossessionVFX.GetNode<GpuParticles3D>("GpuParticles3D").Emitting = true;
    }

    private void RefreshVisibility()
    {
        foreach (var item in showWhilePossessed)
        {
            if (item != null)
                item.Visible = IsPossessed;
        }

        foreach (var item in hideWhilePossessed)
        {
            if (item != null)
                item.Visible = !IsPossessed;
        }
    }

    private void SetHealthBarOffset(float offset)
    {
        var pos = healthBar.Position;
        pos.Y = offset;
        healthBar.Position = pos;
    }

    void ForwardBossDamage(int damage)
    {
        if (IsPossessed)
        {
            BossDamaged?.Invoke(damage);
        }
    }
}
