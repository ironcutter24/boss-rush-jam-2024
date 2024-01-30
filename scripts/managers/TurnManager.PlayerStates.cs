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
                Unit.ResetTurn(FactionType.Player);
                GD.Print(">>> Entered Player turn");
            })
            .OnExit(() =>
            {
                ResetCurrentUnit();
                GD.Print("<<< Exited Player turn");
            });

        sm.Configure(State.PlayerCanEndTurn)
            .SubstateOf(State.PlayerTurn)
            .AddTransition(State.AIContext, () => inputManager.IsEndTurn());

        #endregion

        sm.Configure(State.PlayerSelectUnit)
            .SubstateOf(State.PlayerCanEndTurn)
            .OnEntry(() =>
            {
                ResetCurrentUnit();
                levelData.RefreshGrid();
            })
            .AddTransition(State.PlayerUnitContext, () =>
            {
                if (inputManager.IsCellSelected(out cursorGridPos))
                {
                    Unit selectedUnit = levelData.GetUnitAt(cursorGridPos.Value);
                    if (selectedUnit == null || selectedUnit.Faction == FactionType.Enemy)
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
                levelData.RefreshGrid(currentUnit.GridPosition);
                currentUnit.SetSelected(true);

                unitHUD.Initialize(currentUnit as PlayerUnit);
            })
            .AddTransition(State.PlayerSelectMove, () => currentUnit.HasMovement)
            .AddTransition(State.PlayerSelectAttack, () => inputManager.IsAttack() && currentUnit.HasAction)
            //.AddTransition(, () => inputManager.IsSpecial() && currentUnit.HasAction)
            .AddTransition(State.PlayerAwaitReaction, () => inputManager.IsReaction() && currentUnit.HasAction)
            .AddTransition(State.PlayerSelectUnit, () => inputManager.IsCancel());

        sm.Configure(State.PlayerSelectMove)
            .SubstateOf(State.PlayerCanEndTurn)
            .OnEntry(() =>
            {
                levelData.RefreshGrid();
                DisplayMesh(levelData.GenerateWalkableMesh(currentUnit), MeshColor.Green);
            })
            .OnExit(() => DestroyChildren())
            .AddTransition(State.PlayerAwaitMove, () =>
            {
                return inputManager.IsCellSelected(out cursorGridPos)
                    && cursorGridPos.HasValue
                    && cursorGridPos.Value != currentUnit.GridPosition
                    && levelData.IsReachable(currentUnit, cursorGridPos.Value);
            })
            .AddTransition(State.PlayerSelectAttack, () => inputManager.IsAttack() && currentUnit.HasAction)
            .AddTransition(State.PlayerAwaitReaction, () => inputManager.IsReaction() && currentUnit.HasAction)
            .AddTransition(State.PlayerSelectUnit, () => inputManager.IsCancel());

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
                levelData.RefreshGrid();
                DisplayMesh(levelData.GenerateHittableMesh(currentUnit), MeshColor.Red);
            })
            .OnExit(() => DestroyChildren())
            .AddTransition(State.PlayerAwaitAttack, () =>
            {
                return inputManager.IsCellSelected(out cursorGridPos)
                    && levelData.IsHittable(currentUnit, cursorGridPos.Value);
            })
            .AddTransition(State.PlayerUnitContext, () => inputManager.IsAttack())
            .AddTransition(State.PlayerUnitContext, () => inputManager.IsCancel() && currentUnit.HasMovement)
            .AddTransition(State.PlayerSelectUnit, () => inputManager.IsCancel() && !currentUnit.HasMovement);

        sm.Configure(State.PlayerAwaitAttack)
            .SubstateOf(State.PlayerTurn)
            .OnEntry(() =>
            {
                currentUnit.ConsumeAction();
                currentTask = currentUnit.Attack(LevelData.GetUnitAtPosition(cursorGridPos.Value));
            })
            .AddTransition(State.PlayerSelectUnit, () => currentTask.IsCompleted);

        sm.Configure(State.PlayerAwaitReaction)
            .SubstateOf(State.PlayerTurn)
            .OnEntry(() =>
            {
                currentUnit.ConsumeAction();
                currentTask = (currentUnit as PlayerUnit).PlanReaction();
            })
            .AddTransition(State.PlayerUnitContext, () => currentTask.IsCompleted);
    }
}
