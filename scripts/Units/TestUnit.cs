using Godot;
using System;
using System.Threading.Tasks;

public partial class TestUnit : Unit
{
    public override async Task Attack()
    {
        GD.Print("Performing attack");
        await GDTask.NextFrame();
        return;
    }

    public override async Task Special()
    {
        GD.Print("Performing special");
        await GDTask.NextFrame();
        return;
    }

    public override async Task Reaction()
    {
        GD.Print("Performing reaction");
        await GDTask.NextFrame();
        return;
    }
}
