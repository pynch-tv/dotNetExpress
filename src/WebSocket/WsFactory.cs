using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace dotNetExpress;

public static class WsFactory
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static string HashKey(string key)
    {
        const string handshakeKey = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        var longKey = key + handshakeKey;
        var hashBytes = SHA1.HashData(Encoding.ASCII.GetBytes(longKey));

        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    public static bool IsUpgradeRequest(Request req)
    {
        return (string.Equals(req.Get("connection"), "Upgrade", StringComparison.OrdinalIgnoreCase)
                && string.Equals(req.Get("upgrade"), "websocket", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="req"></param>
    /// <param name="res"></param>
    public static void SendUpgradeResponse(Request req, Response res)
    {
        var key = req.Get("sec-websocket-key");

        res.Set("Upgrade", "WebSocket");
        res.Set("Connection", "Upgrade");
        res.Set("Sec-WebSocket-Accept", HashKey(key));

        res.WriteHead(HttpStatusCode.SwitchingProtocols);
    }
}