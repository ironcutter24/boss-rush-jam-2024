using Godot;
using System;

public partial class LevelData : Node
{
    private const int NUM_OF_ROWS = 8;
    private const int NUM_OF_COLS = 8;
    private const int CELL_SIZE = 1;

    private AStar3D navGrid = new AStar3D();

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
    }

    private void InitAStar(AStar3D aStar)
    {
        for (int i = 0; i < NUM_OF_ROWS; i++)
        {
            for (int j = 0; j < NUM_OF_COLS; j++)
            {
                // TODO: Add collider check to exclude non-walkable points

                AddPoint(aStar, i, j);
                InitConnections(aStar, i, j);
            }
        }
    }

    private void AddPoint(AStar3D aStar, int i, int j)
    {
        aStar.AddPoint(GetId(i, j), new Vector3(j, 0f, i) * CELL_SIZE);
        SpawnSphere(new Vector3(j, 0f, i) * CELL_SIZE);
    }

    private static void InitConnections(AStar3D aStar, int i, int j)
    {
        if (j > 0) aStar.ConnectPoints(GetId(i, j), GetId(i, --j), true);  // Link to left
        if (i > 0) aStar.ConnectPoints(GetId(i, j), GetId(--i, j), true);  // Link to bottom
    }

    private static int GetId(int i, int j)
    {
        return NUM_OF_COLS * i + j;
    }

    private void SpawnSphere(Vector3 pos)
    {
        var packedScene = GD.Load<PackedScene>("res://scenes/sphere.tscn");
        var shapeInstance = packedScene.Instantiate() as Node3D;
        AddChild(shapeInstance);
        shapeInstance.Position = pos;
        shapeInstance.Scale = Vector3.One * .2f;
    }

    public class CellData
    {

    }
}
