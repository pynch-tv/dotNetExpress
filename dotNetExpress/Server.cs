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

    private Channel<TcpClient> _clientQueue = Channel.CreateUnbounded<TcpClient>(new UnboundedChannelOptions
    {
        SingleWriter = true,
        SingleReader = false
    });

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

        // Accept loop
        _ = Task.Run(async () =>
        {
            try
            {
                while (true)
                {
                    TcpClient client = await this.AcceptTcpClientAsync();
                    await _clientQueue.Writer.WriteAsync(client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Accept loop error: {ex}");
                _clientQueue.Writer.Complete();
            }
        });

        for (int i = 0; i < MaxHandlers; i++)
        {
            _ = Task.Run(async () =>
            {
                await foreach (var connection in _clientQueue.Reader.ReadAllAsync())
                {
                    try
                    {
                        //Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] ({DateTime.Now:HH.mm.ss:ffff}) Started client");
                        RaiseHandleConnection(connection);
                        //Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] ({DateTime.Now:HH.mm.ss:ffff}) Ended client");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Handler exception: {ex}");
                    }
                    finally
                    {
                        // Don't close connection here, it is done in the handler
                    }
                }
            });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void End()
    {
        _clientQueue.Writer.Complete();

        Debug.WriteLine("Listener stopping");
    }

}

