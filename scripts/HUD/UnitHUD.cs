using Godot;
using System;

public partial class UnitHUD : Panel
{
    const string attackStr = "Attack";
    const string specialStr = "Special";
    const string reactionStr = "Reaction";
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
        unitInfo.SetContent(Bold("N/A"), $"Class: N/A\nSpeed: N/A", new Texture2D());
        attackInfo.SetContent(Bold(attackStr), "");
        specialInfo.SetContent(Bold(specialStr), "");
        reactionInfo.SetContent(Bold(reactionStr), reactionFooter);
    }

    public void Initialize(PlayerUnit unit)
    {
        unitInfo.SetContent(Bold(unit.CharacterName), $"Class: {unit.CharacterClass}\nSpeed: {unit.MoveDistance}", new Texture2D());
        attackInfo.SetContent(FormatTitle(attackStr, "A"), unit.AttackDescription);
        specialInfo.SetContent(FormatTitle(specialStr, "S"), unit.SpecialDescription);
        reactionInfo.SetContent(FormatTitle(reactionStr, "D"), $"{unit.ReactionDescription}\n\n{reactionFooter}");
    }

    #region Text formatting

    private static string FormatTitle(string name, string key)
    {
        return Bold(name) + Yellow(Italic($" [{key} key]"));
    }

    private static string Bold(string text)
    {
        return $"[b]{text}[/b]";
    }

    private static string Italic(string text)
    {
        return $"[i]{text}[/i]";
    }

    private static string Yellow(string text)
    {
        return $"[color=yellow]{text}[/color]";
    }

    #endregion

}
