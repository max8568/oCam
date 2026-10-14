namespace FrameCam.CaptureTests;

/// <summary>
/// Runs async test scenarios on one shared STA thread with a WinForms message loop, like the app's single
/// UI thread. (Windows created on a second UI thread in the test host did not appear in screen captures,
/// so every scenario uses the same thread.) The process is made Per-Monitor V2 DPI aware first, like the
/// app, so every coordinate is in physical pixels.
/// </summary>
internal static class Sta
{
    private static readonly Lazy<WindowsFormsSynchronizationContext> Context = new(StartUiThread);

    public static void Run(Func<Task> scenario) => Run(async () => { await scenario(); return true; });

    public static T Run<T>(Func<Task<T>> scenario)
    {
        var done = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Context.Value.Post(async _ =>
        {
            try { done.SetResult(await scenario()); }
            catch (Exception e) { done.SetException(e); }
        }, null);
        return done.Task.GetAwaiter().GetResult();
    }

    private static WindowsFormsSynchronizationContext StartUiThread()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        var ready = new TaskCompletionSource<WindowsFormsSynchronizationContext>();
        var thread = new Thread(() =>
        {
            var context = new WindowsFormsSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(context);
            ready.SetResult(context);
            Application.Run(); // until the test process exits
        })
        {
            IsBackground = true,
            Name = "FrameCam test UI",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return ready.Task.Result;
    }
}
