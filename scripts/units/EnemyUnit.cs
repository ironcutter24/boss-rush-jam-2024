using Godot;
using System;
using System.Threading.Tasks;

public partial class EnemyUnit : Unit
{
    public override FactionType Faction => FactionType.Enemy;

    public override async Task Attack(Unit target)
    {
        await SimpleAttack(target);
    }
}
