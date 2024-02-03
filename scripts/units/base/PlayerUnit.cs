using Godot;
using System;
using System.Threading.Tasks;

public abstract partial class PlayerUnit : Unit
{
    [ExportGroup("Meta data")]
    [Export] public string CharacterName { get; private set; }
    [Export] public string CharacterClass { get; private set; }
    [Export(PropertyHint.MultilineText)] public string AttackDescription { get; private set; }
    [Export(PropertyHint.MultilineText)] public string SpecialDescription { get; private set; }
    [Export(PropertyHint.MultilineText)] public string ReactionDescription { get; private set; }

    public bool IsReactionPlanned { get; private set; } = false;

    public override FactionType Faction => FactionType.Player;

    public abstract Task Special(Unit target);
    public abstract Task Reaction(PlayerUnit swappedUnit, EnemyUnit attackingUnit);

    public async Task PlanReaction()
    {
        IsReactionPlanned = true;

        // TODO: Play reaction preparation animation / particles

        await GDTask.DelaySeconds(1f);
    }

    public override void ResetTurn()
    {
        base.ResetTurn();
        IsReactionPlanned = false;
    }
}
