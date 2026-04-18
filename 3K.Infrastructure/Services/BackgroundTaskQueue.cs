using System.Threading.Channels;
using _3K.Core.Interfaces;

namespace _3K.Infrastructure.Services
{
    /// <summary>
    /// System.Threading.Channels tabanlı in-memory iş kuyruğu.
    /// Bounded capacity ile bellek taşmasına karşı koruma sağlar.
    /// </summary>
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly Channel<Func<IServiceProvider, CancellationToken, Task>> _queue;

        public BackgroundTaskQueue(int capacity = 100)
        {
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait // Kuyruk doluysa bekle
            };
            _queue = Channel.CreateBounded<Func<IServiceProvider, CancellationToken, Task>>(options);
        }

        public async ValueTask QueueAsync(Func<IServiceProvider, CancellationToken, Task> workItem)
        {
            ArgumentNullException.ThrowIfNull(workItem);
            await _queue.Writer.WriteAsync(workItem);
        }

        public async ValueTask<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}
