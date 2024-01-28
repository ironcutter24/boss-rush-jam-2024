using Godot;
using System;

public partial class HealthBar3D : Sprite3D
{
    private SubViewport viewport;
    private TextureProgressBar progressBar;

    public override void _EnterTree()
    {
        progressBar = GetNode<TextureProgressBar>("SubViewport/CanvasLayer/TextureProgressBar");
    }

    public override void _Ready()
    {
        Texture = GetNode<SubViewport>("SubViewport").GetTexture();
    }

    public void SetHealth(int health, int maxHealth)
    {
        progressBar.MaxValue = maxHealth;
        progressBar.Value = health;
    }
}
