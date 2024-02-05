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
                SetTurnLabel("Player Turn");
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
            .AddTransition(State.AIInit, () => inputManager.IsEndTurn());

        #endregion

        #region Unit selection / context

        sm.Configure(State.PlayerSelectUnit)
            .SubstateOf(State.PlayerCanEndTurn)
            .OnEntry(() =>
            {
                SetHintLabel("Select a friendly unit to control.");
                ResetCurrentUnit();
                levelData.RefreshGrid();

                unitHUD.Initialize();
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
                SetHintLabel("Press an action key to perform the corresponding action.");

                levelData.RefreshGrid(currentUnit.GridPosition);
                currentUnit.SetSelected(true);

                unitHUD.Initialize(currentUnit as PlayerUnit);
            })
            .AddTransition(State.PlayerSelectMove, () => currentUnit.HasMovement)
            .AddTransition(State.PlayerSelectAttack, () => inputManager.IsAttack() && currentUnit.HasAction)
            .AddTransition(State.PlayerSelectSpecial, () => inputManager.IsSpecial() && currentUnit.HasAction)
            .AddTransition(State.PlayerAwaitReaction, () => inputManager.IsReaction() && currentUnit.HasAction)
            .AddTransition(State.PlayerSelectUnit, () => inputManager.IsCancel());

        #endregion

        #region Movement

        sm.Configure(State.PlayerSelectMove)
            .SubstateOf(State.PlayerCanEndTurn)
            .OnEntry(() =>
            {
                SetHintLabel("Select a tile to move to or press an action button.\nYou won't be able to move after performing an action.");

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
            .AddTransition(State.PlayerSelectSpecial, () => inputManager.IsSpecial() && currentUnit.HasAction)
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

        #endregion

        #region Attack

        sm.Configure(State.PlayerSelectAttack)
            .SubstateOf(State.PlayerCanEndTurn)
            .OnEntry(() =>
            {
                SetHintLabel("Select a unit in range to attack.");

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

        #endregion

        #region Special

        sm.Configure(State.PlayerSelectSpecial)
            .SubstateOf(State.PlayerCanEndTurn)
            .OnEntry(() =>
            {
                SetHintLabel("Select a unit in range to use your special action on it.");

                var playerUnit = currentUnit as PlayerUnit;
                levelData.RefreshGrid();
                var ids = levelData.GetIdsWithMaxDistance(playerUnit, playerUnit.SpecialDistance);
                DisplayMesh(levelData.GenerateMeshFrom(ids), MeshColor.Yellow);
            })
            .OnExit(() => DestroyChildren())
            .AddTransition(State.PlayerAwaitSpecial, () =>
            {
                bool canTransition = !(currentUnit as PlayerUnit).HasSpecialSelection;
                if (canTransition) currentTarget = currentUnit;
                return canTransition;
            })
            .AddTransition(State.PlayerAwaitSpecial, () =>
            {
                GD.Print($"CurrentUnit: {currentUnit != null}");

                var playerUnit = currentUnit as PlayerUnit;
                if (inputManager.IsCellSelected(out cursorGridPos)
                    && cursorGridPos.HasValue
                    && levelData.IsAirDistanceTarget(playerUnit, playerUnit.SpecialDistance, LevelData.GetId(cursorGridPos.Value))
                    )
                {
                    var target = LevelData.GetUnitAtPosition(cursorGridPos.Value);
                    if (target.Faction == FactionType.Player)
                    {
                        currentTarget = target;
                        return true;
                    }
                }
                return false;
            })
            .AddTransition(State.PlayerUnitContext, () => inputManager.IsCancel() && currentUnit.HasMovement)
            .AddTransition(State.PlayerSelectUnit, () => inputManager.IsCancel() && !currentUnit.HasMovement);

        sm.Configure(State.PlayerAwaitSpecial)
            .SubstateOf(State.PlayerTurn)
            .OnEntry(() =>
            {
                currentUnit.ConsumeAction();
                var playerUnit = currentUnit as PlayerUnit;
                currentTask = playerUnit.Special(currentTarget);
            })
            .AddTransition(State.PlayerSelectUnit, () => currentTask.IsCompleted);

        #endregion

        #region Reaction

        sm.Configure(State.PlayerAwaitReaction)
            .SubstateOf(State.PlayerTurn)
            .OnEntry(() =>
            {
                currentUnit.ConsumeAction();
                currentTask = (currentUnit as PlayerUnit).PlanReaction();
            })
            .AddTransition(State.PlayerSelectUnit, () => currentTask.IsCompleted);

        #endregion

    }
}
