using Godot;
using System;

public partial class UnitHUD : Panel
{
    const string reactionFooter = "\n\n[i]Reactions can be used after swapping with an attacked unit, and expire at the beginning of player's turn.[/i]";

    [Export] private ActionInfo attackInfo;
    [Export] private ActionInfo specialInfo;
    [Export] private ActionInfo reactionInfo;

    [Export] private PlayerUnit unit;

    public override void _Ready()
    {
        //Initialize(unit);
    }

    public void Initialize(PlayerUnit unit)
    {
        attackInfo.SetContent("[b]Attack[/b]", unit.AttackDescription);
        specialInfo.SetContent("[b]Special[/b]", unit.SpecialDescription);
        reactionInfo.SetContent("[b]Reaction[/b]", unit.ReactionDescription + reactionFooter);
    }
}
