using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public partial class TurnManager : Node3D
{
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
                PickNextPossessedUnit();

                Unit.ResetTurn(FactionType.Enemy);
                GD.Print("<<< Exited Enemy turn");
            });

        #endregion

        #region AI init / context

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

        #endregion

        #region Movement

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

        #endregion

        #region Attack / swap

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
                SetHint("Select a friendly unit to swap with, or cancel to continue.");

                // Get nearby units
                var nearbyUnits = LevelData.GetNearbyUnits(currentUnit.GridId);
                if (nearbyUnits.Count <= 0)
                {
                    currentTarget = null;
                    return;
                }

                // Set target to random nearby unit
                var rng = new RandomNumberGenerator();
                var randIndex = rng.RandiRange(0, nearbyUnits.Count - 1);
                currentTarget = nearbyUnits[randIndex];

                // Look at target
                currentUnit.FaceTowards(currentTarget);

                // Display chosen target
                Mesh m = levelData.GenerateMeshFrom(new List<int> { currentTarget.GridId });
                DisplayMesh(m, MeshColor.Red);
            })
            .OnExit(() =>
            {
                DestroyChildren();
            })
            .AddTransition(State.AIAttack, () => currentTarget != null && !(currentTarget as PlayerUnit).HasReactionCharge)
            .AddTransition(State.AIAwaitReaction, () =>
            {
                if (inputManager.IsCellSelected(out cursorGridPos))
                {
                    var swapTarget = levelData.GetUnitAt(cursorGridPos.Value) as PlayerUnit;
                    if (swapTarget != null &&
                        swapTarget != currentTarget &&
                        swapTarget.HasReactionCharge
                        )
                    {
                        var swapOwner = currentTarget as PlayerUnit;

                        swapOwner.ConsumeReactionCounter();
                        swapTarget.ConsumeReactionCounter();

                        // Swap selectedUnit with currentTarget
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

        #endregion
    }

    #region Helpers

    private async Task BossTransitionAsync()
    {
        EnemyUnit nextPossessed = nextPossessedUnit as EnemyUnit;
        var possessedUnit = enemyUnits.FirstOrDefault(unit => unit.IsPossessed);
        if (possessedUnit != nextPossessed)
        {
            if (possessedUnit != null)
            {
                await possessedUnit.SetPossessed(false);
            }
            await GDTask.DelaySeconds(.4f);
            await nextPossessed.SetPossessed(true);
        }
    }

    private static async Task AwaitShowcase()
    {
        await Task.Delay(TimeSpan.FromSeconds(showcaseAwaitDuration));
    }

    private static EnemyUnit[] GetEnemyUnits()
    {
        return Unit.GetUnits(FactionType.Enemy).OfType<EnemyUnit>().ToArray();
    }

    #endregion

    private void PickNextPossessedUnit()
    {
        enemyUnits = GetEnemyUnits();

        var rng = new RandomNumberGenerator();
        var randIndex = rng.RandiRange(0, enemyUnits.Length - 1);
        nextPossessedUnit = enemyUnits[randIndex];

        var currentPossessedUnit = enemyUnits.Where(item => item.IsPossessed).FirstOrDefault();
        if (nextPossessedUnit != currentPossessedUnit)
        {
            (nextPossessedUnit as EnemyUnit).SetNextPossessed();
        }
    }

}
