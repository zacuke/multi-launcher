namespace multi_launcher;
public class DisableCtrlCLifeTime() : IHostLifetime, IDisposable
{

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task WaitForStartAsync(CancellationToken cancellationToken)
    {
        Console.CancelKeyPress += OnCancelKeyPressed;
        return Task.CompletedTask;
    }

    private void OnCancelKeyPressed(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
    }

    public void Dispose()
    {
        Console.CancelKeyPress -= OnCancelKeyPressed;
    }
}