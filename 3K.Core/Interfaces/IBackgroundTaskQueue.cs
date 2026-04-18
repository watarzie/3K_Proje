namespace _3K.Core.Interfaces
{
    /// <summary>
    /// Arka plan iş kuyruğu arayüzü.
    /// Command Handler'lar bu interface üzerinden uzun süren işleri
    /// (PDF, Excel, Mail vb.) HTTP döngüsünden bağımsız kuyruğa ekler.
    /// </summary>
    public interface IBackgroundTaskQueue
    {
        /// <summary>İş kuyruğuna yeni bir görev ekler.</summary>
        ValueTask QueueAsync(Func<IServiceProvider, CancellationToken, Task> workItem);

        /// <summary>Kuyruktan sıradaki görevi alır (BackgroundService tarafından çağrılır).</summary>
        ValueTask<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
    }
}
