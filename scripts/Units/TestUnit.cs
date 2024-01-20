using Godot;
using System;
using System.Threading.Tasks;

public partial class TestUnit : Unit
{
    public override async Task Attack(Unit target)
    {
        const float duration = .1f;

        Node3D graphics = GetNode<Node3D>("Graphics");
        Tween tween = CreateTween();

        var targetPos = (target.GlobalPosition - graphics.GlobalPosition).Normalized() * .5f;
        tween.TweenProperty(graphics, "position", targetPos, duration);
        tween.TweenProperty(graphics, "position", Vector3.Zero, duration).SetDelay(duration);

        await GDTask.DelaySeconds(duration);  // Wait for go duration
        target.ApplyDamage(1);
        await GDTask.DelaySeconds(duration * 2);  // Wait for delay + return duration

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
