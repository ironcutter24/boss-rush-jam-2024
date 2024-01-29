using Godot;
using System;
using System.Threading.Tasks;

public partial class PlayerHealer : PlayerUnit
{
    public override async Task Attack(Unit target)
    {
        await SimpleAttack(target);
    }

    public override async Task Special(Unit target)
    {
        const int healingAmount = 1;

        target.ApplyHealing(healingAmount);
        await GDTask.NextFrame();
    }

    public override async Task Reaction(PlayerUnit swappedUnit, EnemyUnit attackingUnit)
    {
        const int healingAmount = 1;

        swappedUnit.ApplyHealing(healingAmount);
        await GDTask.NextFrame();
    }
}
