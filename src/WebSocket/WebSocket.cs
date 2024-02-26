using System;
using System.Net.Sockets;

namespace dotNetExpress;

public partial class WebSocket
{
    private const int MessageBufferSize = 1024;
    private readonly WsServer _server;

    private readonly Socket _socket;

    /// <summary>
    /// </summary>
    /// <param name="server"></param>
    /// <param name="socket"></param>
    public WebSocket(WsServer server, Socket socket)
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
    public WsServer GetServer()
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
            var opCode = WsFrameFactory.GetOpcode(messageBuffer);

            // If the audioVideoServer was closed
            if (opCode == OpcodeType.ClosedConnection)
            {
                GetServer().Disconnect(this);
                return;
            }

            // Start to receive messages again
            GetSocket().BeginReceive(new byte[] { 0 }, 0, 0, SocketFlags.None, MessageCallback, null);
        }
        catch (Exception)
        {
            GetSocket().Close();
            GetSocket().Dispose();
            GetServer().Disconnect(this);
        }
    }
}