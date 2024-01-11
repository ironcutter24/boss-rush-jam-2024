using Godot;
using System;
using System.Threading;
using System.Threading.Tasks;

public partial class TurnManager : Node
{
    public delegate Task<TurnState> TurnState(CancellationToken ct);
    public TurnState CurrentTurn { get; private set; }

    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

    public override void _Ready()
    {
        CurrentTurn = null;
        _ = GameLoop(cancellationTokenSource.Token);
    }

    private async Task GameLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            CurrentTurn = await CurrentTurn(ct);
            await GDTask.NextFrame();
        }
    }

    #region Turn states AI

    #endregion

    #region Turn states Player

    private async Task<TurnState> PlayerUnitSelection(CancellationToken ct)
    {
        bool result = await PlayerInput(
            () => Input.IsActionJustPressed("mouse_left"),
            () => false,
            ct
            );

        return result ? PlayerMoveSelection : CurrentTurn;
    }

    private async Task<TurnState> PlayerMoveSelection(CancellationToken ct)
    {
        bool result = await PlayerInput(
            () => Input.IsActionJustPressed("mouse_left"),
            () => Input.IsActionJustPressed("mouse_right"),
            ct
            );

        return result ? PlayerAttackSelection : PlayerUnitSelection;
    }

    private async Task<TurnState> PlayerAttackSelection(CancellationToken ct)
    {
        bool result = await PlayerInput(
            () => Input.IsActionJustPressed("mouse_left"),
            () => Input.IsActionJustPressed("mouse_right"),
            ct
            );

        return result ? PlayerUnitSelection : PlayerUnitSelection;
    }

    private async Task<bool> PlayerInput(Func<bool> accept, Func<bool> cancel, CancellationToken ct)
    {
        while (true)
        {
            await GDTask.NextFrame();
            if (accept()) return true;
            if (cancel()) return false;
        }
    }

    #endregion

    public override void _Notification(int what)
    {
        if (what == MainLoop.NotificationPredelete)
        {
            CancelAsyncOperations();
        }
    }

    private void CancelAsyncOperations()
    {
        if (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            cancellationTokenSource.Cancel();
            GD.Print("Cancellation token canceled.");
        }
    }
}
