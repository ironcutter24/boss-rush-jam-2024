using Godot;
using System;
using System.Threading.Tasks;

public abstract partial class PlayerUnit : Unit
{
    public override FactionType Faction => FactionType.Player;

    public abstract Task Special();
    public abstract Task Reaction();
}
