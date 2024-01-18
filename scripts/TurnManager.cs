using Godot;
using System;
using System.Threading.Tasks;

public partial class TurnManager : Node3D
{
    public delegate TurnState TurnState();

    private Unit currentUnit;
    private Task currentTask;
    private Vector2I? cursorGridPos;
    private InputManager inputManager;
    private LevelData levelData;

    enum MeshColor { Red, Yellow, Green }

    enum State
    {
        PlayerTurn, SelectUnit, UnitContext, SelectMove, AwaitMove, SelectAttack, AwaitAttack,
        EnemyTurn
    }
    StateMachine<State> sm = new StateMachine<State>(State.SelectUnit);


    public override void _EnterTree()
    {
        inputManager = GetNode<InputManager>("../InputManager");
        levelData = GetNode<LevelData>("../Level");
    }

    public override void _Ready()
    {
        sm.StateChanged += s => GD.Print($"FSM >> Changed to: \"{s}\"");
        InitPlayerStates();
    }

    public override void _Process(double delta)
    {
        sm.Process();
    }

    private void InitEnemyStates()
    {
        sm.Configure(State.EnemyTurn)
            .OnEntry(() =>
            {
                ResetUnits(FactionType.Enemy);
                ResetTurn(FactionType.Enemy);
                GD.Print(">>> Entered Enemy turn");
            })
            .OnExit(() =>
            {
                GD.Print(">>> Exited Enemy turn");
            });
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

    private void ResetUnits(FactionType faction)
    {
        currentUnit?.SetSelected(false);
        currentUnit = null;
    }

    private void ResetTurn(FactionType faction)
    {
        GetTree().CallGroup(GetGroupFrom(faction), "ResetTurn");
    }

    private void RefreshGrid()
    {
        levelData.RefreshLevel();
        levelData.RefreshAStar();
    }

    private StringName GetGroupFrom(FactionType faction)
    {
        return $"{faction.ToString().ToLower()}_units";
    }

    private Material GetMaterialFrom(MeshColor color)
    {
        return GD.Load<Material>($"materials/fade_{color.ToString().ToLower()}_mat.tres");
    }

    #endregion

}
