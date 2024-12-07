using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace dotNetExpress;

/// <summary>
/// 
/// </summary>
public class Server : TcpListener
{
    private Thread? _tcpListenerThread;

    private CancellationTokenSource _cancellation = new();

    private object _lock = new();

    private int connectionCount = 0;

    #region Constructor

    /// <summary>
    /// 
    /// </summary>
    /// <param name="port"></param>
    [Obsolete("Obsolete")]
    public Server(int port) : base(port)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="localAddress"></param>
    /// <param name="port"></param>
    public Server(IPAddress localAddress, int port) : base(localAddress, port)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="localEP"></param>
    public Server(IPEndPoint localEP) : base(localEP)
    {
    }

    #endregion

    #region Events

    /// <summary>
    /// 
    /// </summary>
    public event EventHandler<TcpClient> HandleConnection;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tcpClient"></param>
    private void RaiseHandleConnection(TcpClient tcpClient)
    {
        lock (_lock)
        {
            connectionCount++;
        }

        Debug.WriteLine($"[T{Environment.CurrentManagedThreadId}] (Total open: {connectionCount}) RaiseHandleConnection from {tcpClient.Client.RemoteEndPoint}");

        var handler = HandleConnection;
        handler?.Invoke(this, tcpClient);

        lock (_lock)
        {
            connectionCount--;
        }

        Debug.WriteLine($"[T{Environment.CurrentManagedThreadId}] (Still open: {connectionCount}) Connection done {tcpClient.Client?.RemoteEndPoint} {tcpClient.Client?.IsBound}");
    }

    #endregion

    /// <summary>
    /// 
    /// </summary>
    /// <param name="express"></param>
    public async Task Begin(Express express)
    {
        Start();

        _tcpListenerThread = new Thread(() =>
        {
            Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] Listener started");

            List<Task> tcpClientTasks = [];
            int awaiterTimeoutInMS = 500;

            var keepGoing = true;

            _cancellation.Token.Register(() =>
            {
                keepGoing = false;
            });

            try
            {
                // maintain a max amount of awaiting connections. A closed connection
                // is immediately replace by an awaiting connection.
                while (keepGoing)
                {
                    while (keepGoing && tcpClientTasks.Count < express.MaxConcurrentListeners)
                    {
                        var awaiterTask = Task.Run(async () =>
                        {
                            try
                            {
                                Debug.WriteLine($"[T{Task.CurrentId}] Awaiting new connection");

                                RaiseHandleConnection(await AcceptTcpClientAsync(_cancellation.Token));
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine($"[T{Task.CurrentId}] Error in Client.Connection: {e.Message}");
                                keepGoing = false;
                            }
                        });
                        Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] added task [T{awaiterTask.Id}]");
                        tcpClientTasks.Add(awaiterTask);
                    }

                    var removeAtIndex = Task.WaitAny([.. tcpClientTasks], awaiterTimeoutInMS);
                    if (removeAtIndex > 0)
                    {
                        Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] removing connection at index {removeAtIndex} with Task Id: [T{tcpClientTasks[removeAtIndex].Id}]");
                        tcpClientTasks.RemoveAt(removeAtIndex);
                    }
                }
            }
            catch (Exception e) 
            {
                Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] Inner loop exception: {e.Message}");
            }

            Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] Listener stopped");
        });

        _tcpListenerThread.Start();
        Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] Listener starting");
    }

    /// <summary>
    /// 
    /// </summary>
    public void End()
    {
        _cancellation.Cancel();

        Debug.WriteLine("Listener stopping");

        Stop();
    }

}

