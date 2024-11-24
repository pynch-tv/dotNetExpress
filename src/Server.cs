using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace dotNetExpress;

/// <summary>
/// 
/// </summary>
public class Server : TcpListener
{
    private Thread _tcpListenerThread;

    private Express _express;

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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="express"></param>
    public async Task Begin(Express express)
    {
        _express = express;

        Start();

        _tcpListenerThread = new Thread(() =>
        {
            Debug.WriteLine("Listener started");

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
                        var AwaiterTask = Task.Run(async () =>
                        {
                            try
                            {
                                Debug.WriteLine($"Awaiting new connection");

                                var tcpClient = await AcceptTcpClientAsync(_cancellation.Token);
                                await new Client().Connection(_express, tcpClient);
                            }
                            catch (OperationCanceledException)
                            {
                                keepGoing = false;
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine($"Error in Client.Connection: {e.Message}");
                                keepGoing = false;
                            }
                        });
                        tcpClientTasks.Add(AwaiterTask);
                    }

                    var RemoveAtIndex = Task.WaitAny([.. tcpClientTasks], awaiterTimeoutInMS);
                    if (RemoveAtIndex > 0)
                    {
                        Debug.WriteLine($"removing connection {RemoveAtIndex}");
                        tcpClientTasks.RemoveAt(RemoveAtIndex);
                    }
                }
            }
            catch (Exception) 
            {
            }

            Debug.WriteLine("Listener stopped");
        });

        _tcpListenerThread.Start();
        Debug.WriteLine("Listener starting");
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

