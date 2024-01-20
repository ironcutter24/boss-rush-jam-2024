using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class TurnManager : Node3D
{

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
                ResetTurn(FactionType.Enemy);
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
                cursorGridPos = RunUtilityDecisionMaker(currentUnit);
                currentTask = currentUnit.FollowPathTo(cursorGridPos.Value);
            })
            .OnExit(() => RefreshGrid())
            .AddTransition(State.AIShowHittable, () => currentTask.IsCompleted);

        sm.Configure(State.AIShowHittable)
            .SubstateOf(State.EnemyTurn)
            .OnEntry(() =>
            {
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
            .OnExit(() => RefreshGrid())
            .AddTransition(State.AISwap, () => true /*currentTask.IsCompleted*/);

        sm.Configure(State.AISwap)
            .SubstateOf(State.EnemyTurn)
            .AddTransition(State.PlayerSelectUnit, () => true);
    }

    #region Utility-Based decision maker

    private Vector2I RunUtilityDecisionMaker(Unit unit)
    {
        Dictionary<int, int> cellScores = new Dictionary<int, int>();

        var reachableIds = levelData.GetReachableIds(unit);
        reachableIds.ForEach(id => cellScores.Add(id, 0));

        // Calculate nearby players for all valid movement cells
        // 0->0 1->2, 2->0, 3->-2 etc...
        foreach (var id in reachableIds)
        {
            int nearby = CountNearbyUnits(id);
            if (nearby > 0)
            {
                cellScores[id] += (2 - nearby) * 2;
            }
        }

        // Calculate scores influenced by player units
        var playerUnits = GetPlayerUnits();
        foreach (var playerUnit in playerUnits)
        {
            UpdateScores(unit, playerUnit, ref cellScores);
        }

        // Print matrix to console
        GD.PrintRich("[b]Unit utility scores:[/b]");
        DebugPrintCellScores(unit.GridId, cellScores);

        cellScores = ApplyKernel(cellScores, kernel, LevelData.NUM_OF_ROWS, LevelData.NUM_OF_COLS);

        // Print matrix to console
        GD.PrintRich("[b]Convolution:[/b]");
        DebugPrintCellScores(unit.GridId, cellScores);

        // Sort by score
        cellScores = cellScores.OrderBy(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);

        // Return grid position for highest score
        var (i, j) = LevelData.GetIndexes(cellScores.Last().Key);
        return new Vector2I(i, j);
    }

    #endregion

    #region AI helpers

    private void UpdateScores(Unit unit, Unit playerUnit, ref Dictionary<int, int> scores)
    {
        // Reduce score of cells where can get attacked
        var cells = levelData.GetHittableCells(playerUnit);
        foreach (var cell in cells)
        {
            if (scores.ContainsKey(cell.id))
                scores[cell.id] -= cell.dist - 1;  // Distance 1 has no impact on weight
        }

        // Increase score of cells that get you closer to a target
        var path = levelData.GetPath(unit, playerUnit.GridPosition);
        if (path != null && path.Length > 0)
        {
            for (int i = 1; i < path.Length; i++)  // Ignore first element == unit position
            {
                int id = levelData.GetId(path[i]);
                if (scores.ContainsKey(id))
                    scores[id] += i;
            }
        }
    }

    int CountNearbyUnits(int cellId)
    {
        int count = 0;
        var (iCell, jCell) = LevelData.GetIndexes(cellId);
        for (int i = iCell - 1; i < iCell + 2; i++)
        {
            for (int j = jCell - 1; j < jCell + 2; j++)
            {
                if ((i - iCell + j - jCell) % 2 == 0) continue;

                var unit = LevelData.GetUnitAtPosition(new Vector2I(i, j));
                if (unit != null && unit.Faction == FactionType.Player)
                    count++;
            }
        }
        return count;
    }

    #endregion

    #region Matrix operations

    //readonly int[,] kernel =
    //{
    //    { 0, 0, 1, 0, 0},
    //    { 0, 1, 2, 1, 0},
    //    { 1, 2, 5, 2, 1},
    //    { 0, 1, 2, 1, 0},
    //    { 0, 0, 1, 0, 0}
    //};

    readonly int[,] kernel =
    {
        { 0, 0, 0, 0, 0},
        { 0, 0, 1, 0, 0},
        { 0, 1, 3, 1, 0},
        { 0, 0, 1, 0, 0},
        { 0, 0, 0, 0, 0}
    };

    private Dictionary<int, int> ApplyKernel(Dictionary<int, int> inputMatrix, int[,] kernel, int matrixHeight, int matrixWidth)
    {
        Dictionary<int, int> outputMatrix = new Dictionary<int, int>(inputMatrix.ToDictionary(entry => entry.Key, entry => entry.Value));
        string debugStr = "";

        foreach (var cell in inputMatrix)
        {
            // Grid coordinates for cell ID
            var (x, y) = LevelData.GetIndexes(cell.Key);

            debugStr += $"Grid pos: ({x}, {y})\n";

            int sum = 0;
            for (int i = 0; i < kernel.GetLength(0); i++)
            {
                for (int j = 0; j < kernel.GetLength(1); j++)
                {
                    // Grid coordinates with kernel offset
                    int xMatrix = x - i + 2;
                    int yMatrix = y - j + 2;

                    if (LevelData.IsWithinBounds(xMatrix, yMatrix))
                    {
                        int val;
                        var testId = LevelData.GetId(xMatrix, yMatrix);
                        if (inputMatrix.TryGetValue(testId, out val))
                        {
                            debugStr += $"Kernel factors: \t{val}\t{kernel[i, j]}\n";
                            sum += val * kernel[i, j];
                        }
                    }
                }
            }
            debugStr += "-------------------\n";
            outputMatrix[cell.Key] = sum;
        }
        //GD.Print(debugStr);
        return outputMatrix;
    }

    #endregion

    #region Debug methods

    private void DebugPrintCellScores(int unitId, Dictionary<int, int> cellScores)
    {
        // Debug print cell scores
        int cellCount = LevelData.NUM_OF_ROWS * LevelData.NUM_OF_COLS;
        string debugStr = string.Empty;
        string lineStr = "|";
        for (int i = 0; i < cellCount; i++)
        {
            if (i != 0 && i % 8 == 0)
            {
                debugStr = lineStr + "\n" + debugStr;
                lineStr = "|";
            }

            if (cellScores.ContainsKey(i))
            {
                int score = cellScores[i];
                string color = score > 0 ? "green" : score < 0 ? "red" : "yellow";
                lineStr += $"[color={color}]{Mathf.Abs(score)}[/color]";
            }
            else
            {
                lineStr += i == unitId ? "[color=blue]X[/color]" : "0";
            }
            lineStr += "|";
        }
        GD.PrintRich(debugStr);
    }

    #endregion

}
