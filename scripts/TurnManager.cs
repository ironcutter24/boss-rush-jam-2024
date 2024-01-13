using Godot;
using System;

public partial class TurnManager : Node
{
    public delegate TurnState TurnState();

    private Unit currentUnit;

    [Export] InputManager inputManager;
    [Export] LevelData levelData;

    public TurnState CurrentTurn { get; private set; }


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

    #region AI turn states

    #endregion

    #region Player turn states

    Vector2I? cellPos;

    private TurnState PlayerSelectUnit()
    {
        if (inputManager.CellSelected(out cellPos))
        {
            currentUnit = levelData.GetUnitAt(cellPos.Value);
            if (currentUnit != null)
            {
                GD.Print($"Selected unit at: ({cellPos.Value.X}, {cellPos.Value.Y})");
                return PlayerSelectAction;
            }
        }

        return CurrentTurn;
    }

    private TurnState PlayerSelectAction()
    {
        if (currentUnit.HasMovement) return PlayerSelectMove;
        if (inputManager.Attack()) return PlayerSelectAttack;
        if (inputManager.Cancel()) return PlayerSelectUnit;
        return CurrentTurn;
    }

    private TurnState PlayerSelectMove()
    {
        // Display move UI

        if (inputManager.CellSelected(out cellPos))
        {
            // TODO:
            // Let LevelData validate move and return path
            // Let Unit follow path
            // On completion return PlayerUnitSelected
            if (cellPos.HasValue && levelData.IsReachable(currentUnit, cellPos.Value))
            {
                // Move
                currentUnit.HasMovement = false;
                return PlayerSelectAction;
            }
        }
        if (inputManager.Attack()) return PlayerSelectAttack;
        if (inputManager.Cancel()) return PlayerSelectUnit;

        return CurrentTurn;
    }

    private TurnState PlayerSelectAttack()
    {
        if (inputManager.CellSelected(out cellPos))
        {
            // Attack unit at location

            return PlayerSelectAction;
        }

        return CurrentTurn;
    }

    #endregion
}
