using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace dotNetExpress.Tools;
public partial class SseSocket
{
    private const int MessageBufferSize = 1024;
    private readonly SseServer _server;

    private readonly Socket _socket;

    public DateTime LastAction;

    /// <summary>
    /// </summary>
    /// <param name="server"></param>
    /// <param name="socket"></param>
    public SseSocket(SseServer server, Socket socket)
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
    public SseServer GetServer()
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
            if (asyncResult.AsyncState is not Socket clientSocket)
            {
                Debug.WriteLine($"SSE asyncState error is not socket");
                throw new Exception("SSE asyncState error is not socket");
            }

            int bufferSize = clientSocket.EndReceive(asyncResult, out var errorCode);

            if (errorCode == SocketError.Success)
            {
                // Read the incoming message 
                var messageBuffer = new byte[MessageBufferSize];
                var bytesReceived = GetSocket().Receive(messageBuffer);

                // Resize the byte array to remove whitespaces 
                if (bytesReceived < messageBuffer.Length) Array.Resize(ref messageBuffer, bytesReceived);

                Debug.WriteLine($"SSE Bytes received: {bytesReceived} {Encoding.UTF8.GetString(messageBuffer)} ");

                // Start to receive messages again
                GetSocket().BeginReceive([0], 0, 0, SocketFlags.None, MessageCallback, null);
            }
            else
            {
                Debug.WriteLine($"SSE error: {errorCode}");

                GetSocket().Shutdown(SocketShutdown.Both);
                GetSocket().Close();
                GetSocket().Dispose();

                GetServer().Remove(this);
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine($"SSE exception: {e.Message}");

            GetSocket().Shutdown(SocketShutdown.Both);
            GetSocket().Close();
            GetSocket().Dispose();

            GetServer().Remove(this);
        }
    }
}