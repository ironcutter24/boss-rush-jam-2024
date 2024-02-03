using Godot;
using System;
using System.Threading.Tasks;

public abstract partial class PlayerUnit : Unit
{
    private UnitIcons3D unitIcons;

    [Export] public bool HasSpecialSelection { get; private set; } = false;
    [Export] public int SpecialDistance { get; private set; } = 0;

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
        ReactionVFX.Visible = true;
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
        ReactionVFX.Visible = false;
        unitIcons.Reset();
    }
}
