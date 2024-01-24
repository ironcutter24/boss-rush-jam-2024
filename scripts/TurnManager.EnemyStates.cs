using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class TurnManager : Node3D
{
    private void InitEnemyStates()
    {
        sm.Configure(State.EnemyTurn)
            .OnEntry(() =>
            {
                enemyIndex = 0;
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
                if (enemyIndex < enemyUnits.Count)
                {
                    currentUnit = enemyUnits[enemyIndex];
                }
                enemyIndex++;
            })
            .AddTransition(State.PlayerSelectUnit, () => enemyIndex > enemyUnits.Count)
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
            .AddTransition(State.AISelectSwap, () => currentTask.IsCompleted && currentUnit.HasAttack)
            .AddTransition(State.AIContext, () => currentTask.IsCompleted && !currentUnit.HasAttack);

        sm.Configure(State.AISelectSwap)
            .SubstateOf(State.EnemyTurn)
            .OnEntry(() =>
            {
                var nearbyUnits = LevelData.GetNearbyUnits(currentUnit.GridId);
                if (nearbyUnits.Count <= 0)
                {
                    currentTarget = null;
                    return;
                }

                var rng = new RandomNumberGenerator();
                var randIndex = rng.RandiRange(0, nearbyUnits.Count - 1);
                currentTarget = nearbyUnits[randIndex];

                // Display chosen target
                Mesh m = levelData.GenerateMeshFrom(new List<int> { currentTarget.GridId });
                DisplayMesh(m, MeshColor.Red);
            })
            .OnExit(() =>
            {
                DestroyChildren();
            })
            .AddTransition(State.AIAttack, () =>
            {
                if (inputManager.CellSelected(out cursorGridPos))
                {
                    Unit selectedUnit = levelData.GetUnitAt(cursorGridPos.Value);
                    if (selectedUnit != null &&
                        selectedUnit.Faction == FactionType.Player &&
                        selectedUnit != currentTarget)
                    {
                        // Swap selectedUnit with currentTarget
                        Vector3 posApp = selectedUnit.Position;
                        selectedUnit.Position = currentTarget.Position;
                        currentTarget.Position = posApp;

                        return true;
                    }
                }
                return false;
            })
            .AddTransition(State.AIContext, () => currentTarget == null)
            .AddTransition(State.AIAttack, () => inputManager.Cancel());

        sm.Configure(State.AIAttack)
            .SubstateOf(State.EnemyTurn)
            .OnEntry(() =>
            {
                RefreshGrid();
                currentTask = currentUnit.Attack(currentTarget);
            })
            .AddTransition(State.AIContext, () => currentTask.IsCompleted);
    }

    private static async Task AwaitShowcase()
    {
        await Task.Delay(TimeSpan.FromSeconds(showcaseAwaitDuration));
    }
}
