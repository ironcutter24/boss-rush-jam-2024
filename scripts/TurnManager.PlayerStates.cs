using Godot;
using System;

public partial class TurnManager : Node3D
{

    private void InitPlayerStates()
    {
        sm.Configure(State.PlayerTurn)
            .OnEntry(() =>
            {
                ResetCurrentUnit();
                ResetTurn(FactionType.Player);
                GD.Print(">>> Entered Player turn");
            })
            .OnExit(() =>
            {
                GD.Print("<<< Exited Player turn");
            })
            .AddTransition(State.EnemyAI, () => Input.IsKeyPressed(Key.A));

        sm.Configure(State.SelectUnit)
            .SubstateOf(State.PlayerTurn)
            .OnEntry(() => ResetCurrentUnit())
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
            .AddTransition(State.SelectAttack, () => inputManager.Attack() && currentUnit.HasAttack)
            .AddTransition(State.SelectUnit, () => inputManager.Cancel());

        sm.Configure(State.SelectMove)
            .SubstateOf(State.PlayerTurn)
            .OnEntry(() =>
            {
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
            .AddTransition(State.SelectAttack, () => inputManager.Attack() && currentUnit.HasAttack)
            .AddTransition(State.SelectUnit, () => inputManager.Cancel());

        sm.Configure(State.AwaitMove)
            .SubstateOf(State.PlayerTurn)
            .OnEntry(() =>
            {
                currentUnit.ConsumeMovement();
                currentTask = currentUnit.FollowPathTo(cursorGridPos.Value);
            })
            .OnExit(() => RefreshGrid())
            .AddTransition(State.UnitContext, () => currentTask.IsCompleted);

        sm.Configure(State.SelectAttack)
            .SubstateOf(State.PlayerTurn)
            .OnEntry(() =>
            {
                DisplayMesh(levelData.GenerateHittableMesh(currentUnit), MeshColor.Red);
            })
            .OnExit(() => DestroyChildren())
            .AddTransition(State.AwaitAttack, () =>
            {
                return inputManager.CellSelected(out cursorGridPos)
                    && levelData.IsHittable(currentUnit, cursorGridPos.Value);
            })
            .AddTransition(State.UnitContext, () => inputManager.Attack())
            .AddTransition(State.UnitContext, () => inputManager.Cancel() && currentUnit.HasMovement)
            .AddTransition(State.SelectUnit, () => inputManager.Cancel() && !currentUnit.HasMovement);

        sm.Configure(State.AwaitAttack)
            .SubstateOf(State.PlayerTurn)
            .OnEntry(() =>
            {
                currentUnit.ConsumeAttack();
                currentTask = currentUnit.Attack(LevelData.GetUnitAtPosition(cursorGridPos.Value));
            })
            .OnExit(() => RefreshGrid())
            .AddTransition(State.SelectUnit, () => currentTask.IsCompleted);
    }

}
