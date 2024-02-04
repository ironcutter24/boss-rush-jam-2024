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

    public int BuffTurns { get; private set; } = 0;
    public override int AttackDamage => base.AttackDamage * (BuffTurns > 0 ? 2 : 1);

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

    public override void _Ready()
    {
        base._Ready();
        Dying += () => AudioManager.Instance.PlayPlayerDeath();
    }

    public async Task PlanReaction()
    {
        AudioManager.Instance.PlayReactionCharge();

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
        BuffTurns = Mathf.Max(BuffTurns - 1, 0);
        unitIcons.Reset();
    }

    public void ApplyBuffFor(int playerTurns)
    {
        BuffTurns = playerTurns;
    }
}
