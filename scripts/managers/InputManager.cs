using Godot;
using Godot.Collections;
using System;

public partial class InputManager : Node3D
{
    private Vector2I? hitCellPos = null;
    private Node3D marker;

    public override void _EnterTree()
    {
        marker = GetNode<Node3D>("Marker");
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

                int i = Mathf.FloorToInt(point.X);
                int j = Mathf.FloorToInt(point.Z);

                hitCellPos = new Vector2I(i, j);
                marker.Visible = true;
                var worldPos = new Vector3(hitCellPos.Value.X, 0f, hitCellPos.Value.Y);
                marker.GlobalPosition = worldPos;

                //GD.Print($"Ray hit point: {point}");
                //GD.Print($"Selected cell ({i}, {j})");

                return;
            }
        }
        //GD.Print("No hit");
        marker.Visible = false;
        hitCellPos = null;
    }

    #region Input Events

    public bool IsCellSelected(out Vector2I? pos)
    {
        pos = hitCellPos;
        if (pos.HasValue && Input.IsActionJustPressed("select"))
        {
            return true;
        }
        return false;
    }

    public bool IsCancel()
    {
        return Input.IsActionJustPressed("cancel");
    }

    public bool IsAttack()
    {
        return Input.IsActionJustPressed("attack");
    }

    public bool IsSpecial()
    {
        return Input.IsActionJustPressed("special");
    }

    public bool IsReaction()
    {
        return Input.IsActionJustPressed("reaction");
    }

    public bool IsEndTurn()
    {
        return Input.IsActionJustPressed("end_turn");
    }

    #endregion

    #region Helpers

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

    #endregion

}
