using Godot;
using System;
using System.Threading.Tasks;

public partial class PlayerTank : PlayerUnit
{
    [Export] protected GpuParticles3D ShieldVFX { get; private set; }

    public override void _Ready()
    {
        base._Ready();
        Attacking += x => AudioManager.Instance.PlayTankAttack();
    }

    public override async Task Attack(Unit target)
    {
        await AnimatedAttack(target);
    }

    public override async Task Special(Unit target)
    {
        const int tauntTurns = 1;

        var playerUnit = target as PlayerUnit;
        playerUnit.ApplyTauntFor(tauntTurns);
        SpecialVFX.Emitting = true;

        _ = SetAnimationTrigger("special");
        await GDTask.NextFrame();
    }

    public override async Task Reaction(PlayerUnit swappedUnit, EnemyUnit attackingUnit)
    {
        _ = SetAnimationTrigger("reaction");
        attackingUnit.ConsumeAction();
        ShieldVFX.Emitting = true;
        await GDTask.DelaySeconds(.8f);
    }
}
