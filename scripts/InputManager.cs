using Godot;
using Godot.Collections;
using System;

public partial class InputManager : Node3D
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("mouse_left"))
        {
            var hits = CastRayFromScreen(1 << 8);  // Layer 9 -> Ground
            if (hits.Count > 0)
            {
                var point = hits["position"].AsVector3();
                if (point.Y == 0f)
                {
                    point += new Vector3(1, 0, 1) * .5f;
                    GD.Print($"Ray hit point: {point}");

                    int i = Mathf.FloorToInt(point.X);
                    int j = Mathf.FloorToInt(point.Z);
                    GD.Print($"Selected cell ({i}, {j})");

                    return;
                }
            }
            GD.Print("No hit");
        }

        if (Input.IsActionJustPressed("mouse_right"))
        {

        }

        if (Input.IsActionJustPressed("quit"))
        {
            GetTree().Quit();
        }
    }

    private Dictionary CastRayFromScreen(uint mask = 1)
    {
        const float rayLength = 100f;
        var mousePos = GetViewport().GetMousePosition();
        var cam = GetViewport().GetCamera3D();

        var rayQuery = new PhysicsRayQueryParameters3D();
        rayQuery.From = cam.ProjectRayOrigin(mousePos);
        rayQuery.To = rayQuery.From + cam.ProjectRayNormal(mousePos) * rayLength;
        rayQuery.CollideWithAreas = true;
        rayQuery.CollideWithBodies = false;
        rayQuery.CollisionMask = mask;

        var space = GetWorld3D().DirectSpaceState;
        return space.IntersectRay(rayQuery);
    }
}
