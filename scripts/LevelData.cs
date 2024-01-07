using Godot;
using System;

public partial class LevelData : Node3D
{
    private const int NUM_OF_ROWS = 8;
    private const int NUM_OF_COLS = 8;
    private const int CELL_SIZE = 1;

    private AStar3D navGrid = new AStar3D();

    [Export]
    private RayCast3D ray;

    /*
     * Grid in game world:
     * 
     * .^ (Row / i / z)
     * .|
     * 2|
     * 1|
     * 0|
     *  |----------> (Column / j / x)
     *   0 1 2 3 ...
     */
    public CellData[,] Level = new CellData[NUM_OF_ROWS, NUM_OF_COLS];


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        InitAStar(navGrid);

        const int start = 4;
        const int end = 27;
        GD.Print($"Path from {start} to {end}");
        var testPath = navGrid.GetPointPath(start, end);
        foreach(var point in testPath)
        {
            GD.Print(point);
        }
    }

    private void InitAStar(AStar3D aStar)
    {
        for (int i = 0; i < NUM_OF_ROWS; i++)
        {
            for (int j = 0; j < NUM_OF_COLS; j++)
            {
                var worldPos = GetWorldPos(i, j);
                if (!HasColliderAt(worldPos))
                {
                    AddPoint(aStar, i, j, worldPos);
                    InitConnections(aStar, i, j);
                    SpawnSphere(worldPos, .2f);  // Debug only
                }
            }
        }
    }

    private static void AddPoint(AStar3D aStar, int i, int j, Vector3 pos)
    {
        aStar.AddPoint(GetId(i, j), pos);
    }

    private static void InitConnections(AStar3D aStar, int i, int j)
    {
        if (i > 0) ConnectIfValid(aStar, i, j, i - 1, j);
        if (j > 0) ConnectIfValid(aStar, i, j, i, j - 1);
    }

    private static void ConnectIfValid(AStar3D aStar, int i, int j, int toI, int toJ)
    {
        var toId = GetId(toI, toJ);
        if (aStar.HasPoint(toId))
        {
            var fromId = GetId(i, j);
            aStar.ConnectPoints(fromId, toId, true);
        }
        else
        {
            GD.Print($"({i}, {j}) -> ({toI}, {toJ}) | Valid: false");
        }
    }

    private static int GetId(int i, int j)
    {
        return NUM_OF_COLS * i + j;
    }

    private static Vector3 GetWorldPos(int i, int j)
    {
        return new Vector3(j, 0f, i) * CELL_SIZE;
    }

    private bool HasColliderAt(Vector3 pos)
    {
        ray.Position = pos;
        ray.ForceRaycastUpdate();
        return ray.IsColliding();
    }

    private void SpawnSphere(Vector3 pos, float scale)
    {
        var packedScene = GD.Load<PackedScene>("res://scenes/sphere.tscn");
        var shapeInstance = packedScene.Instantiate() as Node3D;
        AddChild(shapeInstance);
        shapeInstance.Position = pos;
        shapeInstance.Scale = Vector3.One * scale;
    }

    public class CellData
    {

    }
}
