using Godot;
using System;
using System.Threading.Tasks;

public abstract partial class PlayerUnit : Unit
{
    private UnitIcons3D unitIcons;

    [ExportGroup("Meta data")]
    [Export] public string CharacterName { get; private set; }
    [Export] public string CharacterClass { get; private set; }
    [Export(PropertyHint.MultilineText)] public string AttackDescription { get; private set; }
    [Export(PropertyHint.MultilineText)] public string SpecialDescription { get; private set; }
    [Export(PropertyHint.MultilineText)] public string ReactionDescription { get; private set; }

    public bool IsReactionPlanned { get; private set; } = false;
    public bool HasReactionCharge => unitIcons.HasChargeLeft;

    public override FactionType Faction => FactionType.Player;

    public abstract Task Special(Unit target);
    public abstract Task Reaction(PlayerUnit swappedUnit, EnemyUnit attackingUnit);

    public override void _EnterTree()
    {
        base._EnterTree();
        unitIcons = GetNode<UnitIcons3D>("UnitIcons3D");
    }

    public async Task PlanReaction()
    {
        IsReactionPlanned = true;

        // TODO: Play reaction preparation animation / particles

        //await GDTask.DelaySeconds(1f);
        await GDTask.NextFrame();
    }

    public void ConsumeReactionCounter()
    {
        unitIcons.ConsumeSwapCharge();
    }

    public override void ResetTurn()
    {
        base.ResetTurn();
        IsReactionPlanned = false;
        unitIcons.Reset();
    }
}
