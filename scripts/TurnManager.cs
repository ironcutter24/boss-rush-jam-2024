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

    public bool IsPlayerTurn { get; private set; } = true;

    enum State { SelectUnit, UnitContext, SelectMove, AwaitMove, SelectAttack, AwaitAttack }
    StateMachine<State> sm = new StateMachine<State>(State.SelectUnit);


    public override void _EnterTree()
    {
        inputManager = GetNode<InputManager>("../InputManager");
        levelData = GetNode<LevelData>("../Level");
    }

    public override void _Ready()
    {
        InitPlayerStates();
    }

    State previousState;
    public override void _Process(double delta)
    {
        if (inputManager.EndTurn()) SwitchTurnOwner();  // Make base state
        sm.Process();

        if (sm.CurrentState.Key != previousState)
        {
            GD.Print("Entered: " + sm.CurrentState.Key.ToString());
            previousState = sm.CurrentState.Key;
        }
    }

    private void InitPlayerStates()
    {
        sm.Configure(State.SelectUnit)
            .OnEntry(() =>
            {
                currentUnit?.SetSelected(false);
                currentUnit = null;
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
            .OnEntry(() =>
            {
                levelData.RefreshAStar(currentUnit.GridPosition);
                currentUnit.SetSelected(true);
            })
            .AddTransition(State.SelectMove, () => currentUnit.HasMovement)
            .AddTransition(State.SelectAttack, () => inputManager.Attack())
            .AddTransition(State.SelectUnit, () => inputManager.Cancel());

        sm.Configure(State.SelectMove)
            .OnEntry(() =>
            {
                // TODO: Display move UI

                var m = new MeshInstance3D();
                m.Mesh = levelData.GenerateWalkableMesh(currentUnit);
                m.MaterialOverride = GD.Load<Material>("materials/fade_green_mat.tres");
                m.Position = Vector3.Up * .05f;
                AddChild(m);
            })
            .OnExit(() =>
            {
                foreach (var child in GetChildren())
                    child.Free();
            })
            .AddTransition(State.AwaitMove, () =>
            {
                return inputManager.CellSelected(out cursorGridPos)
                    && cursorGridPos.HasValue
                    && levelData.IsReachable(currentUnit, cursorGridPos.Value);
            })
            .AddTransition(State.SelectAttack, () => inputManager.Attack())
            .AddTransition(State.SelectUnit, () => inputManager.Cancel());

        sm.Configure(State.AwaitMove)
            .OnEntry(() =>
            {
                currentUnit.HasMovement = false;
                currentTask = currentUnit.FollowPathTo(cursorGridPos.Value);
            })
            .OnExit(() =>
            {
                levelData.RefreshLevel();
                levelData.RefreshAStar();
            })
            .AddTransition(State.UnitContext, () => currentTask.IsCompleted);

        sm.Configure(State.SelectAttack)
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
            .AddTransition(State.UnitContext, () => inputManager.Cancel());

        sm.Configure(State.AwaitAttack)
            .OnEntry(() => currentUnit.HasAttack = false)
            .OnExit(() =>
            {
                levelData.RefreshLevel();
                levelData.RefreshAStar();
            })
            .AddTransition(State.SelectUnit, () => currentTask.IsCompleted);
    }

    #region Helper methods

    private void SwitchTurnOwner()
    {
        currentUnit?.SetSelected(false);
        currentUnit = null;
        GetTree().CallGroup("units", "ResetTurn");

        //if (IsPlayerTurn) CurrentTurn = EnemyAIRoot;
        //else CurrentTurn = PlayerSelectUnit;

        IsPlayerTurn = !IsPlayerTurn;
    }

    #endregion

}
