using Godot;
using System;
using System.Threading.Tasks;

public partial class TurnManager : Node
{
    private struct TurnTask
    {
        private Task task;
        public TurnState NextState { get; private set; }
        public bool IsCompleted => task.IsCompleted;

        public TurnTask(Task task, TurnState nextState)
        {
            this.task = task;
            NextState = nextState;
        }

        public void Dispose() { task?.Dispose(); }
    }


    public delegate TurnState TurnState();

    private Unit currentUnit;
    private TurnTask? currentTask;
    private Vector2I? cursorGridPos;
    private InputManager inputManager;
    private LevelData levelData;

    public TurnState CurrentTurn { get; private set; }


    public override void _EnterTree()
    {
        inputManager = GetNode<InputManager>("../InputManager");
        levelData = GetNode<LevelData>("../Level");
    }

    public override void _Ready()
    {
        CurrentTurn = PlayerSelectUnit;
    }

    TurnState previousTurn = null;
    public override void _Process(double delta)
    {
        if (previousTurn != CurrentTurn)
        {
            GD.Print($"Turn state: {CurrentTurn.Method.Name}");
            previousTurn = CurrentTurn;
        }
        CurrentTurn = CurrentTurn();
    }

    private TurnState AwaitingTask()
    {
        if (!currentTask.HasValue) throw new System.Exception("currentTask is null");
        if (currentTask.Value.IsCompleted)
        {
            levelData.RefreshLevel();  // Refresh Level to update moved, killed and spawned units

            var nextState = currentTask.Value.NextState;
            currentTask.Value.Dispose();
            currentTask = null;
            return nextState;
        }
        return CurrentTurn;
    }

    #region AI turn states

    #endregion

    #region Player turn states

    private TurnState PlayerSelectUnit()
    {
        currentUnit?.SetSelected(false);
        currentUnit = null;

        if (inputManager.CellSelected(out cursorGridPos))
        {
            currentUnit = levelData.GetUnitAt(cursorGridPos.Value);
            if (currentUnit != null)
            {
                currentUnit.SetSelected(true);
                GD.Print($"Selected unit at: ({cursorGridPos.Value.X}, {cursorGridPos.Value.Y})");

                levelData.RefreshAStar(currentUnit.GridPosition);
                return PlayerContextUnit;
            }
        }

        return CurrentTurn;
    }

    private TurnState PlayerContextUnit()
    {
        if (currentUnit.HasMovement) return PlayerSelectMove;
        if (inputManager.Attack()) return PlayerSelectAttack;
        if (inputManager.Cancel()) return PlayerSelectUnit;
        return CurrentTurn;
    }

    private TurnState PlayerSelectMove()
    {
        // TODO: Display move UI

        if (inputManager.CellSelected(out cursorGridPos))
        {
            Vector3[] path;
            if (cursorGridPos.HasValue && levelData.IsReachable(currentUnit, cursorGridPos.Value, out path))
            {
                //currentUnit.HasMovement = false;
                currentTask = new TurnTask(currentUnit.FollowPath(path), PlayerContextUnit);
                return AwaitingTask;
            }
        }
        if (inputManager.Attack()) return PlayerSelectAttack;
        if (inputManager.Cancel()) return PlayerSelectUnit;

        return CurrentTurn;
    }

    private TurnState PlayerSelectAttack()
    {
        if (inputManager.CellSelected(out cursorGridPos))
        {
            // TODO: Attack unit at location

            return PlayerContextUnit;
        }

        return CurrentTurn;
    }

    #endregion

}
