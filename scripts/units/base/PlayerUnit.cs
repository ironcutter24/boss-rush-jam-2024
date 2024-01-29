using Godot;
using System;
using System.Threading.Tasks;

public abstract partial class PlayerUnit : Unit
{
    public bool IsReactionPlanned { get; private set; } = false;

    public override FactionType Faction => FactionType.Player;

    public abstract Task Special(Unit target);
    public abstract Task Reaction(PlayerUnit swappedUnit, EnemyUnit attackingUnit);

    public async Task PlanReaction()
    {
        IsReactionPlanned = true;

        // Play reaction preparation animation / particles
        await GDTask.DelaySeconds(1f);
    }

    public override void ResetTurn()
    {
        base.ResetTurn();
        IsReactionPlanned = false;
    }
}
