using Godot;
using Godot.Collections;
using System;

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
            .SubstateOf(State.EnemyTurn);

        sm.Configure(State.SelectSwap)
            .SubstateOf(State.EnemyTurn);

        sm.Configure(State.AwaitSwap)
            .SubstateOf(State.EnemyTurn);
    }

    private void RunUtilityDecisionMaker(Unit unit)
    {
        Dictionary<int, int> cellUtilities = new Dictionary<int, int>();

        var reachableIds = levelData.GetReachableIds(unit);
        reachableIds.ForEach(id => cellUtilities.Add(id, 0));



    }

}
