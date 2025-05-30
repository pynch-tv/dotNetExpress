using System.Diagnostics;
using System.Net;
using System.Text;

namespace dotNetExpress.Tools;

public class SseValue
{
    public required string Topic;
    public required SseSocket Socket;
}

/// <summary>
/// Warning: When not used over HTTP/2, SSE suffers from a limitation to the maximum number of open connections, 
/// which can be especially painful when opening multiple tabs, as the limit is per browser and is set to a very
/// low number (6). The issue has been marked as "Won't fix" in Chrome and Firefox. This limit is per browser + domain, 
/// which means that you can open 6 SSE connections across all of the tabs to www.example1.com and another 6 SSE connections 
/// to www.example2.com (per StackOverflow). When using HTTP/2, the maximum number of simultaneous HTTP streams is negotiated 
/// between the server and the client (defaults to 100).
/// 
/// 
/// SSE works over a standard HTTP connection, using a specific content type and format to send updates. The server 
/// responds with a text/event-stream content type, and data is sent in the following format:
/// 
/// Data: Each message can contain multiple data fields, with each field separated by a newline(\n). 
/// The event must end with two newlines(\n\n).
/// 
/// Event: Optional custom event types can be defined.
/// 
/// ID: An optional message ID that allows the client to track and handle reconnection cases.

/// 
/// 
/// </summary>

public class SseServer
{
    private readonly Dictionary<EndPoint, SseValue> _sseSockets = [];

    private CancellationTokenSource _cancellationTokenSource = new();

    private Task? _idleTask;

    private static readonly SemaphoreSlim semaphoreSlim = new(1, 1);

    /// <summary>
    /// Idle timeout value in seconds
    /// </summary>
    public int IdleTimeout { get; set; } = 2;

    /// <summary>
    /// Idle message to send to clients. Leading : must NOT be included
    /// </summary>

    /// <summary>
    /// 
    /// </summary>
    public SseServer()
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task<bool> Start()
    {
        Debug.Assert(_idleTask == null, "_idleTask must be null here");

        _idleTask = Task.Run(() => IdleWorker(_cancellationTokenSource.Token));

        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    public void Stop()
    {
        _cancellationTokenSource.Cancel();

        try
        {
            _idleTask?.Wait();
            _idleTask = null;
        }
        catch (AggregateException e)
        {
            foreach (var inner in e.InnerExceptions)
            {
                Debug.WriteLine($"Exception: {inner.Message}");
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="socket"></param>
    public async void Add(Request req, Response res, string topic)
    {
        var sseValue = new SseValue
        {
            Topic = topic,
            Socket = new SseSocket(this, req.Socket)
        };

        await semaphoreSlim.WaitAsync();
        try {
            _sseSockets.Add(req.Socket.RemoteEndPoint, sseValue);

            Debug.WriteLine($"SseServer.Add: {topic}");
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    public async void Remove(SseSocket value)
    {
        await semaphoreSlim.WaitAsync();
        try {
            foreach (var item in _sseSockets.Where(kvp => kvp.Value.Socket == value).ToList())
            {
                Debug.WriteLine($"SSE Server Remove: {item.Key}");
                _sseSockets.Remove(item.Key);
            }
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="token"></param>
    private async void IdleWorker(CancellationToken token)
    {
        while (true)
        {
            await semaphoreSlim.WaitAsync(token);

            try
            {
                var toRemove = new List<SseSocket>();

                var idleMessage = ":\n\n";

                var frameMessage = Encoding.UTF8.GetBytes(idleMessage);

                foreach (var kvp in _sseSockets)
                {
                    var sseSocket = kvp.Value.Socket;

                    if (!sseSocket.GetSocket().Connected)
                        toRemove.Add(sseSocket);
                    else
                    {
                        if (DateTime.Now.Subtract(sseSocket.LastAction).TotalSeconds > IdleTimeout)
                        {
                            try
                            {
                                await sseSocket.GetSocket().SendAsync(frameMessage);

                                sseSocket.LastAction = DateTime.Now;
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine($"SSE Socket exception {e.Message}");
                                toRemove.Add(sseSocket);
                            }
                        }
                    }
                }

                foreach (var client in toRemove)
                    Remove(client);
            }
            finally
            {
                semaphoreSlim.Release();
            }

            var eventThatSignaledIndex = WaitHandle.WaitAny([token.WaitHandle], 1000);
//            Debug.WriteLine($"sse IdleWorker: WaitHandle.WaitAny: {eventThatSignaledIndex}");
            if (eventThatSignaledIndex == 0)
            {
                Debug.WriteLine("IdleWorker: CancellationToken signaled");
                break;
            }
        }
    }

    public delegate string FilterByTopic(string key);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    public async Task SendText(FilterByTopic filter)
    {
        await semaphoreSlim.WaitAsync();
        try
        {
            var toRemove = new List<SseSocket>();

            foreach (var kvp in _sseSockets)
            {
                var client = kvp.Value.Socket;

                try
                {
                    var text = filter(kvp.Value.Topic);
                    if (string.IsNullOrEmpty(text))
                        continue;

                    var message = $"data:{text}\n\n";

                    var s = client.GetSocket();
                    await s.SendAsync(Encoding.UTF8.GetBytes(message));

                    client.LastAction = DateTime.Now;
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Socket exception {e.Message}");
                    toRemove.Add(client);
                }
            }

            foreach (var client in toRemove)
                Remove(client);
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }
}
