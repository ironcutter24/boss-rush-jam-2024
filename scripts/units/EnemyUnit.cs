using Godot;
using System;
using System.Threading.Tasks;

public partial class EnemyUnit : Unit
{
    [ExportGroup("Boss parameters")]
    [Export] private int _bossAttackDamage = 2;
    [Export] private int _bossAttackDistance = 1;
    [Export] private int _bossMoveDistance = 0;

    public bool IsPossessed { get; private set; } = false;
    public override int AttackDamage => IsPossessed ? _bossAttackDamage : base.AttackDamage;
    public override int AttackDistance => IsPossessed ? _bossAttackDistance : base.AttackDistance;
    public override int MoveDistance => IsPossessed ? _bossMoveDistance : base.MoveDistance;

    public override FactionType Faction => FactionType.Enemy;

    public override async Task Attack(Unit target)
    {
        await SimpleAttack(target);
    }

    public async Task SetPossessed(bool state)
    {
        IsPossessed = state;

        // TODO: Start animation transition

        await Task.Delay(1000);
    }
}
