using Godot;
using System;
using System.Threading.Tasks;

public partial class TurnManager : Node3D
{
    const float showcaseAwaitDuration = .6f;

    private void InitEnemyStates()
    {
        sm.Configure(State.EnemyTurn)
            .OnEntry(() =>
            {
                GD.Print(">>> Entered Enemy turn");

            })
            .OnExit(() =>
            {
                ResetCurrentUnit();
                Unit.ResetTurn(FactionType.Enemy);
                GD.Print("<<< Exited Enemy turn");
            });

        sm.Configure(State.AIContext)
            .SubstateOf(State.EnemyTurn)
            .OnEntry(() =>
            {
                currentUnit = enemyUnits[0];
            })
            .AddTransition(State.AIShowWalkable, () => true);

        sm.Configure(State.AIShowWalkable)
            .SubstateOf(State.EnemyTurn)
            .OnEntry(() =>
            {
                RefreshGrid();
                DisplayMesh(levelData.GenerateWalkableMesh(currentUnit), MeshColor.Green);
                currentTask = AwaitShowcase();
            })
            .OnExit(() => DestroyChildren())
            .AddTransition(State.AIMove, () => currentTask.IsCompleted && currentUnit.HasMovement)
            .AddTransition(State.AIShowHittable, () => currentTask.IsCompleted && !currentUnit.HasMovement);

        sm.Configure(State.AIMove)
            .SubstateOf(State.EnemyTurn)
            .OnEntry(() =>
            {
                cursorGridPos = DecisionMaker.GetNextCell(currentUnit, levelData);
                currentTask = currentUnit.FollowPathTo(cursorGridPos.Value);
            })
            .AddTransition(State.AIShowHittable, () => currentTask.IsCompleted);

        sm.Configure(State.AIShowHittable)
            .SubstateOf(State.EnemyTurn)
            .OnEntry(() =>
            {
                RefreshGrid();
                DisplayMesh(levelData.GenerateHittableMesh(currentUnit), MeshColor.Red);
                currentTask = AwaitShowcase();
            })
            .OnExit(() => DestroyChildren())
            .AddTransition(State.AIAttack, () => currentTask.IsCompleted && currentUnit.HasAttack)
            .AddTransition(State.AISwap, () => currentTask.IsCompleted && !currentUnit.HasAttack);

        sm.Configure(State.AIAttack)
            .SubstateOf(State.EnemyTurn)
            .OnEntry(() =>
            {
                //currentTask = currentUnit.Attack();
            })
            .AddTransition(State.AISwap, () => true /*currentTask.IsCompleted*/);

        sm.Configure(State.AISwap)
            .SubstateOf(State.EnemyTurn)
            .AddTransition(State.PlayerSelectUnit, () => true);
    }

    private static async Task AwaitShowcase()
    {
        await Task.Delay(TimeSpan.FromSeconds(showcaseAwaitDuration));
    }
}
