using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Utility-based decision maker.
/// Provides AI decisions for units.
/// </summary>
public static partial class DecisionMaker
{
    public static Vector2I GetNextCell(Unit unit, LevelData levelData)
    {
        var cellScores = new Dictionary<int, int> { { unit.GridId, 0 } };
        var playerUnits = Unit.GetUnits(FactionType.Player).ToArray();
        var reachableIds = levelData.GetReachableIds(unit);
        reachableIds.ForEach(id => cellScores.Add(id, 0));

        // Update scores
        ScoreEvaluator.PlayerAttackAreas(ref cellScores, levelData, playerUnits);
        ScoreEvaluator.PathToPlayerUnits(ref cellScores, levelData, playerUnits, unit);
        ScoreEvaluator.NearbyPlayerUnits(ref cellScores);

        // Matrix convolution with before/after debug print
        DebugPrintCellScores(unit.GridId, cellScores, "Unit utility scores");
        cellScores = ApplyKernel(cellScores, kernel, LevelData.NUM_OF_ROWS, LevelData.NUM_OF_COLS);
        DebugPrintCellScores(unit.GridId, cellScores, "Scores convolution");

        // Get ID of cell with highest score
        cellScores = cellScores.OrderBy(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
        var targetCell = cellScores.Last();

        // Use current cell if has same score of chosen cell
        var currentCell = GetKeyValuePairAt(cellScores, unit.GridId);
        if (currentCell.Value == targetCell.Value)
        {
            targetCell = currentCell;
        }

        // Return grid position for highest score
        var (i, j) = LevelData.GetIndexes(targetCell.Key);
        return new Vector2I(i, j);
    }

    #region Matrix operations

    //private static readonly int[,] kernel =
    //{
    //    { 0, 0, 1, 0, 0},
    //    { 0, 1, 2, 1, 0},
    //    { 1, 2, 5, 2, 1},
    //    { 0, 1, 2, 1, 0},
    //    { 0, 0, 1, 0, 0}
    //};

    private static readonly int[,] kernel =
    {
        { 0, 0, 0, 0, 0},
        { 0, 0, 1, 0, 0},
        { 0, 1, 3, 1, 0},
        { 0, 0, 1, 0, 0},
        { 0, 0, 0, 0, 0}
    };

    private static Dictionary<int, int> ApplyKernel(Dictionary<int, int> inputMatrix, int[,] kernel, int matrixHeight, int matrixWidth)
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

    #region Helpers

    private static KeyValuePair<int, int> GetKeyValuePairAt(Dictionary<int, int> data, int id)
    {
        return new KeyValuePair<int, int>(id, data[id]);
    }

    #endregion

    #region Debug methods

    private static void DebugPrintCellScores(int unitId, Dictionary<int, int> cellScores, string description)
    {
        GD.PrintRich($"[b]{description}:[/b]");

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
                string color = (i == unitId ? "blue" : (score > 0 ? "green" : (score < 0 ? "red" : "yellow")));
                lineStr += $"[color={color}]{Mathf.Abs(score)}[/color]";
            }
            else
            {
                lineStr += "0";
            }
            lineStr += "|";
        }
        GD.PrintRich(debugStr);
    }

    #endregion


    private static class ScoreEvaluator
    {
        public static void NearbyPlayerUnits(ref Dictionary<int, int> scores)
        {
            // Calculate nearby players for each valid movement cell
            // 0->0 1->2, 2->0, 3->-2 etc...
            foreach (var id in scores.Keys)
            {
                int nearby = LevelData.CountNearbyUnits(id);
                if (nearby > 0)
                {
                    scores[id] += (2 - nearby) * 6;
                }
            }
        }

        public static void PlayerAttackAreas(ref Dictionary<int, int> scores, LevelData levelData, Unit[] playerUnits)
        {
            foreach (var playerUnit in playerUnits)
            {
                // Reduce score of cells where can get attacked
                var cells = levelData.GetHittableCells(playerUnit);
                foreach (var cell in cells)
                {
                    if (scores.ContainsKey(cell.id))
                        scores[cell.id] -= cell.dist - 1;  // Distance 1 has no impact on weight
                }
            }
        }

        public static void PathToPlayerUnits(ref Dictionary<int, int> scores, LevelData levelData, Unit[] playerUnits, Unit unit)
        {
            foreach (var playerUnit in playerUnits)
            {
                // Increase score of cells that get you closer to a target
                var path = levelData.GetPath(unit, playerUnit.GridPosition);
                if (path != null && path.Length > 0)
                {
                    GD.Print("Path to player len: " + path.Length);
                    for (int i = 1; i < path.Length; i++)  // Ignore first element == unit position
                    {
                        int id = levelData.GetId(path[i]);
                        if (scores.ContainsKey(id))
                            scores[id] += i;
                    }
                }
            }
        }
    }

}
