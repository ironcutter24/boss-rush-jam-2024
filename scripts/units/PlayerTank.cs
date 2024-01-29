using Godot;
using System;
using System.Threading.Tasks;

public partial class PlayerTank : PlayerUnit
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
        attackingUnit.ConsumeAction();
        await GDTask.NextFrame();
    }
}
