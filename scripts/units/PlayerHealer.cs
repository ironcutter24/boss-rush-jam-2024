using Godot;
using System;
using System.Threading.Tasks;

public partial class PlayerHealer : PlayerUnit
{
    public override void _Ready()
    {
        base._Ready();
        Attacking += () => AudioManager.Instance.PlayHealerAttack();
    }

    public override async Task Attack(Unit target)
    {
        await AnimatedAttack(target);
    }

    public override async Task Special(Unit target)
    {
        const int healingAmount = 1;

        _ = SetAnimationTrigger("special");
        target.ApplyHealing(healingAmount);
        await GDTask.NextFrame();
    }

    public override async Task Reaction(PlayerUnit swappedUnit, EnemyUnit attackingUnit)
    {
        const int healingAmount = 1;

        _ = SetAnimationTrigger("reaction");
        swappedUnit.ApplyHealing(healingAmount);
        await GDTask.NextFrame();
    }
}
