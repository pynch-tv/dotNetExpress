using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace dotNetExpress.Tools;

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
    private static readonly Dictionary<int, SseSocket> _sseSockets = [];

    private CancellationTokenSource _cancellationTokenSource = new();

    private Task _idleTask;

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
    public bool Start()
    {
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
            _idleTask.Wait();
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
    /// <param name="token"></param>
    private void IdleWorker(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            lock (_sseSockets)
            {
                var toRemove = new List<SseSocket>();

                var idleMessage = ":\n\n";

                var frameMessage = Encoding.UTF8.GetBytes(idleMessage);

                foreach (var kvp in _sseSockets)
                {
                    var sseSocket = kvp.Value;

                    if (!sseSocket.GetSocket().Connected)
                        toRemove.Add(sseSocket);
                    else
                    {
                        if (DateTime.Now.Subtract(sseSocket.LastAction).TotalSeconds > IdleTimeout)
                        {
                            try
                            {
                                sseSocket.GetSocket().SendAsync(frameMessage);

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

            Thread.Sleep(1000);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="socket"></param>
    public void Add(int id, Socket socket)
    {
        var sse = new SseSocket(this, socket);

        lock (_sseSockets)
        {
            _sseSockets.Add(id, sse);

            Debug.WriteLine($"SseServer.Add: {id}");
        }

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    public void Remove(SseSocket value)
    {
        lock (_sseSockets)
        {
            foreach (var item in _sseSockets.Where(kvp => kvp.Value == value).ToList())
            {
                Debug.WriteLine($"SSE Server.Remove: {item.Key}");
                _sseSockets.Remove(item.Key);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    public async Task SendText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        lock (_sseSockets)
        {
            var toRemove = new List<SseSocket>();

            var message = $"data:{text}\n\n";

            foreach (var kvp in _sseSockets)
            {
                var client = kvp.Value;

                try
                {
                    var s = client.GetSocket();
                    s.SendAsync(Encoding.UTF8.GetBytes(message));

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
    }
}
