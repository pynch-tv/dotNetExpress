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
    /// <param name="connection"></param>
    private void RaiseHandleConnection(TcpClient connection)
    {
        var handler = HandleConnection;
        handler?.Invoke(this, connection);
    }

    #endregion

    /// <summary>
    /// https://www.codeproject.com/Articles/5270779/High-Performance-TCP-Client-Server-using-TCPListen
    /// </summary>
    /// <param name="express"></param>
    public async Task Begin(Express express)
    {
        Start();

        _tcpListenerThread = new Thread(() =>
        {
            Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] ({DateTime.Now:HH.mm.ss:ffff}) Listener started");

            List<Task> tcpClientTasks = [];
            int awaiterTimeoutInMS = 500;

            _cancellation.Token.Register(() => this.Stop());

            try
            {
                // maintain a max amount of awaiting connections. A closed connection
                // is immediately replace by an awaiting connection.
                while (true)
                {
                    if (_cancellation.Token.IsCancellationRequested)
                        break;

                    while (tcpClientTasks.Count < express.MaxConcurrentListeners)
                    {
                        var awaiterTask = Task.Run(async () =>
                        {
                            try
                            {
                                Debug.WriteLine($"[T{Task.CurrentId}] ({DateTime.Now:HH.mm.ss:ffff}) Awaiting new connection");

                                RaiseHandleConnection(await AcceptTcpClientAsync(_cancellation.Token));
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine($"[T{Task.CurrentId}] ({DateTime.Now:HH.mm.ss:ffff}) Error in Client.Connection: {e.Message}");
                            }
                        });
                        tcpClientTasks.Add(awaiterTask);
                    }

                    // Waits for any of the provided Task objects to complete execution.
                    // Note: only the first complete Task is returned. If more Task ran to completion, it will be picked up
                    //       time in this routine
                    var indexOfFirstCompletedTask = Task.WaitAny([.. tcpClientTasks], awaiterTimeoutInMS);
                    if (indexOfFirstCompletedTask >= 0)
                        tcpClientTasks.RemoveAt(indexOfFirstCompletedTask);
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception e) 
            {
                Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] ({DateTime.Now:HH.mm.ss:ffff}) Inner loop exception: {e.Message}");
                _cancellation.Token.ThrowIfCancellationRequested();
            }

            Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] ({DateTime.Now:HH.mm.ss:ffff}) Listener stopped");
        });

        _tcpListenerThread.Start();
        Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] ({DateTime.Now:HH.mm.ss:ffff}) Listener starting");
    }

    /// <summary>
    /// 
    /// </summary>
    public void End()
    {
        _cancellation.Cancel();

        Debug.WriteLine("Listener stopping");
    }

}

