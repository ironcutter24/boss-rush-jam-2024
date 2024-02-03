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
        _ = SetAnimationTrigger("special");
        await GDTask.NextFrame();
    }

    public override async Task Reaction(PlayerUnit swappedUnit, EnemyUnit attackingUnit)
    {
        _ = SetAnimationTrigger("reaction");
        await AnimatedAttack(attackingUnit);
    }
}
