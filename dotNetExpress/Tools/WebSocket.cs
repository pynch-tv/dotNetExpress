using System.Diagnostics;
using System.Net.Sockets;

namespace dotNetExpress.Tools;
public partial class WebSocket
{
    private const int MessageBufferSize = 1024;
    private readonly WsServer _server;

    private readonly Socket _socket;

    public DateTime LastAction;

    /// <summary>
    /// </summary>
    /// <param name="server"></param>
    /// <param name="socket"></param>
    public WebSocket(WsServer server, Socket socket)
    {
        _server = server;
        _socket = socket;

        GetSocket().BeginReceive([0], 0, 0, SocketFlags.None, MessageCallback, null);
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
            GetSocket().EndReceive(asyncResult, out var errorCode);

            if (errorCode == SocketError.Success)
            {
                // Read the incoming message 
                var messageBuffer = new byte[MessageBufferSize];
                var bytesReceived = GetSocket().Receive(messageBuffer);

                // Resize the byte array to remove whitespaces 
                if (bytesReceived < messageBuffer.Length) Array.Resize(ref messageBuffer, bytesReceived);

                // Get the opcode of the frame
                var opCode = WsFrameFactory.GetOpcode(messageBuffer);

                // If the socket was closed
                if (opCode == OpcodeType.ClosedConnection)
                {
                    Debug.WriteLine($"WS Closing");

                    // When using a connection-oriented Socket, always call the Shutdown method
                    // before closing the Socket. This ensures that all data is sent and received
                    // on the connected socket before it is closed. Call the Close method to free
                    // all managed and unmanaged resources associated with the Socket.
                    GetSocket().Shutdown(SocketShutdown.Both);
                    GetSocket().Close();
                    GetSocket().Dispose();

                    GetServer().Remove(this);
                }
                else
                {
                    // Don't care about the content of the message
                    //var data = WsFrameFactory.GetDataFromFrame(messageBuffer);
                    //Debug.WriteLine($"WS received: {data}");

                    // Start to receive messages again
                    GetSocket().BeginReceive([0], 0, 0, SocketFlags.None, MessageCallback, null);
                }
            }
            else
            {
                Debug.WriteLine($"WS error: {errorCode}");

                // When using a connection-oriented Socket, always call the Shutdown method
                // before closing the Socket. This ensures that all data is sent and received
                // on the connected socket before it is closed. Call the Close method to free
                // all managed and unmanaged resources associated with the Socket.
                GetSocket().Shutdown(SocketShutdown.Both);
                GetSocket().Close();
                GetSocket().Dispose();

                GetServer().Remove(this);
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine($"WS exception: {e.Message}");

            GetSocket().Shutdown(SocketShutdown.Both);
            GetSocket().Close();
            GetSocket().Dispose();
            GetServer().Remove(this);
        }
    }
}