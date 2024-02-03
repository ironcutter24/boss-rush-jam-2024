using Godot;
using System;
using System.Threading.Tasks;

public partial class PlayerBerserker : PlayerUnit
{
    public override async Task Attack(Unit target)
    {
        await AnimatedAttack(target);
    }

    public override async Task Special(Unit target)
    {
        const int buffTurns = 2;

        var playerUnit = target as PlayerUnit;
        playerUnit.ApplyBuffFor(buffTurns);

        _ = SetAnimationTrigger("special");
        await GDTask.NextFrame();
    }

    public override async Task Reaction(PlayerUnit swappedUnit, EnemyUnit attackingUnit)
    {
        _ = SetAnimationTrigger("reaction");
        await AnimatedAttack(attackingUnit);
    }
}
