using Godot;
using System;

public partial class UnitIcons3D : Node3D
{
    const int maxSwapCharge = 2;

    private Sprite3D swapCharge1, swapCharge2;
    private int swapChargeLevel;

    [Export] Color baseColor = new Color(0, 0, 0, 1);
    [Export] Color chargedColor = new Color(1, 1, 1, 1);

    public bool HasChargeLeft => swapChargeLevel > 0;

    public override void _Ready()
    {
        swapCharge1 = GetNode<Sprite3D>("%SwapCharge1");
        swapCharge2 = GetNode<Sprite3D>("%SwapCharge2");
        swapChargeLevel = maxSwapCharge;

        Reset();
    }

    public void ConsumeSwapCharge()
    {
        if (swapChargeLevel > 0)
        {
            swapChargeLevel--;
            RefreshGfx();
        }
    }

    public void Reset()
    {
        swapChargeLevel = maxSwapCharge;
        RefreshGfx();
    }

    private void RefreshGfx()
    {
        swapCharge1.Modulate = swapChargeLevel <= 0 ? baseColor : chargedColor;
        swapCharge2.Modulate = swapChargeLevel <= 1 ? baseColor : chargedColor;
    }

}
