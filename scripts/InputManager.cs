using Godot;
using Godot.Collections;
using System;

public partial class InputManager : Node3D
{
    [Export]
    private Node3D marker;

    private Vector3? hitCellPos = null;

    public override void _Ready()
    {
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("mouse_left"))
        {
            if (hitCellPos.HasValue)
            {

            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        var hits = CastRayFromScreen(1 << 8);  // Layer 9 -> "Ground"
        if (hits.Count > 0)
        {
            var point = hits["position"].AsVector3();
            if (Mathf.IsEqualApprox(point.Y, 0f))
            {
                point += new Vector3(1, 0, 1) * .5f;
                GD.Print($"Ray hit point: {point}");

                int i = Mathf.FloorToInt(point.X);
                int j = Mathf.FloorToInt(point.Z);

                hitCellPos = new Vector3(i, 0f, j);
                marker.Visible = true;
                marker.GlobalPosition = hitCellPos.Value;
                GD.Print($"Selected cell ({i}, {j})");

                return;
            }
        }
        //GD.Print("No hit");
        marker.Visible = false;
        hitCellPos = null;
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
