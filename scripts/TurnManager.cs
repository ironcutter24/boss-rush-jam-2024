using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class TurnManager : Node3D
{
    public delegate TurnState TurnState();

    private Unit currentUnit, currentTarget;
    private Task currentTask;
    private Vector2I? cursorGridPos;
    private InputManager inputManager;
    private LevelData levelData;
    private List<Unit> enemyUnits = new List<Unit>();

    enum MeshColor { Red, Yellow, Green }
    enum State
    {
        // Player
        PlayerTurn, PlayerCanEndTurn,  // Base states
        PlayerSelectUnit, PlayerUnitContext, PlayerSelectMove, PlayerAwaitMove, PlayerSelectAttack, PlayerAwaitAttack,

        // Enemy
        EnemyTurn,  // Base states
        AIContext, AIShowWalkable, AIMove, AIShowHittable, AIAttack, AISwap
    }
    StateMachine<State> sm = new StateMachine<State>(State.PlayerSelectUnit);


    public override void _EnterTree()
    {
        inputManager = GetNode<InputManager>("../InputManager");
        levelData = GetNode<LevelData>("../Level");
    }

    public override void _Ready()
    {
        enemyUnits = Unit.GetUnits(FactionType.Enemy);

        InitPlayerStates();
        InitEnemyStates();

        sm.StateChanged += s => GD.Print($"FSM >> Changed to: \"{s}\"");
    }

    public override void _Process(double delta)
    {
        sm.Process();
    }

    #region Helper methods

    private void DisplayMesh(Mesh mesh, MeshColor color)
    {
        var m = new MeshInstance3D();
        m.Mesh = mesh;
        m.MaterialOverride = GetMaterialFrom(color);
        m.Position = Vector3.Up * .05f;
        AddChild(m);
    }

    private void DestroyChildren()
    {
        foreach (var child in GetChildren())
        {
            child.Free();
        }
    }

    private void ResetCurrentUnit()
    {
        currentUnit?.SetSelected(false);
        currentUnit = null;
    }

    private void RefreshGrid()
    {
        levelData.RefreshLevel();
        levelData.RefreshAStar();
    }

    private Material GetMaterialFrom(MeshColor color)
    {
        return GD.Load<Material>($"materials/fade_{color.ToString().ToLower()}_mat.tres");
    }

    #endregion

}
