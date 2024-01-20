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
                ResetCurrentUnit();
                ResetTurn(FactionType.Enemy);
                GD.Print(">>> Entered Enemy turn");
            })
            .OnExit(() =>
            {
                GD.Print("<<< Exited Enemy turn");
            });

        sm.Configure(State.EnemyAI)
            .SubstateOf(State.EnemyTurn)
            .OnEntry(() =>
            {
                // Debug run
                RunUtilityDecisionMaker(enemyUnits[0]);
            });

        sm.Configure(State.SelectSwap)
            .SubstateOf(State.EnemyTurn);

        sm.Configure(State.AwaitSwap)
            .SubstateOf(State.EnemyTurn);
    }

    private void RunUtilityDecisionMaker(Unit unit)
    {
        Dictionary<int, int> cellScores = new Dictionary<int, int>();

        var reachableIds = levelData.GetReachableIds(unit);
        reachableIds.ForEach(id => cellScores.Add(id, 0));

        GD.Print("Reachables: " + reachableIds.Count);

        // Calculate nearby players for all valid movement cells
        // 0->0 1->2, 2->1, 3->0 etc...
        foreach (var id in reachableIds)
        {
            int nearby = CountNearbyUnits(id);
            if (nearby > 0)
            {
                cellScores[id] += nearby;
            }
        }

        // Calculate scores influenced by player units
        var playerUnits = GetPlayerUnits();
        foreach (var playerUnit in playerUnits)
        {
            UpdateScores(unit, playerUnit, ref cellScores);
        }

        // Sort by score
        cellScores.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value);


        // Move to last pair or pick random from highest scores


        DebugPrintCellScores(cellScores);
    }

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
        var (i, j) = LevelData.GetIndexes(cellId);
        for (--i; i < i + 3; i++)
        {
            for (--j; j < j + 3; j++)
            {
                var unit = LevelData.GetUnitAtPosition(new Vector2I(i, j));
                if (unit != null)
                    count++;
            }
        }
        return count;
    }

    private void DebugPrintCellScores(Dictionary<int, int> cellScores)
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
                lineStr += cellScores[i];
            }
            else
            {
                lineStr += "0";
            }
            lineStr += "|";
        }
        GD.Print("Unit utility scores:");
        GD.Print(debugStr);
    }
}
