using System.Collections.Generic;
using System.Text;

namespace dotNetExpress;

public static class WsFrameFactory
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static byte[] FromString(string message)
    {
        const WebSocket.OpcodeType opcode = WebSocket.OpcodeType.Text;

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
    /// <param name="frame"></param>
    /// <returns></returns>
    public static WebSocket.OpcodeType GetOpcode(IReadOnlyList<byte> frame)
    {
        return (WebSocket.OpcodeType)frame[0] - 128;
    }

}