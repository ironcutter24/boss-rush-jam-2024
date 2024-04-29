using Godot;
using System;
using System.Threading.Tasks;

public partial class PlayerHealer : PlayerUnit
{
    [Export] protected GpuParticles3D HealSelfVFX { get; private set; }

    public override void _Ready()
    {
        base._Ready();
        Attacking += x => AudioManager.Instance.PlayHealerAttack();
    }

    public override async Task Attack(Unit target)
    {
        await AnimatedAttack(target, attackAnimDuration);
    }

    public override async Task Special(Unit target)
    {
        const int healingAmount = 1;

        _ = SetAnimationTrigger("special");
        target.ApplyHealing(healingAmount);
        SpecialVFX.GlobalPosition = target.GlobalPosition;
        SpecialVFX.Emitting = true;

        await GDTask.NextFrame();
    }

    public override async Task Reaction(PlayerUnit swappedUnit, EnemyUnit attackingUnit)
    {
        const int healingAmount = 1;

        _ = SetAnimationTrigger("reaction");

        if (swappedUnit != null)
        {
            swappedUnit.ApplyHealing(healingAmount);
            SpecialVFX.GlobalPosition = swappedUnit.GlobalPosition + Vector3.Up * .6f;
            SpecialVFX.Emitting = true;
        }

        ApplyHealing(healingAmount);
        HealSelfVFX.Emitting = true;

        await GDTask.NextFrame();
    }
}
