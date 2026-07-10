using System.Collections.Concurrent;
using System.Threading.Channels;
using _3K.Core.Constants;
using _3K.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace _3K.Infrastructure.Services
{
    public class SseNotifier : ISseNotifier
    {
        private const int HeartbeatIntervalSeconds = 25;
        private readonly ConcurrentDictionary<Guid, SseClient> _clients = new();

        public async Task SubscribeAsync(object contextObj, int kullaniciId)
        {
            if (contextObj is not HttpContext context || kullaniciId <= 0)
                return;

            context.Response.Headers.Append("Content-Type", "text/event-stream");
            context.Response.Headers.Append("Cache-Control", "no-cache, no-store");
            context.Response.Headers.Append("Connection", "keep-alive");
            context.Response.Headers.Append("X-Accel-Buffering", "no");

            var clientId = Guid.NewGuid();
            var client = new SseClient(kullaniciId);
            _clients.TryAdd(clientId, client);
            client.Events.Writer.TryWrite(new SseEvent("connected", "connected"));

            using var connectionLifetime = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            var heartbeatTask = ProduceHeartbeatsAsync(client, connectionLifetime.Token);

            try
            {
                await foreach (var sseEvent in client.Events.Reader.ReadAllAsync(connectionLifetime.Token))
                {
                    await context.Response.WriteAsync(
                        $"event: {sseEvent.EventName}\ndata: {sseEvent.Data}\n\n",
                        context.RequestAborted);
                    await context.Response.Body.FlushAsync(context.RequestAborted);
                }
            }
            catch (OperationCanceledException)
            {
                // İstemcinin bağlantıyı kapatması normal akıştır.
            }
            catch (IOException)
            {
                // Ağ bağlantısı beklenmedik şekilde kapandı.
            }
            finally
            {
                _clients.TryRemove(clientId, out _);
                client.Events.Writer.TryComplete();
                connectionLifetime.Cancel();

                try
                {
                    await heartbeatTask;
                }
                catch (OperationCanceledException)
                {
                    // RequestAborted sonrasında heartbeat görevi de kapanır.
                }
            }
        }

        public Task NotifyUsersAsync(IEnumerable<int> kullaniciIdleri, string eventName, string data = "refresh")
        {
            var hedefKullanicilar = kullaniciIdleri.Where(id => id > 0).ToHashSet();
            if (hedefKullanicilar.Count == 0)
                return Task.CompletedTask;

            var sseEvent = new SseEvent(eventName, data);
            foreach (var client in _clients.Values.Where(client => hedefKullanicilar.Contains(client.KullaniciId)))
                client.Events.Writer.TryWrite(sseEvent);

            return Task.CompletedTask;
        }

        public Task BroadcastApprovalUpdateAsync()
        {
            var sseEvent = new SseEvent(SseOlaylari.OnayGuncellendi, "refresh");
            foreach (var client in _clients.Values)
                client.Events.Writer.TryWrite(sseEvent);

            return Task.CompletedTask;
        }

        private static async Task ProduceHeartbeatsAsync(SseClient client, CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(HeartbeatIntervalSeconds));
            while (await timer.WaitForNextTickAsync(cancellationToken))
                client.Events.Writer.TryWrite(new SseEvent("heartbeat", "ping"));
        }

        private sealed class SseClient
        {
            public SseClient(int kullaniciId)
            {
                KullaniciId = kullaniciId;
                Events = Channel.CreateBounded<SseEvent>(new BoundedChannelOptions(50)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.DropOldest
                });
            }

            public int KullaniciId { get; }
            public Channel<SseEvent> Events { get; }
        }

        private sealed record SseEvent(string EventName, string Data);
    }
}
