using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class TurnManager : Node3D
{
    public delegate TurnState TurnState();
    
    const float showcaseAwaitDuration = .6f;

    private Unit currentUnit;
    private Unit currentTarget;
    private Task currentTask;
    private Vector2I? cursorGridPos;
    private InputManager inputManager;
    private LevelData levelData;
    private List<Unit> enemyUnits = new List<Unit>();

    enum MeshColor { Red, Yellow, Green }

    enum State
    {
        PlayerTurn, PlayerSelectUnit, PlayerUnitContext, PlayerSelectMove, PlayerAwaitMove, PlayerSelectAttack, PlayerAwaitAttack,
        EnemyTurn, AIContext, AIShowWalkable, AIMove, AIShowHittable, AIAttack, AISwap
    }
    StateMachine<State> sm = new StateMachine<State>(State.PlayerSelectUnit);


    public override void _EnterTree()
    {
        inputManager = GetNode<InputManager>("../InputManager");
        levelData = GetNode<LevelData>("../Level");
    }

    public override void _Ready()
    {
        enemyUnits = GetTree().GetNodesInGroup(Unit.GetGroupFrom(FactionType.Enemy))
            .OfType<Unit>().ToList();

        sm.StateChanged += s => GD.Print($"FSM >> Changed to: \"{s}\"");
        InitPlayerStates();
        InitEnemyStates();
    }

    public override void _Process(double delta)
    {
        sm.Process();
    }

    public static async Task AwaitShowcase()
    {
        await Task.Delay(TimeSpan.FromSeconds(showcaseAwaitDuration));
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

    private void ResetTurn(FactionType faction)
    {
        GetTree().CallGroup(Unit.GetGroupFrom(faction), "ResetTurn");
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

    private List<Unit> GetPlayerUnits()
    {
        return GetTree().GetNodesInGroup(Unit.GetGroupFrom(FactionType.Player))
            .OfType<Unit>().ToList();
    }

    #endregion

}
