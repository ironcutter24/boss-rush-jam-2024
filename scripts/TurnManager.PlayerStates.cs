using Godot;
using System;

public partial class TurnManager : Node3D
{
    private void InitPlayerStates()
    {
        #region Base States

        sm.Configure(State.PlayerTurn)
            .OnEntry(() =>
            {
                GD.Print(">>> Entered Player turn");
            })
            .OnExit(() =>
            {
                ResetCurrentUnit();
                Unit.ResetTurn(FactionType.Player);
                GD.Print("<<< Exited Player turn");
            });

        sm.Configure(State.PlayerCanEndTurn)
            .SubstateOf(State.PlayerTurn)
            .AddTransition(State.AIContext, () => inputManager.EndTurn());

        #endregion

        sm.Configure(State.PlayerSelectUnit)
            .SubstateOf(State.PlayerCanEndTurn)
            .OnEntry(() =>
            {
                ResetCurrentUnit();
                RefreshGrid();
            })
            .AddTransition(State.PlayerUnitContext, () =>
            {
                if (inputManager.CellSelected(out cursorGridPos))
                {
                    Unit selectedUnit = levelData.GetUnitAt(cursorGridPos.Value);
                    if (selectedUnit.Faction == FactionType.Enemy)
                        return false;

                    currentUnit = selectedUnit;
                    GD.Print($"Selected unit at: ({cursorGridPos.Value.X}, {cursorGridPos.Value.Y})");
                    return currentUnit != null;
                }
                return false;
            });

        sm.Configure(State.PlayerUnitContext)
            .SubstateOf(State.PlayerCanEndTurn)
            .OnEntry(() =>
            {
                levelData.RefreshAStar(currentUnit.GridPosition);
                currentUnit.SetSelected(true);
            })
            .AddTransition(State.PlayerSelectMove, () => currentUnit.HasMovement)
            .AddTransition(State.PlayerSelectAttack, () => inputManager.Attack() && currentUnit.HasAttack)
            .AddTransition(State.PlayerSelectUnit, () => inputManager.Cancel());

        sm.Configure(State.PlayerSelectMove)
            .SubstateOf(State.PlayerCanEndTurn)
            .OnEntry(() =>
            {
                RefreshGrid();
                DisplayMesh(levelData.GenerateWalkableMesh(currentUnit), MeshColor.Green);
            })
            .OnExit(() => DestroyChildren())
            .AddTransition(State.PlayerAwaitMove, () =>
            {
                return inputManager.CellSelected(out cursorGridPos)
                    && cursorGridPos.HasValue
                    && cursorGridPos.Value != currentUnit.GridPosition
                    && levelData.IsReachable(currentUnit, cursorGridPos.Value);
            })
            .AddTransition(State.PlayerSelectAttack, () => inputManager.Attack() && currentUnit.HasAttack)
            .AddTransition(State.PlayerSelectUnit, () => inputManager.Cancel());

        sm.Configure(State.PlayerAwaitMove)
            .SubstateOf(State.PlayerTurn)
            .OnEntry(() =>
            {
                currentUnit.ConsumeMovement();
                currentTask = currentUnit.FollowPathTo(cursorGridPos.Value);
            })
            .AddTransition(State.PlayerUnitContext, () => currentTask.IsCompleted);

        sm.Configure(State.PlayerSelectAttack)
            .SubstateOf(State.PlayerCanEndTurn)
            .OnEntry(() =>
            {
                RefreshGrid();
                DisplayMesh(levelData.GenerateHittableMesh(currentUnit), MeshColor.Red);
            })
            .OnExit(() => DestroyChildren())
            .AddTransition(State.PlayerAwaitAttack, () =>
            {
                return inputManager.CellSelected(out cursorGridPos)
                    && levelData.IsHittable(currentUnit, cursorGridPos.Value);
            })
            .AddTransition(State.PlayerUnitContext, () => inputManager.Attack())
            .AddTransition(State.PlayerUnitContext, () => inputManager.Cancel() && currentUnit.HasMovement)
            .AddTransition(State.PlayerSelectUnit, () => inputManager.Cancel() && !currentUnit.HasMovement);

        sm.Configure(State.PlayerAwaitAttack)
            .SubstateOf(State.PlayerTurn)
            .OnEntry(() =>
            {
                currentUnit.ConsumeAttack();
                currentTask = currentUnit.Attack(LevelData.GetUnitAtPosition(cursorGridPos.Value));
            })
            .AddTransition(State.PlayerSelectUnit, () => currentTask.IsCompleted);
    }
}
