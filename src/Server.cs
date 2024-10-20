using System;
using System.Collections.Generic;
using dotNetExpress.Overrides;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace dotNetExpress;

internal class Parameters
{
    public Express Express { get; }
    public TcpClient TcpClient { get; }
    private Parameters(Express express, TcpClient tcpClient)
    {
        Express = express;
        TcpClient = tcpClient;
    }

    public static Parameters CreateInstance(Express express, TcpClient tcpClient)
    {
        return new Parameters(express, tcpClient);
    }
}

public class Server : TcpListener
{
    private Thread _tcpListenerThread;

    private bool _running;

    private Express _express;

    public readonly WsServer WsServer = new();

    /// <summary>
    /// KeepALive allows HTTP clients to re-use connections for multiple requests, and relies on timeout configurations
    /// on both the client and target server to decide when to close open TCP sockets.
    ///
    /// There is overhead in establishing a new TCP connection (DNS lookups, TCP handshake, SSL/TLS handshake, etc).
    /// Without a keep-alive, every HTTP request has to establish a new TCP connection, and then close the connection
    /// once the response has been sent/received. A keep-alive allows an existing TCP connection to be re-used for
    /// multiple requests/responses, thus avoiding all of that overhead. That is what makes the connection "persistent".
    /// </summary>
    public bool KeepAlive = false;

    /// <summary>
    /// An integer that is the time in seconds that the host will allow an idle connection to remain open before it is closed.
    /// A connection is idle if no data is sent or received by a host. A host may keep an idle connection open for longer than
    /// timeout seconds, but the host should attempt to retain a connection for at least timeout seconds.
    /// </summary>
    public int KeepAliveTimeout = 2; // timing in seconds

    public int MaxSockets = 2; // not used

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
    public void Begin(Express express)
    {
        _express = express;

        Start();

        _tcpListenerThread = new Thread(() =>
        {
            _running = true;
            while (_running)
            {
                try
                {
                    _ = ThreadPool.QueueUserWorkItem(ClientConnection!, Parameters.CreateInstance(_express, AcceptTcpClient()));
                }
                catch (Exception e)
                {
                    _running = false;
                }
            }
        });
        _tcpListenerThread.Start();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stateInfo"></param>
    private void ClientConnection(object stateInfo)
    {
        try
        {
            if (stateInfo is not Parameters param) return;

            var tcpClient = param.TcpClient;
            var stream = tcpClient.GetStream();
            var express = param.Express;

            stream.ReadTimeout = KeepAliveTimeout * 1000; // KeepAliveTimeout is in seconds, ReadTimeout in milliseconds
            if (stream.ReadTimeout == 0) KeepAlive = false;

            while (true)
            {
                // Read the http headers
                // Note: the body part is NOT read at this stage
                var headerLines = new List<string>();
                var streamReader = new MessageBodyStreamReader(stream);
                {
                    try
                    {
                        while (true)
                        {
                            // Readline is a blocking IO call. If it takes to long for data to come in,
                            // an IOException is thrown.
                            // Note: if KeepAlive is true, ReadTimeout will be set to KeepAliveTimeout
                            var line = streamReader.ReadLine();
                            if (string.IsNullOrEmpty(line))
                                break;

                            headerLines.Add(line);
                        }

                    }
                    catch (Exception e) // includes IOException when timeout
                    {
                        streamReader.Close();
                        tcpClient.Close();

                        return;
                    }
                }

                // Construct a Request object, based on the header info
                if (!Request.TryParse(express, headerLines, out var req))
                    throw new HttpProtocolException(500, "Unable to construct Request",
                        new ProtocolViolationException("Unable to construct Request"));

                // If the request header has an explicit request to close the connection, set
                // KeepAlive to false
                var connection = req.Headers["Connection"];
                if (connection != null)
                {
                    KeepAlive = !connection!.Equals("close");
                }

                // get the IP from the remove endPoint
                if (tcpClient.Client.RemoteEndPoint is IPEndPoint ipEndPoint)
                    req.Ip = ipEndPoint.Address;

                // Make Response object
                req.Res = new Response(express, stream);

                if (!string.IsNullOrEmpty(req.Get("Content-Length")))
                {
                    var contentLength = int.Parse(req.Get("Content-Length"));

                    // When a content-length is available, a stream is provided in Request
                    req.StreamReader = streamReader;
                    req.StreamReader.SetLength(contentLength);
                }

                if (WsFactory.IsUpgradeRequest(req))
                {
                    WsFactory.SendUpgradeResponse(req, req.Res);

                    // Spin of a WebSocket
                    WsServer.Connect(tcpClient.Client);

                    break;
                }
                else
                {
                    express.Dispatch(req, req.Res);

                    req.StreamReader?.Close();

                    // https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.tcplistener.accepttcpclient?view=net-8.0
                    // Remark: When you are through with the TcpClient, be sure to call its Close method. If you want greater
                    // flexibility than a TcpClient offers, consider using AcceptSocket.
                    if (KeepAlive) continue;

                    // Do not keep the connection alive
                    tcpClient.Close();
                    break;
                }
            }

        }
        catch (HttpProtocolException e)
        {
            // Typical exception when the client disconnects prematurely
            // Assumes socket closes automatically
        }
        catch (IOException e)
        {
        }
        catch (Exception e)
        {
            // TODO: send server error
        }
    }
}