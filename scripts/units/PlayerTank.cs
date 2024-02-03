using Godot;
using System;
using System.Threading.Tasks;

public partial class PlayerTank : PlayerUnit
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
        attackingUnit.ConsumeAction();
        await GDTask.NextFrame();
    }
}
