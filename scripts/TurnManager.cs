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

    private void InitPlayerStates()
    {
        sm.Configure(State.PlayerTurn)
            .OnEntry(() =>
            {
                ResetUnits(FactionType.Player);
                GD.Print(">>> Entered Player turn");
            })
            .OnExit(() =>
            {
                GD.Print("<<< Exited Player turn");
            })
            .AddTransition(State.EnemyTurn, () => Input.IsKeyPressed(Key.A));

        sm.Configure(State.SelectUnit)
            .SubstateOf(State.PlayerTurn)
            .OnEntry(() =>
            {
                ResetUnits(FactionType.Player);
            })
            .AddTransition(State.UnitContext, () =>
            {
                if (inputManager.CellSelected(out cursorGridPos))
                {
                    currentUnit = levelData.GetUnitAt(cursorGridPos.Value);
                    GD.Print($"Selected unit at: ({cursorGridPos.Value.X}, {cursorGridPos.Value.Y})");
                    return currentUnit != null;
                }
                return false;
            });

        sm.Configure(State.UnitContext)
            .SubstateOf(State.PlayerTurn)
            .OnEntry(() =>
            {
                levelData.RefreshAStar(currentUnit.GridPosition);
                currentUnit.SetSelected(true);
            })
            .AddTransition(State.SelectMove, () => currentUnit.HasMovement)
            .AddTransition(State.SelectAttack, () => inputManager.Attack())
            .AddTransition(State.SelectUnit, () => inputManager.Cancel());

        sm.Configure(State.SelectMove)
            .SubstateOf(State.PlayerTurn)
            .OnEntry(() =>
            {
                // TODO: Display move UI

                DisplayMesh(levelData.GenerateWalkableMesh(currentUnit), MeshColor.Green);
            })
            .OnExit(() => DestroyChildren())
            .AddTransition(State.AwaitMove, () =>
            {
                return inputManager.CellSelected(out cursorGridPos)
                    && cursorGridPos.HasValue
                    && cursorGridPos.Value != currentUnit.GridPosition
                    && levelData.IsReachable(currentUnit, cursorGridPos.Value);
            })
            .AddTransition(State.SelectAttack, () => inputManager.Attack())
            .AddTransition(State.SelectUnit, () => inputManager.Cancel());

        sm.Configure(State.AwaitMove)
            .SubstateOf(State.PlayerTurn)
            .OnEntry(() =>
            {
                currentTask = currentUnit.FollowPathTo(cursorGridPos.Value);
            })
            .OnExit(() => RefreshGrid())
            .AddTransition(State.UnitContext, () => currentTask.IsCompleted);

        sm.Configure(State.SelectAttack)
            .OnEntry(() =>
            {
                DisplayMesh(levelData.GenerateHittableMesh(currentUnit), MeshColor.Red);
            })
            .OnExit(() => DestroyChildren())
            .SubstateOf(State.PlayerTurn)
            .AddTransition(State.SelectUnit, () =>
            {
                if (inputManager.CellSelected(out cursorGridPos))
                {
                    // TODO: Check for valid cell
                    // TODO: Attack unit at location

                    return true;
                }
                return false;
            })
            .AddTransition(State.UnitContext, () => !currentUnit.HasAttack)
            .AddTransition(State.UnitContext, () => inputManager.Cancel() && currentUnit.HasMovement)
            .AddTransition(State.SelectUnit, () => inputManager.Cancel() && !currentUnit.HasMovement);

        sm.Configure(State.AwaitAttack)
            .SubstateOf(State.PlayerTurn)
            .OnEntry(() => currentUnit.HasAttack = false)
            .OnExit(() => RefreshGrid())
            .AddTransition(State.SelectUnit, () => currentTask.IsCompleted);
    }

    private void InitEnemyStates()
    {
        sm.Configure(State.EnemyTurn)
            .OnEntry(() =>
            {
                ResetUnits(FactionType.Enemy);
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
