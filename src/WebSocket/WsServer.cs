using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace dotNetExpress;

public class WsServer
{
    private static readonly List<WebSocket> _webSockets = new();

    public void Connect(Socket socket)
    {
        var ws = new WebSocket(this, socket);

        lock (_webSockets)
        {
            _webSockets.Add(ws);
        }

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="client"></param>
    public void Disconnect(WebSocket client)
    {
        lock (_webSockets)
        {
            _webSockets.Remove(client);
        }
    }

    public static void SendText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        var frameMessage = WsFrameFactory.FromString(text);

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


}