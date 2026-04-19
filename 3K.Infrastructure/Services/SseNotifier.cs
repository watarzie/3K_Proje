using _3K.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;

namespace _3K.Infrastructure.Services
{
    public class SseNotifier : ISseNotifier
    {
        // Thread-safe dictionary to maintain connection streams
        private readonly ConcurrentDictionary<Guid, HttpResponse> _clients = new();

        public async Task SubscribeAsync(object contextObj)
        {
            if (contextObj is not HttpContext context)
                return;

            context.Response.Headers.Append("Content-Type", "text/event-stream");
            context.Response.Headers.Append("Cache-Control", "no-cache");
            context.Response.Headers.Append("Connection", "keep-alive");

            var clientId = Guid.NewGuid();
            _clients.TryAdd(clientId, context.Response);

            try
            {
                // Send an initial connection event
                var initMessage = "event: connected\ndata: connected\n\n";
                await context.Response.WriteAsync(initMessage);
                await context.Response.Body.FlushAsync();

                // Keep connection alive indefinitely until client disconnects
                while (!context.RequestAborted.IsCancellationRequested)
                {
                    // Ping interval to keep proxy connections alive
                    await Task.Delay(15000, context.RequestAborted);
                    await context.Response.WriteAsync(":\n\n");
                    await context.Response.Body.FlushAsync();
                }
            }
            catch (TaskCanceledException)
            {
                // Normal client disconnect
            }
            finally
            {
                _clients.TryRemove(clientId, out _);
            }
        }

        public async Task BroadcastApprovalUpdateAsync()
        {
            var message = "event: approval_update\ndata: refresh\n\n";
            var tasks = _clients.Values.Select(async client =>
            {
                try
                {
                    await client.WriteAsync(message);
                    await client.Body.FlushAsync();
                }
                catch
                {
                    // Ignore disconnected clients (will be cleaned up by the RequestAborted loop)
                }
            });

            await Task.WhenAll(tasks);
        }
    }
}
