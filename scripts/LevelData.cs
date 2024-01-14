using Godot;
using System;
using System.Collections.Generic;

public partial class LevelData : Node3D
{
    public struct CellData
    {
        public Unit unit = null;

        public CellData() { }
    }

    private const int NUM_OF_ROWS = 8;
    private const int NUM_OF_COLS = 8;
    private const int CELL_SIZE = 1;

    private AStar3D navGrid = new AStar3D();

    [Export] RayCast3D ray;

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


    public override void _Ready()
    {
        InitLevel();
        InitAStar();
        RefreshLevel();
        RefreshAStar();

        //DebugPrintPath(17, 29);
        //DebugPrintReachables(10, 3);

        //GenerateReachableMesh();
    }

    public Unit GetUnitAt(Vector2I pos)
    {
        return Level[pos.X, pos.Y].unit;
    }

    #region Level Initialization

    private void InitLevel()
    {
        for (int i = 0; i < NUM_OF_ROWS; i++)
        {
            for (int j = 0; j < NUM_OF_COLS; j++)
            {
                Level[i, j] = new CellData();
            }
        }
    }

    public void RefreshLevel()
    {
        for (int i = 0; i < NUM_OF_ROWS; i++)
        {
            for (int j = 0; j < NUM_OF_COLS; j++)
            {
                Unit unit;
                if (HasMovableObstacleAt(GetWorldPos(i, j), out unit))
                {
                    Level[i, j].unit = unit;
                    GD.Print($"Unit at: ({i}, {j})");
                }
                else
                {
                    Level[i, j].unit = null;
                }
            }
        }
    }

    #endregion

    #region AStar Initialization

    private void InitAStar()
    {
        for (int i = 0; i < NUM_OF_ROWS; i++)
        {
            for (int j = 0; j < NUM_OF_COLS; j++)
            {
                var worldPos = GetWorldPos(i, j);
                if (!HasStaticObstacleAt(worldPos))
                {
                    AddPoint(navGrid, i, j, worldPos);
                    InitConnections(navGrid, i, j);
                    //SpawnSphere(worldPos, .2f);  // Debug only
                }
            }
        }


        static void AddPoint(AStar3D aStar, int i, int j, Vector3 pos)
        {
            aStar.AddPoint(GetId(i, j), pos);
        }

        static void InitConnections(AStar3D aStar, int i, int j)
        {
            if (i > 0) ConnectIfValid(aStar, i, j, i - 1, j);
            if (j > 0) ConnectIfValid(aStar, i, j, i, j - 1);
        }
    }

    public void RefreshAStar(Vector2I? currentPos = null)
    {
        for (int i = 0; i < NUM_OF_ROWS; i++)
        {
            for (int j = 0; j < NUM_OF_COLS; j++)
            {
                if (!navGrid.HasPoint(GetId(i, j))) continue;

                bool isCurrentPos = currentPos.HasValue ? currentPos.Value.Equals(new Vector2I(i, j)) : false;
                if (!isCurrentPos && Level[i, j].unit != null)
                {
                    navGrid.SetPointDisabled(GetId(i, j));
                }
                else
                {
                    navGrid.SetPointDisabled(GetId(i, j), false);
                }
            }
        }
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

    #endregion

    #region Path validation

    public bool IsReachable(Unit unit, Vector2I pos)
    {
        Vector3[] path;
        return IsReachable(unit, pos, out path);
    }

    public bool IsReachable(Unit unit, Vector2I pos, out Vector3[] path)
    {
        int destId = GetId(pos);
        if (navGrid.HasPoint(destId))
        {
            path = navGrid.GetPointPath(unit.GridId, destId);
            GD.Print($"Path length: {path.Length - 1}");
            return path.Length - 1 <= unit.MoveDistance;
        }
        else
        {
            path = null;
            GD.Print("Path destination is not valid");
            return false;
        }
    }

    private void GenerateReachableMesh()
    {
        List<Vector3> allVertices = new List<Vector3>();
        for (int i = 0; i < NUM_OF_ROWS; i++)
        {
            for (int j = 0; j < NUM_OF_COLS; j++)
            {
                allVertices.AddRange(GetQuadVertices(new Vector3(i, 0, j)));
            }
        }

        // Initialize the ArrayMesh.
        var arrMesh = new ArrayMesh();
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = allVertices.ToArray(); ;

        // Create the Mesh.
        arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        var m = new MeshInstance3D();
        m.Mesh = arrMesh;

        m.Position = Vector3.Up * 1f;
        AddChild(m);
    }

    Vector3[] GetQuadVertices(Vector3 pos)
    {
        var min = pos - Vector3.One * .5f;
        var max = pos + Vector3.One * .5f;

        return new Vector3[]
        {
            new Vector3(min.X, 0, min.Z),
            new Vector3(max.X, 0, min.Z),
            new Vector3(min.X, 0, max.Z),

            new Vector3(max.X, 0, max.Z),
            new Vector3(min.X, 0, max.Z),
            new Vector3(max.X, 0, min.Z),
        };
    }

    private static List<int> GetReachableIds(AStar3D aStar, int id, int maxDist)
    {
        string debug = "";
        var reachables = new List<int>();
        foreach (int testId in GetBoundIds(id, maxDist))
        {
            if (aStar.HasPoint(testId) && aStar.GetIdPath(id, testId).Length - 1 <= maxDist)
            {
                reachables.Add(testId);
                debug += $"{testId} - ";
            }
        }
        return reachables;
    }

    private static List<int> GetBoundIds(int id, int maxDist)
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

    #endregion

    #region Helper methods

    public static int GetId(Vector2I pos)
    {
        return GetId(pos.X, pos.Y);
    }

    public static int GetId(int i, int j)
    {
        return NUM_OF_COLS * i + j;
    }

    public static (int i, int j) GetIndexes(int id)
    {
        return (id / NUM_OF_COLS, id % NUM_OF_COLS);
    }

    public Vector3 GetWorldPos(int id)
    {
        var (i, j) = GetIndexes(id);
        return GetWorldPos(i, j);
    }

    public Vector3 GetWorldPos(Vector2I pos)
    {
        return GetWorldPos(pos.X, pos.Y);
    }

    public Vector3 GetWorldPos(int i, int j)
    {
        return GlobalPosition + new Vector3(i, 0f, j) * CELL_SIZE;
    }

    private bool HasStaticObstacleAt(Vector3 pos)
    {
        ray.Position = pos;
        ray.CollisionMask = 1 << 10;  // Layer 11
        ray.ForceRaycastUpdate();
        return ray.IsColliding();
    }

    private bool HasMovableObstacleAt(Vector3 pos, out Unit unit)
    {
        ray.Position = pos;
        ray.CollisionMask = 1 << 11;  // Layer 12
        ray.ForceRaycastUpdate();
        unit = ray.GetCollider() as Unit;
        return ray.IsColliding();
    }

    #endregion

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

    private void SpawnSphere(Vector3 pos, float scale)
    {
        var packedScene = GD.Load<PackedScene>("res://scenes/debug/sphere.tscn");
        var shapeInstance = packedScene.Instantiate() as Node3D;
        AddChild(shapeInstance);
        shapeInstance.Position = pos;
        shapeInstance.Scale = Vector3.One * scale;
    }

    #endregion

}
