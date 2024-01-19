using Godot;
using System;
using System.Threading.Tasks;

public partial class TestUnit : Unit
{
    public override async Task Attack(Unit target)
    {
        const float duration = .1f;
        Vector3 startPos = Position;
        Tween tween = CreateTween();

        var targetPos = Position + (target.Position - Position).Normalized() * .5f;
        tween.TweenProperty(this, "position", targetPos, duration);
        tween.TweenProperty(this, "position", startPos, duration).SetDelay(duration);
        
        await GDTask.DelaySeconds(duration);
        target.ApplyDamage(1);
        await GDTask.DelaySeconds(duration);

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
