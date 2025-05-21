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

        Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] ({DateTime.Now:HH.mm.ss:ffff}) Listener starting");

        _ = Task.Run(() => RunListenerAsync(express, _cancellation.Token));
    }

    private async Task RunListenerAsync(Express express, CancellationToken cancellationToken)
    {
        Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] ({DateTime.Now:HH.mm.ss:ffff}) Listener started");

        using var semaphore = new SemaphoreSlim(express.MaxConcurrentListeners);
        var tasks = new List<Task>();
        int awaiterTimeoutInMS = 500;

        cancellationToken.Register(Stop);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Remove completed tasks
                tasks.RemoveAll(t => t.IsCompleted);

                // Start new tasks if we have capacity
                while (tasks.Count < express.MaxConcurrentListeners)
                {
                    await semaphore.WaitAsync(cancellationToken);

                    var task = HandleIncomingConnectionAsync(semaphore, cancellationToken);
                    tasks.Add(task);
                }

                // Wait a bit to allow some task completions
                await Task.WhenAny(tasks.Concat([Task.Delay(awaiterTimeoutInMS, cancellationToken)]));
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful exit on cancellation
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] ({DateTime.Now:HH.mm.ss:ffff}) Inner loop exception: {ex.Message}");
            cancellationToken.ThrowIfCancellationRequested();
        }

        Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] ({DateTime.Now:HH.mm.ss:ffff}) Listener stopped");
    }

    private async Task HandleIncomingConnectionAsync(SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        try
        {
            // Debug.WriteLine($"[T{Task.CurrentId}] ({DateTime.Now:HH.mm.ss:ffff}) Awaiting new connection");
            var client = await AcceptTcpClientAsync(cancellationToken);
            RaiseHandleConnection(client);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[T{Task.CurrentId}] ({DateTime.Now:HH.mm.ss:ffff}) Error in Client.Connection: {ex.Message}");
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void End()
    {
        // Note: calling Cancel here will Stop this listener.
        //       see the Token.Register function where this.Stop() is called.
        _cancellation.Cancel();

        Debug.WriteLine("Listener stopping");
    }

}

