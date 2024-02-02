using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public partial class TurnManager : Node3D
{
    private async Task BossTransitionAsync()
    {
        var nextPossessedUnit = enemyUnits.MaxBy(unit => LevelData.CountNearbyUnits(unit.GridId));
        var possessedUnit = enemyUnits.FirstOrDefault(unit => unit.IsPossessed);
        if (possessedUnit != nextPossessedUnit)
        {
            if (possessedUnit != null)
            {
                await possessedUnit.SetPossessed(false);
            }
            await GDTask.DelaySeconds(.4f);
            await nextPossessedUnit.SetPossessed(true);
        }
    }


    private void InitEnemyStates()
    {
        #region Base States

        sm.Configure(State.EnemyTurn)
            .OnEntry(() =>
            {
                enemyUnits = GetEnemyUnits();
                enemyIndex = 0;
                GD.Print(">>> Entered Enemy turn");
            })
            .OnExit(() =>
            {
                ResetCurrentUnit();
                Unit.ResetTurn(FactionType.Enemy);
                GD.Print("<<< Exited Enemy turn");
            });

        #endregion

        sm.Configure(State.AIInit)
            .SubstateOf(State.EnemyTurn)
            .OnEntry(() =>
            {
                currentTask = BossTransitionAsync();
            })
            .AddTransition(State.AIContext, () => currentTask.IsCompleted);

        sm.Configure(State.AIContext)
            .SubstateOf(State.EnemyTurn)
            .OnEntry(() =>
            {
                enemyUnits = GetEnemyUnits();
                if (enemyIndex < enemyUnits.Length)
                {
                    currentUnit = enemyUnits[enemyIndex];
                }
                enemyIndex++;
            })
            .AddTransition(State.PlayerSelectUnit, () => enemyIndex > enemyUnits.Length)
            .AddTransition(State.AIShowWalkable, () => true);

        sm.Configure(State.AIShowWalkable)
            .SubstateOf(State.EnemyTurn)
            .OnEntry(() =>
            {
                levelData.RefreshGrid();
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
                levelData.RefreshGrid();
                DisplayMesh(levelData.GenerateHittableMesh(currentUnit), MeshColor.Red);
                currentTask = AwaitShowcase();
            })
            .OnExit(() => DestroyChildren())
            .AddTransition(State.AISelectSwap, () => currentTask.IsCompleted && currentUnit.HasAction)
            .AddTransition(State.AIContext, () => currentTask.IsCompleted && !currentUnit.HasAction);

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

                currentUnit.FaceTowards(currentTarget);

                // Display chosen target
                Mesh m = levelData.GenerateMeshFrom(new List<int> { currentTarget.GridId });
                DisplayMesh(m, MeshColor.Red);
            })
            .OnExit(() =>
            {
                DestroyChildren();
            })
            .AddTransition(State.AIAwaitReaction, () =>
            {
                if (inputManager.IsCellSelected(out cursorGridPos))
                {
                    Unit selectedUnit = levelData.GetUnitAt(cursorGridPos.Value);
                    if (selectedUnit != null &&
                        selectedUnit.Faction == FactionType.Player &&
                        selectedUnit != currentTarget)
                    {
                        // Swap selectedUnit with currentTarget
                        var swapTarget = selectedUnit as PlayerUnit;
                        var swapOwner = currentTarget as PlayerUnit;

                        Vector3 appPos = swapTarget.Position;
                        swapTarget.Position = swapOwner.Position;
                        swapOwner.Position = appPos;
                        currentTarget = swapTarget;

                        //levelData.RefreshGrid();

                        if (swapTarget.IsReactionPlanned && currentUnit is EnemyUnit)
                        {
                            currentTask = swapTarget.Reaction(swapOwner, currentUnit as EnemyUnit);
                        }
                        else
                        {
                            currentTask = null;
                        }

                        return true;
                    }
                }
                return false;
            })
            .AddTransition(State.AIContext, () => currentTarget == null)
            .AddTransition(State.AIAttack, () => inputManager.IsCancel());

        sm.Configure(State.AIAwaitReaction)
            .SubstateOf(State.EnemyTurn)
            .AddTransition(State.AIAttack, () =>
            {
                var unit = currentTarget as PlayerUnit;
                return unit != null && !unit.IsReactionPlanned;
            })
            .AddTransition(State.AIAttack, () => currentTask != null && currentTask.IsCompleted);

        sm.Configure(State.AIAttack)
            .SubstateOf(State.EnemyTurn)
            .OnEntry(() =>
            {
                if (!currentUnit.HasAction) return;

                levelData.RefreshGrid();
                currentTask = currentUnit.Attack(currentTarget);
            })
            .AddTransition(State.AIContext, () => currentTask.IsCompleted)
            .AddTransition(State.AIContext, () => !currentUnit.HasAction);
    }

    private static EnemyUnit[] GetEnemyUnits()
    {
        return Unit.GetUnits(FactionType.Enemy).OfType<EnemyUnit>().ToArray();
    }

    private static async Task AwaitShowcase()
    {
        await Task.Delay(TimeSpan.FromSeconds(showcaseAwaitDuration));
    }
}
