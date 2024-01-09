using Godot;
using System;
using System.Collections.Generic;

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
     * .^ (Row / i / x)
     * .|
     * 2|
     * 1|
     * 0|
     *  |----------> (Column / j / z)
     *   0 1 2 3 ...
     */
    public CellData[,] Level = new CellData[NUM_OF_ROWS, NUM_OF_COLS];


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        InitAStar(navGrid);

        //DebugPrintPath(17, 29);
        //DebugPrintReachables(19, 3);
    }

    private void InitAStar(AStar3D aStar)
    {
        for (int i = 0; i < NUM_OF_ROWS; i++)
        {
            for (int j = 0; j < NUM_OF_COLS; j++)
            {
                var worldPos = GetWorldPos(i, j);
                if (!HasObstacleAt(worldPos))
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

    private static List<int> GetReachableIds(AStar3D aStar, int id, int maxDist)
    {
        var reachable = new List<int>();
        foreach (int testId in GetBoundIds(id, maxDist))
        {
            if (aStar.HasPoint(testId) && aStar.GetIdPath(id, testId).Length - 1 <= maxDist)
            {
                reachable.Add(testId);
            }
        }
        return reachable;


        static List<int> GetBoundIds(int id, int maxDist)
        {
            var (x, z) = GetIndexes(id);
            var bounds = new List<int>();
            for (int i = 0; i < NUM_OF_ROWS; i++)
            {
                for (int j = 0; j < NUM_OF_COLS; j++)
                {
                    if (i == x && j == z) continue;
                    if (Mathf.Abs(x - i) <= maxDist && Mathf.Abs(z - j) <= maxDist)
                    {
                        bounds.Add(GetId(i, j));
                    }
                }
            }
            return bounds;
        }
    }

    private static int GetId(int i, int j)
    {
        return NUM_OF_COLS * i + j;
    }

    private static (int i, int j) GetIndexes(int id)
    {
        return (id / NUM_OF_COLS, id % NUM_OF_COLS);
    }

    private Vector3 GetWorldPos(int id)
    {
        var (i, j) = GetIndexes(id);
        return GetWorldPos(i, j);
    }

    private Vector3 GetWorldPos(int i, int j)
    {
        return GlobalPosition + new Vector3(j, 0f, i) * CELL_SIZE;
    }

    private bool HasObstacleAt(Vector3 pos)
    {
        ray.Position = pos;
        ray.ForceRaycastUpdate();
        return ray.IsColliding();
    }

    private void SpawnSphere(Vector3 pos, float scale)
    {
        var packedScene = GD.Load<PackedScene>("res://scenes/debug/sphere.tscn");
        var shapeInstance = packedScene.Instantiate() as Node3D;
        AddChild(shapeInstance);
        shapeInstance.Position = pos;
        shapeInstance.Scale = Vector3.One * scale;
    }

    #region Debug methods

    private void DebugPrintPath(int start, int end)
    {
        SpawnSphere(GetWorldPos(start), .6f);
        SpawnSphere(GetWorldPos(end), .6f);

        GD.Print($"Path from {start} to {end}");
        var path = navGrid.GetPointPath(start, end);
        foreach (var point in path)
        {
            GD.Print(point);
            SpawnSphere(point, .4f);
        }
    }

    private void DebugPrintReachables(int id, int maxDist)
    {
        SpawnSphere(GetWorldPos(id), .6f);
        foreach (var testId in GetReachableIds(navGrid, id, maxDist))
        {
            var (i, j) = GetIndexes(testId);
            var pos = GetWorldPos(i, j);
            SpawnSphere(pos, .4f);
        }
    }

    #endregion

    public class CellData
    {

    }
}
