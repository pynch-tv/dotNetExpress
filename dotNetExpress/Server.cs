using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using dotNetExpress;

namespace dotNetExpress;

/// <summary>
/// 
/// </summary>
public class Server : TcpListener
{
    const int MaxHandlers = 10;

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
    public event EventHandler<TcpClient>? HandleConnection;

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

    public async Task Begin(Express express)
    {
        Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] ({DateTime.Now:HH.mm.ss:ffff}) TcpListener starting");

        Start();

//        var connectionPool = Channel.CreateBounded<TcpClient>(10);

        // Use an unbounded channel
        Channel<TcpClient> clientQueue = Channel.CreateUnbounded<TcpClient>(new UnboundedChannelOptions
        {
            SingleWriter = true,
            SingleReader = false
        });


        // Accept loop
        _ = Task.Run(async () =>
        {
            try
            {
                while (true)
                {
                    TcpClient client = await this.AcceptTcpClientAsync();
                    await clientQueue.Writer.WriteAsync(client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Accept loop error: {ex}");
                clientQueue.Writer.Complete();
            }
        });

        //// Start background handlers (1 per slot in pool)
        //for (int i = 0; i < MaxHandlers; i++)
        //{
        //    _ = Task.Run(async () =>
        //    {
        //        await foreach (var connection in connectionPool.Reader.ReadAllAsync())
        //        {
        //            try
        //            {
        //                Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] ({DateTime.Now:HH.mm.ss:ffff}) Started client");
        //                RaiseHandleConnection(connection);
        //                Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] ({DateTime.Now:HH.mm.ss:ffff}) Ended client");
        //            }
        //            catch (Exception ex)
        //            {
        //                Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] ({DateTime.Now:HH.mm.ss:ffff}) Error in Client.Connection: {ex.Message}");
        //            }
        //        }
        //    });
        //}

        // Handler pool (10 handlers)
        int maxHandlers = 10;

        for (int i = 0; i<maxHandlers; i++)
        {
            _ = Task.Run(async () =>
            {
                await foreach (var connection in clientQueue.Reader.ReadAllAsync())
                {
                    try
                    {
                        Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] ({DateTime.Now:HH.mm.ss:ffff}) Started client");
                        RaiseHandleConnection(connection);
                        Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] ({DateTime.Now:HH.mm.ss:ffff}) Ended client");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Handler exception: {ex}");
                    }
                    finally
                    {
                        connection.Close(); // Always close TcpClient
                    }
                }
            });
        }

    }


    private async Task HandleIncomingConnectionAsync(SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        try
        {
            var client = await AcceptTcpClientAsync(cancellationToken);
            Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] ({DateTime.Now:HH.mm.ss:ffff}) Accepted client");
            RaiseHandleConnection(client);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] ({DateTime.Now:HH.mm.ss:ffff}) Error in Client.Connection: {ex.Message}");
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
        Debug.WriteLine("Listener stopping");
    }

}

