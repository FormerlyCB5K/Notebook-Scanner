namespace NotebookScanner;

// Attempts are strictly serial. The same token cancels connection, receive and backoff.
public static class ConnectionRetry
{
    public static async Task Run(Func<CancellationToken, Task> attempt,
        Action<Exception, TimeSpan> failed, CancellationToken stop,
        TimeSpan? initialDelay = null)
    {
        var delay = initialDelay ?? TimeSpan.FromSeconds(2);
        while (true) {
            stop.ThrowIfCancellationRequested();
            try { await attempt(stop); }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { throw; }
            catch (UnauthorizedAccessException) { throw; } // Do not repeat denied consent prompts.
            catch (Exception ex) {
                stop.ThrowIfCancellationRequested();
                failed(ex, delay);
                await Task.Delay(delay, stop);
                delay = TimeSpan.FromSeconds(Math.Min(8, delay.TotalSeconds * 2));
                continue;
            }
            // A connection is expected to last until stopped or disconnected.
            stop.ThrowIfCancellationRequested();
            throw new IOException("The connection ended unexpectedly.");
        }
    }
}
