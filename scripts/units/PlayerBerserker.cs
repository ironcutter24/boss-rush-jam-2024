using Godot;
using System;
using System.Threading.Tasks;

public partial class PlayerBerserker : PlayerUnit
{
    public override async Task Attack(Unit target)
    {
        await AnimatedAttack(target);
    }

    public override Task Special(Unit target)
    {
        throw new NotImplementedException();
    }

    public override async Task Reaction(PlayerUnit swappedUnit, EnemyUnit attackingUnit)
    {
        await AnimatedAttack(attackingUnit);
    }
}
