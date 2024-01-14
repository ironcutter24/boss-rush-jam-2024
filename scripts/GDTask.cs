using System;
using System.Threading.Tasks;

public static class GDTask
{
    public static async Task WaitUntil(Func<bool> condition)
    {
        while (condition()) await NextFrame();
    }

    public static async Task NextFrame()
    {
        await Global.Instance.ToSignal(Global.Instance.Tree, "process_frame");
    }

    public static async Task DelaySeconds(float s)
    {
        await Task.Delay(TimeSpan.FromSeconds(s));
    }
}
