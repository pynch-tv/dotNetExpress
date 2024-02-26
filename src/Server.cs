using System;
using System.Collections.Generic;
using dotNetExpress.Overrides;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Pynch.Nexa.Tools.Net;

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

public class WsClient
{
    public enum OpcodeType
    {
        /* Denotes a continuation code */
        Fragment = 0,

        /* Denotes a text code */
        Text = 1,

        /* Denotes a binary code */
        Binary = 2,

        /* Denotes a closed audioVideoServer */
        ClosedConnection = 8
    }

    private const int MessageBufferSize = 1024;
    private readonly Server _server;

    private readonly Socket _socket;

    /// <summary>
    /// </summary>
    /// <param name="server"></param>
    /// <param name="socket"></param>
    public WsClient(Server server, Socket socket)
    {
        _server = server;
        _socket = socket;

        GetSocket().BeginReceive(new byte[] { 0 }, 0, 0, SocketFlags.None, MessageCallback, null);
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public Socket GetSocket()
    {
        return _socket;
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public Server GetServer()
    {
        return _server;
    }

    /// <summary>
    /// </summary>
    /// <param name="asyncResult"></param>
    private void MessageCallback(IAsyncResult asyncResult)
    {
        try
        {
            GetSocket().EndReceive(asyncResult);

            // Read the incoming message 
            var messageBuffer = new byte[MessageBufferSize];
            var bytesReceived = GetSocket().Receive(messageBuffer);

            // Resize the byte array to remove whitespaces 
            if (bytesReceived < messageBuffer.Length) Array.Resize(ref messageBuffer, bytesReceived);

            // Get the opcode of the frame
            var opCode = GetFrameOpcode(messageBuffer);

            // If the audioVideoServer was closed
            if (opCode == OpcodeType.ClosedConnection)
            {
                GetServer().WsDisconnect(this);
                return;
            }

            // Start to receive messages again
            GetSocket().BeginReceive(new byte[] { 0 }, 0, 0, SocketFlags.None, MessageCallback, null);
        }
        catch (Exception)
        {
            GetSocket().Close();
            GetSocket().Dispose();
            GetServer().WsDisconnect(this);
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="frame"></param>
    /// <returns></returns>
    private static OpcodeType GetFrameOpcode(IReadOnlyList<byte> frame)
    {
        return (OpcodeType)frame[0] - 128;
    }
}

public class Server : TcpListener
{
    private Thread _tcpListenerThread;

    private bool _running = false;

    private Express _express;

    private static readonly List<WsClient> _webSockets = new();

    public bool KeepAlive = false;

    public int KeepAliveTimeout = 3; // timing in ms

    #region Constructor

    public Server(int port) : base(port)
    {
    }

    public Server(IPAddress localaddr, int port) : base(localaddr, port)
    {
    }

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

        var listenerSocket = this.Server;
        //var lingerOption = new LingerOption(true, 10);
        //listenerSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, lingerOption);

        //listenerSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, KeepAliveTimeout);
        //listenerSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 5);
        //listenerSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 5); //note this doesnt work on some windows versions
        //listenerSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, KeepAlive);

        this.Start();

        _tcpListenerThread = new Thread(() =>
        {
            _running = true;
            while (_running)
            {
                try
                {
                    _ = ThreadPool.QueueUserWorkItem(ClientConnection!, Parameters.CreateInstance(_express, this.AcceptTcpClient()));
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
    public void End()
    {

    }

    #region WebSocket


    /// <summary>
    /// </summary>
    /// <param name="client"></param>
    public void WsDisconnect(WsClient client)
    {
        lock (_webSockets)
        {
            _webSockets.Remove(client);
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public void WsSend(string data)
    {
        var frameMessage = GetFrameFromString(data);

        lock (_webSockets)
        {
            // reverse iterate, so that we can remove
            // items from array in case of error
            // without screwing up the array
            for (var i = _webSockets.Count - 1; i >= 0; i--)
            {
                var client = _webSockets[i];

                try
                {
                    client.GetSocket().Send(frameMessage);
                }
                catch (Exception)
                {
                    _webSockets.Remove(client);
                }
            }
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="message"></param>
    /// <param name="opcode"></param>
    /// <returns></returns>
    private static byte[] GetFrameFromString(string message, OpcodeTypeEnum opcode = OpcodeTypeEnum.Text)
    {
        var bytesRaw = Encoding.Default.GetBytes(message);
        var frame = new byte[10];
        var length = bytesRaw.Length;

        frame[0] = (byte)(128 + (int)opcode);

        int indexStartRawData;
        if (length <= 125)
        {
            frame[1] = (byte)length;
            indexStartRawData = 2;
        }
        else if (length <= 65535)
        {
            frame[1] = 126;
            frame[2] = (byte)((length >> 8) & 255);
            frame[3] = (byte)(length & 255);
            indexStartRawData = 4;
        }
        else
        {
            frame[1] = 127;
            frame[2] = (byte)((length >> 56) & 255);
            frame[3] = (byte)((length >> 48) & 255);
            frame[4] = (byte)((length >> 40) & 255);
            frame[5] = (byte)((length >> 32) & 255);
            frame[6] = (byte)((length >> 24) & 255);
            frame[7] = (byte)((length >> 16) & 255);
            frame[8] = (byte)((length >> 8) & 255);
            frame[9] = (byte)(length & 255);

            indexStartRawData = 10;
        }

        var response = new byte[indexStartRawData + length];

        var responseIdx = 0;

        for (var i = 0; i < indexStartRawData; i++)
        {
            response[responseIdx] = frame[i];
            responseIdx++;
        }

        for (var i = 0; i < length; i++)
        {
            response[responseIdx] = bytesRaw[i];
            responseIdx++;
        }

        return response;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    private static string HashKey(string key)
    {
        const string handshakeKey = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        var longKey = key + handshakeKey;
        var hashBytes = SHA1.HashData(Encoding.ASCII.GetBytes(longKey));

        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private static bool IsWebSocketUpgradeRequest(Request req)
    {
        return (string.Equals(req.Get("connection"), "Upgrade", StringComparison.OrdinalIgnoreCase)
                && string.Equals(req.Get("upgrade"), "websocket", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="req"></param>
    /// <param name="res"></param>
    private static void DoWebSocketUpgradeRequest(Request req, Response res)
    {
        var key = req.Get("sec-websocket-key");

        res.Set("Upgrade", "WebSocket");
        res.Set("Connection", "Upgrade");
        res.Set("Sec-WebSocket-Accept", HashKey(key));
        res.Status(HttpStatusCode.SwitchingProtocols);
        res.End();
    }

    #endregion

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

            // Read the http headers
            // Note: the body part is NOT read at this stage
            var headerLines = new List<string>();
            var streamReader = new MessageBodyStreamReader(stream);
            {
                while (true)
                {
                    var line = streamReader.ReadLine();
                    if (string.IsNullOrEmpty(line))
                        break;
                    headerLines.Add(line);
                }
            }

            // Construct a Request object, based on the header info
            if (!Request.TryParse(express, headerLines.ToArray(), out var req))
                throw new HttpProtocolException(500, "Unable to construct Request", new ProtocolViolationException("Unable to construct Request"));

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

            if (IsWebSocketUpgradeRequest(req))
            {
                DoWebSocketUpgradeRequest(req, req.Res);

                // These socket are not disposed, but kept!
                lock (_webSockets)
                {
                    _webSockets.Add( new WsClient(this, tcpClient.Client));
                }
            }
            else
            {
                express.Dispatch(req, req.Res);

                // https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.tcplistener.accepttcpclient?view=net-8.0
                // Remark: When you are through with the TcpClient, be sure to call its Close method. If you want greater
                // flexibility than a TcpClient offers, consider using AcceptSocket.
                if (!KeepAlive)
                    tcpClient.Close();
            }
        }
        catch (HttpProtocolException e)
        {

        }
        catch (Exception e)
        {

        }
    }
}