using System.Diagnostics;
using System.Net;

namespace dotNetExpress.Tools;

public class WsValue
{
    public required string Topic;
    public required WebSocket Socket;
}

public class WsServer
{
    private readonly Dictionary<EndPoint, WsValue> _webSockets = [];

    private CancellationTokenSource _cancellationTokenSource = new();

    private Task? _idleTask;

    private static readonly SemaphoreSlim semaphoreSlim = new(1, 1);

    /// <summary>
    /// Idle timeout value in seconds
    /// </summary>
    public int IdleTimeout { get; set; } = 2;

    /// <summary>
    /// Idle JSON message to send to clients.
    /// </summary>
    public string IdleMessage { get; set; } = "{}";

    /// <summary>
    /// 
    /// </summary>
    public WsServer()
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
    /// <param name="token"></param>
    private async void IdleWorker(CancellationToken token)
    {
        while (true)
        {
            await semaphoreSlim.WaitAsync(token);
            try
            {
                var toRemove = new List<WsValue>();

                var frameMessage = WsFrameFactory.FromString(IdleMessage);

                foreach (var kvp in _webSockets)
                {
                    var wsSocket = kvp.Value.Socket;
                    if (DateTime.Now.Subtract(wsSocket.LastAction).TotalSeconds > IdleTimeout)
                    {
                        try
                        {
                            wsSocket.GetSocket().Send(frameMessage);

                            // TODO: send idle message
                            wsSocket.LastAction = DateTime.Now;
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine($"Socket exception {e.Message}");
                            toRemove.Add(kvp.Value);
                        }
                    }
                }

                foreach (var client in toRemove)
                    Remove(client.Socket);

                if (toRemove.Count > 0)
                    Debug.WriteLine($"Sockets remaining {_webSockets.Count}.");
            }
            finally
            {
                semaphoreSlim.Release();
            }

            var eventThatSignaledIndex = WaitHandle.WaitAny([token.WaitHandle], 1000);
//            Debug.WriteLine($"ws IdleWorker: WaitHandle.WaitAny: {eventThatSignaledIndex}");
            if (eventThatSignaledIndex == 0)
            {
                Debug.WriteLine("IdleWorker: CancellationToken signaled");
                break;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="req"></param>
    /// <param name="res"></param>
    public async void Add(Request req, Response res, string topic)
    {
        var wsValue = new WsValue
        {
            Topic = topic,
            Socket = new WebSocket(this, res.Socket)
        };

        await semaphoreSlim.WaitAsync();
        try {
            _webSockets.Add(req.Socket.RemoteEndPoint, wsValue);
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }

    /// <summary>
    /// Socket in argument has already been closed
    /// </summary>
    /// <param name="client"></param>
    public async void Remove(WebSocket value)
    {
        await semaphoreSlim.WaitAsync();
        try {
            foreach (var item in _webSockets.Where(kvp => kvp.Value.Socket == value).ToList())
            {
                item.Value.Socket.GetSocket().Close();
                _webSockets.Remove(item.Key);
            }
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }

    public delegate string FilterByTopic(string key);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    public async Task SendText(FilterByTopic filter)
    {
        // lock (_webSockets)
        await semaphoreSlim.WaitAsync();
        try
        {
            // reverse iterate, so that we can remove
            // items from array in case of error
            // without screwing up the array

            var toRemove = new List<WsValue>();

            foreach (var kvp in _webSockets)
            {
                var client = kvp.Value;

                try
                {
                    var text = filter(kvp.Value.Topic);
                    if (string.IsNullOrEmpty(text))
                        continue;

                    var frameMessage = WsFrameFactory.FromString(text);

                    //      client.GetSocket().Send(frameMessage);
                    await client.Socket.GetSocket().SendAsync(frameMessage);

                    client.Socket.LastAction = DateTime.Now;

                }
                catch (Exception)
                {
                    toRemove.Add(client);
                }
            }

            foreach (var client in toRemove)
                Remove(client.Socket);
        }
        finally
        {
            semaphoreSlim.Release();
        }       
    }
}