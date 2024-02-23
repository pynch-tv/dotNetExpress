using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace dotNetExpress;

internal class Utils
{
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    private static string HashKey(string key)
    {
        const string handshakeKey = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        var longKey = key + handshakeKey;

        var sha1 = SHA1.Create();
        var hashBytes = sha1.ComputeHash(Encoding.ASCII.GetBytes(longKey));

        return Convert.ToBase64String(hashBytes);
    }


    /// <summary>
    /// 
    /// </summary>
    private static readonly List<TcpClient> _webSockets = new();

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

        res._send();

        //lock (_webSockets)
        //{
        //    _webSockets.Add(tcpClient);
        //}
    }

    /// <summary>
    /// 
    /// </summary>
    private static void CompleteHttpRequest(Request req, Response res)
    {
        var app = res.App();
        app.Router().Dispatch(req, res);

        res._send();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stateInfo"></param>
    internal static void ClientThread(object stateInfo)
    {
        var aa = stateInfo as Parameters;
        var tcpClient = aa.TcpClient;
        var express = aa.Express;

        // Read the http headers
        // Note: the body part is NOT read at this stage
        var headerLines = new List<string>();
        var sr = new StreamReader(tcpClient.GetStream());
        {
            while (true)
            {
                var line = sr.ReadLine();
                if (string.IsNullOrEmpty(line)) break;
                headerLines.Add(line);
            }
        }

        // Construct a Request object, based on the header info
        if (!Request.TryParse(headerLines.ToArray(), out var req))
        {
            // error - return
        }

        // Make Response object
        var res = new Response(express, tcpClient);

        if (IsWebSocketUpgradeRequest(req))
        {
            DoWebSocketUpgradeRequest(req, res);

            // These socket are not disposed, but kept!
            lock (_webSockets)
            {
                _webSockets.Add(tcpClient);
            }
        }
        else
        {
            CompleteHttpRequest(req, res);
        }

    }
}
