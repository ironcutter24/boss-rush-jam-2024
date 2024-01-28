using Godot;
using System;
using System.Threading.Tasks;

public partial class PlayerTank : PlayerUnit
{
    public override async Task Attack(Unit target)
    {
        await SimpleAttack(target);
    }

    public override Task Reaction()
    {
        throw new NotImplementedException();
    }

    public override Task Special()
    {
        throw new NotImplementedException();
    }
}
