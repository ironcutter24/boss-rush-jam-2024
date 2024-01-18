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

    public static LevelData Instance { get; private set; }

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        InitLevel();
        InitAStar();
        RefreshLevel();
        RefreshAStar();
    }

    public Unit GetUnitAt(Vector2I pos)
    {
        return Level[pos.X, pos.Y].unit;
    }

    #region Initialization

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

        static void ConnectIfValid(AStar3D aStar, int i, int j, int toI, int toJ)
        {
            var toId = GetId(toI, toJ);
            if (aStar.HasPoint(toId))
            {
                var fromId = GetId(i, j);
                aStar.ConnectPoints(fromId, toId, true);
            }
        }
    }

    #endregion

    #region Board refresh

    public void RefreshLevel()
    {
        for (int i = 0; i < NUM_OF_ROWS; i++)
        {
            for (int j = 0; j < NUM_OF_COLS; j++)
            {
                Unit unit;
                bool hasObstacle = HasMovableObstacleAt(GetWorldPos(i, j), out unit);
                Level[i, j].unit = hasObstacle ? unit : null;
            }
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
                bool isWalkable = isCurrentPos || Level[i, j].unit == null;
                navGrid.SetPointDisabled(GetId(i, j), !isWalkable);
            }
        }
    }

    #endregion

    #region Path validation

    public Vector3[] GetPath(Unit unit, Vector2I pos)
    {
        Vector3[] path;
        IsReachable(unit, pos, out path);
        return path;
    }

    public bool IsReachable(Unit unit, Vector2I pos)
    {
        Vector3[] path;
        return IsReachable(unit, pos, out path);
    }

    public bool IsReachable(Unit unit, Vector2I pos, out Vector3[] path)
    {
        int destId = GetId(pos);
        if (navGrid.HasPoint(destId) && !navGrid.IsPointDisabled(destId))
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

    private static List<int> GetReachableIds(AStar3D aStar, int id, int maxDist)
    {
        string debug = "";
        var reachables = new List<int>();
        foreach (int testId in GetBoundIds(id, maxDist))
        {
            if (aStar.HasPoint(testId) && !aStar.IsPointDisabled(testId) && aStar.GetIdPath(id, testId).Length - 1 <= maxDist)
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

    #region Attack validation

    private static List<int> GetHittableIds(AStar3D aStar, Unit unit)
    {
        if (unit.AttackDistance <= 0) return null;

        var hittables = new List<int>();
        hittables.AddRange(GetLineOfSightIds(aStar, unit, Vector2I.Up));
        hittables.AddRange(GetLineOfSightIds(aStar, unit, Vector2I.Down));
        hittables.AddRange(GetLineOfSightIds(aStar, unit, Vector2I.Right));
        hittables.AddRange(GetLineOfSightIds(aStar, unit, Vector2I.Left));
        return hittables;
    }

    private static List<int> GetLineOfSightIds(AStar3D aStar, Unit unit, Vector2I dir)
    {
        var visibles = new List<int>();
        for (int i = 1; i <= unit.AttackDistance; i++)
        {
            var pos = unit.GridPosition + dir * i;

            if (!IsWithinBounds(pos))
                break;

            var targetId = GetId(pos);
            if (!aStar.HasPoint(targetId))
                break;

            Unit target = GetUnitAtPosition(pos);
            if (target != null)
            {
                if (target.Faction == unit.Faction)
                    break;

                visibles.Add(targetId);
                break;
            }

            visibles.Add(targetId);
        }

        return visibles;
    }

    #endregion

    #region Mesh Generation

    public Mesh GenerateWalkableMesh(Unit unit)
    {
        var walkables = GetReachableIds(navGrid, unit.GridId, unit.MoveDistance);
        return GenerateMeshFrom(walkables);
    }

    public Mesh GenerateHittableMesh(Unit unit)
    {
        var hittables = GetHittableIds(navGrid, unit);
        return GenerateMeshFrom(hittables);
    }

    private Mesh GenerateMeshFrom(List<int> ids)
    {
        List<Vector3> allVertices = new List<Vector3>();

        foreach (var id in ids)
        {
            var (i, j) = GetIndexes(id);
            allVertices.AddRange(GetQuadVertices(new Vector3(i, 0, j)));
        }

        // Initialize the ArrayMesh.
        var arrMesh = new ArrayMesh();
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = allVertices.ToArray(); ;

        // Create the Mesh.
        arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return arrMesh;
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

    private static bool IsWithinBounds(Vector2I pos)
    {
        return pos.X >= 0 && pos.X < Instance.Level.GetLength(0) && pos.Y >= 0 && pos.Y < Instance.Level.GetLength(1);
    }

    private static Unit GetUnitAtPosition(Vector2I pos)
    {
        try { return Instance.Level[pos.X, pos.Y].unit; }
        catch (IndexOutOfRangeException) { return null; }
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
