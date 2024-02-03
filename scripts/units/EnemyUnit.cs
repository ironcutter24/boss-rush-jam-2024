using Godot;
using System;
using System.Threading.Tasks;

public partial class EnemyUnit : Unit
{
    public event Action Possessed;
    public event Action Unpossessed;
    public event Action<int> BossDamaged;

    [Export] private bool useSimpleAttack = false;
    [Export] private Node3D[] showWhilePossessed;
    [Export] private Node3D[] hideWhilePossessed;

    [ExportGroup("Boss parameters")]
    [Export] private int _bossAttackDamage = 2;
    [Export] private int _bossAttackDistance = 1;
    [Export] private int _bossMoveDistance = 0;

    public bool IsPossessed { get; private set; } = false;
    public override int AttackDamage => IsPossessed ? _bossAttackDamage : base.AttackDamage;
    public override int AttackDistance => IsPossessed ? _bossAttackDistance : base.AttackDistance;
    public override int MoveDistance => IsPossessed ? _bossMoveDistance : base.MoveDistance;

    public override FactionType Faction => FactionType.Enemy;

    public override void _Ready()
    {
        base._Ready();
        RefreshVisibility();
        GD.Print("Possessed: " + IsPossessed);

        Damaged += ForwardBossDamage;
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
        (IsPossessed ? Possessed : Unpossessed)?.Invoke();

        animTree.Set("parameters/conditions/possessed", IsPossessed);
        animTree.Set("parameters/conditions/not_possessed", !IsPossessed);

        if (IsPossessed) RefreshVisibility();
        await Task.Delay(1000);
        if (!IsPossessed) RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        foreach (var item in showWhilePossessed)
            item.Visible = IsPossessed;

        foreach (var item in hideWhilePossessed)
            item.Visible = !IsPossessed;
    }

    void ForwardBossDamage(int damage)
    {
        if (IsPossessed)
        {
            BossDamaged?.Invoke(damage);
        }
    }
}
