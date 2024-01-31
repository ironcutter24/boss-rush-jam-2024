using Godot;
using System;

public partial class UnitHUD : Panel
{
    const string reactionFooter = "[i]Reactions can be used after swapping with an attacked unit, and expire at the beginning of player's turn.[/i]";

    [Export] private UnitInfo unitInfo;
    [Export] private ActionInfo attackInfo;
    [Export] private ActionInfo specialInfo;
    [Export] private ActionInfo reactionInfo;

    public override void _Ready()
    {
        Initialize();
    }

    public void Initialize()
    {
        unitInfo.SetContent($"[b]N/A[/b]", $"Class: N/A\nSpeed: N/A", new Texture2D());
        attackInfo.SetContent("[b]Attack[/b]", "");
        specialInfo.SetContent("[b]Special[/b]", "");
        reactionInfo.SetContent("[b]Reaction[/b]", reactionFooter);
    }

    public void Initialize(PlayerUnit unit)
    {
        unitInfo.SetContent($"[b]{unit.CharacterName}[/b]", $"Class: {unit.CharacterClass}\nSpeed: {unit.MoveDistance}", new Texture2D());
        attackInfo.SetContent("[b]Attack[/b]", unit.AttackDescription);
        specialInfo.SetContent("[b]Special[/b]", unit.SpecialDescription);
        reactionInfo.SetContent("[b]Reaction[/b]", $"{unit.ReactionDescription}\n\n{reactionFooter}");
    }
}
