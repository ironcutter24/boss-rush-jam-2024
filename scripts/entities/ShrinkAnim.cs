using Godot;
using System;

public partial class ShrinkAnim : MeshInstance3D
{
    const float duration = .6f;
    const float scale = .65f;

    private Vector3 startScale;

    [Export] private EnemyUnit unitInstance;

    public override void _Ready()
    {
        startScale = Scale;
        unitInstance.Possessed += OnPossessed;
        unitInstance.Unpossessed += OnUnpossessed;
    }

    public override void _ExitTree()
    {
        unitInstance.Possessed -= OnPossessed;
        unitInstance.Unpossessed -= OnUnpossessed;
    }

    private void OnPossessed()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(this, "scale", startScale * scale, duration);
    }

    private void OnUnpossessed()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(this, "scale", startScale, duration);
    }
}
