using System.Diagnostics;

namespace dotNetExpress.Tools;
public class WsServer
{
    private readonly Dictionary<string, WebSocket> _webSockets = [];

    private CancellationTokenSource _cancellationTokenSource = new();

    private Task _idleTask;

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
            lock (_webSockets)
            {
                var toRemove = new List<WebSocket>();

                var frameMessage = WsFrameFactory.FromString(IdleMessage);

                foreach (var kvp in _webSockets)
                {
                    var wsSocket = kvp.Value;
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
                            toRemove.Add(wsSocket);
                        }
                    }
                }

                foreach (var client in toRemove)
                    Remove(client);

                if (toRemove.Count > 0)
                    Debug.WriteLine($"Sockets remaining {_webSockets.Count}.");
            }

            Thread.Sleep(1000);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="req"></param>
    /// <param name="res"></param>
    public void Add(Request req, Response res)
    {
        var ws = new WebSocket(this, res.Socket);

        lock (_webSockets)
        {
            _webSockets.Add(req.Path, ws);
        }
    }

    /// <summary>
    /// Socket in argument has already been closed
    /// </summary>
    /// <param name="client"></param>
    public void Remove(WebSocket value)
    {
        lock (_webSockets)
        {
            foreach (var item in _webSockets.Where(kvp => kvp.Value == value).ToList())
                _webSockets.Remove(item.Key);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    public async Task SendText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        var frameMessage = WsFrameFactory.FromString(text);

        lock (_webSockets)
        {
            // reverse iterate, so that we can remove
            // items from array in case of error
            // without screwing up the array

            var toRemove = new List<WebSocket>();

            foreach (var kvp in _webSockets)
            {
                var client = kvp.Value;

                try
                {
                    client.GetSocket().Send(frameMessage);

                    client.LastAction = DateTime.Now;

                }
                catch (Exception)
                {
                    toRemove.Add(client);
                }
            }

            foreach (var client in toRemove)
                Remove(client);
        }
    }
}