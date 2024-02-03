using Godot;
using System;
using System.Threading.Tasks;

public partial class HealthBar : ProgressBar
{
    const float seekSpeed = 20f;

    private ProgressBar deltaBar;
    private Timer deltaTimer;
    private double deltaTargetValue;

    [Export] bool isTestEnabled = false;

    public override void _EnterTree()
    {
        deltaBar = GetNode<ProgressBar>("DeltaBar");
        deltaTimer = GetNode<Timer>("DeltaTimer");
    }

    public override void _Ready()
    {
        var playerUnits = Unit.GetUnits(FactionType.Enemy);
        foreach (EnemyUnit unit in playerUnits)
        {
            unit.BossDamaged += ApplyDamage;
        }

        Value = deltaBar.Value = deltaTargetValue = 100;
        deltaTimer.Timeout += () => deltaTargetValue = Value;

        if (isTestEnabled)
        {
            _ = TestAsync();
        }
    }

    public override void _Process(double delta)
    {
        deltaBar.Value = Mathf.Clamp(deltaBar.Value - seekSpeed * delta, deltaTargetValue, deltaBar.MaxValue);
    }

    async Task TestAsync()
    {
        while (true)
        {
            await Task.Delay(1000);
            ApplyDamage(10);

            await Task.Delay(1000);
            await Task.Delay(1000);
            ApplyDamage(10);
            ApplyDamage(10);
        }
    }

    public void ApplyDamage(int damage)
    {
        damage = Mathf.Abs(damage);
        var newValue = Mathf.Clamp(Value - damage, MinValue, MaxValue);
        if (newValue != Value)
        {
            Value = newValue;
            deltaTargetValue = deltaBar.Value;
            deltaTimer.Start();
        }
    }
}
