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
                SetTurnLabel("Enemy Turn");
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
            .AddTransition(State.AISpawnMinions, () => currentTask.IsCompleted);

        sm.Configure(State.AISpawnMinions)
            .SubstateOf(State.EnemyTurn)
            .OnEntry(() =>
            {
                currentTask = RefillMinionsAsync();
            })
            .AddTransition(State.AIContext, () => currentTask.IsCompleted);

        sm.Configure(State.AIContext)
            .SubstateOf(State.EnemyTurn)
            .OnEntry(() =>
            {
                // Reset scene if all player units are dead
                if (Unit.GetUnits(FactionType.Player).Count <= 0)
                {
                    GetTree().ReloadCurrentScene();
                }

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

                var mesh = levelData.GenerateHittableMesh(currentUnit);
                if (mesh != null)
                {
                    DisplayMesh(mesh, MeshColor.Red);
                    currentTask = AwaitShowcase();
                }
                else
                {
                    currentTask = null;
                }

            })
            .OnExit(() => DestroyChildren())
            .AddTransition(State.AIContext, () => currentTask == null)
            .AddTransition(State.AISelectSwap, () => currentTask.IsCompleted && currentUnit.HasAction)
            .AddTransition(State.AIContext, () => currentTask.IsCompleted && !currentUnit.HasAction);

        sm.Configure(State.AISelectSwap)
            .SubstateOf(State.EnemyTurn)
            .OnEntry(() =>
            {
                SetHintLabel("Select a friendly unit to swap with, or cancel to continue.");

                swappedUnit = null;

                // Get nearby units
                List<PlayerUnit> nearbyUnits = LevelData.GetNearbyUnits(currentUnit.GridId).OfType<PlayerUnit>().ToList();

                if (nearbyUnits.Count <= 0)
                {
                    currentTarget = null;
                    return;
                }

                var tauntUnits = nearbyUnits.Where(item => item.TauntTurns > 0).ToList();
                currentTarget = GetRandomElement(tauntUnits.Count > 0 ? tauntUnits : nearbyUnits);

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
            .AddTransition(State.AIAwaitReaction, () => currentTarget != null && !(currentTarget as PlayerUnit).HasReactionCharge)
            .AddTransition(State.AIAwaitReaction, () =>
            {
                if (inputManager.IsCellSelected(out cursorGridPos))
                {
                    var swapTarget = levelData.GetUnitAt(cursorGridPos.Value) as PlayerUnit;
                    if (CanSwapWith(swapTarget))
                    {
                        var swapOwner = currentTarget as PlayerUnit;
                        swapOwner.ConsumeReactionCounter();
                        swapTarget.ConsumeReactionCounter();

                        // Swap selectedUnit with currentTarget
                        Vector3 appPos = swapTarget.Position;
                        swapTarget.Position = swapOwner.Position;
                        swapOwner.Position = appPos;
                        currentTarget = swapTarget;
                        swappedUnit = swapOwner;

                        return true;
                    }
                }
                return false;
            })
            .AddTransition(State.AIContext, () => currentTarget == null)
            .AddTransition(State.AIAwaitReaction, () => inputManager.IsCancel());

        sm.Configure(State.AIAwaitReaction)
                .SubstateOf(State.EnemyTurn)
                .OnEntry(() =>
                {
                    var targetUnit = currentTarget as PlayerUnit;
                    if (targetUnit.IsReactionPlanned && currentUnit is EnemyUnit unit)
                    {
                        currentTask = targetUnit.Reaction(swappedUnit, unit);
                    }
                    else
                    {
                        currentTask = null;
                    }
                })
                .AddTransition(State.AIAttack, () => currentTask == null)
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
        if (nextPossessedUnit == null) return;

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

    private async Task RefillMinionsAsync()
    {
        enemyUnits = GetEnemyUnits();
        while (enemyUnits.Length < minionCount)
        {
            await GDTask.DelaySeconds(.4f);

            levelData.RefreshGrid();
            var spawnCellId = levelData.GetRandomFreeCellId();
            var newMinion = minionPackedScene.Instantiate() as Node3D;
            newMinion.GlobalPosition = levelData.GetWorldPos(spawnCellId);
            GetWindow().AddChild(newMinion);

            await GDTask.DelaySeconds(1.2f);

            enemyUnits = GetEnemyUnits();
        }
        await GDTask.NextFrame();
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

    private T GetRandomElement<T>(List<T> list)
    {
        var rng = new RandomNumberGenerator();
        var randIndex = rng.RandiRange(0, list.Count - 1);
        return list[randIndex];
    }

    private bool CanSwapWith(PlayerUnit target)
    {
        return target != null && target != currentTarget && target.HasReactionCharge;
    }
}
