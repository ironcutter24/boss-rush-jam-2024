using Godot;
using System;
using System.Threading.Tasks;

public abstract partial class PlayerUnit : Unit
{
    private UnitIcons3D unitIcons;

    [Export] public bool HasSpecialSelection { get; private set; } = false;
    [Export] public int SpecialDistance { get; private set; } = 0;
    [Export] protected GpuParticles3D AttackVFX { get; private set; }
    [Export] protected GpuParticles3D SpecialVFX { get; private set; }
    [Export] protected GpuParticles3D HurtVFX { get; private set; }


    [ExportGroup("Meta data")]
    [Export] public string CharacterName { get; private set; }
    [Export] public string CharacterClass { get; private set; }
    [Export(PropertyHint.MultilineText)] public string AttackDescription { get; private set; }
    [Export(PropertyHint.MultilineText)] public string SpecialDescription { get; private set; }
    [Export(PropertyHint.MultilineText)] public string ReactionDescription { get; private set; }

    public int BuffTurns { get; private set; } = 0;
    public int TauntTurns { get; private set; } = 0;

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
        Attacking += OnAttacking;
        Damaged += OnDamaged;
        Dying += () => AudioManager.Instance.PlayPlayerDeath();
    }

    private void OnAttacking(Unit target)
    {
        AttackVFX.GlobalPosition = target.GlobalPosition;
        AttackVFX.Emitting = true;
    }

    private void OnDamaged(int _val)
    {
        HurtVFX.Emitting = true;
        HurtVFX.GetNode<GpuParticles3D>("GPUParticles3D").Emitting = true;
    }

    public async Task PlanReaction()
    {
        AudioManager.Instance.PlayReactionCharge();

        IsReactionPlanned = true;
        ReactionVFX.Emitting = true;
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
        ReactionVFX.Emitting = false;
        ConsumeBuffTurn();
        ConsumeTauntTurn();
        unitIcons.Reset();
    }

    #region Special statuses

    public void ApplyBuffFor(int playerTurns)
    {
        BuffTurns = playerTurns;
    }

    private void ConsumeBuffTurn()
    {
        if (BuffTurns > 0)
        {
            BuffTurns = Mathf.Max(BuffTurns - 1, 0);
            if (BuffTurns == 0)
            {
                SpecialVFX.Emitting = false;
            }
        }
    }

    public void ApplyTauntFor(int playerTurns)
    {
        TauntTurns = playerTurns;
    }

    private void ConsumeTauntTurn()
    {
        TauntTurns = Mathf.Max(TauntTurns - 1, 0);
    }

    #endregion
}
