using System.Text;

namespace dotNetExpress.Tools;
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

        frame[0] = 128 + (int)opcode;

        int indexStartRawData;
        if (length <= 125)
        {
            frame[1] = (byte)length;
            indexStartRawData = 2;
        }
        else if (length <= 65535)
        {
            frame[1] = 126;
            frame[2] = (byte)(length >> 8 & 255);
            frame[3] = (byte)(length & 255);
            indexStartRawData = 4;
        }
        else
        {
            frame[1] = 127;
            frame[2] = (byte)(length >> 56 & 255);
            frame[3] = (byte)(length >> 48 & 255);
            frame[4] = (byte)(length >> 40 & 255);
            frame[5] = (byte)(length >> 32 & 255);
            frame[6] = (byte)(length >> 24 & 255);
            frame[7] = (byte)(length >> 16 & 255);
            frame[8] = (byte)(length >> 8 & 255);
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

    public struct SFrameMaskData
    {
        public int DataLength, KeyIndex, TotalLenght;
        public EOpcodeType Opcode;

        public SFrameMaskData(int DataLength, int KeyIndex, int TotalLenght, EOpcodeType Opcode)
        {
            this.DataLength = DataLength;
            this.KeyIndex = KeyIndex;
            this.TotalLenght = TotalLenght;
            this.Opcode = Opcode;
        }
    }

    /// <summary>
    /// Enum for opcode types
    /// </summary>
    public enum EOpcodeType
    {
        /* Denotes a continuation code */
        Fragment = 0,

        /* Denotes a text code */
        Text = 1,

        /* Denotes a binary code */
        Binary = 2,

        /* Denotes a closed connection */
        ClosedConnection = 8,

        /* Denotes a ping*/
        Ping = 9,

        /* Denotes a pong */
        Pong = 10
    }

    /// <summary>Gets data for a encoded websocket frame message</summary>
    /// <param name="Data">The data to get the info from</param>
    /// <returns>The frame data</returns>
    public static SFrameMaskData GetFrameData(byte[] Data)
    {
        // Get the opcode of the frame
        int opcode = Data[0] - 128;

        // If the length of the message is in the 2 first indexes
        if (Data[1] - 128 <= 125)
        {
            int dataLength = (Data[1] - 128);
            return new SFrameMaskData(dataLength, 2, dataLength + 6, (EOpcodeType)opcode);
        }

        // If the length of the message is in the following two indexes
        if (Data[1] - 128 == 126)
        {
            // Combine the bytes to get the length
            int dataLength = BitConverter.ToInt16(new byte[] { Data[3], Data[2] }, 0);
            return new SFrameMaskData(dataLength, 4, dataLength + 8, (EOpcodeType)opcode);
        }

        // If the data length is in the following 8 indexes
        if (Data[1] - 128 == 127)
        {
            // Get the following 8 bytes to combine to get the data 
            byte[] combine = new byte[8];
            for (int i = 0; i < 8; i++) combine[i] = Data[i + 2];

            // Combine the bytes to get the length
            //int dataLength = (int)BitConverter.ToInt64(new byte[] { Data[9], Data[8], Data[7], Data[6], Data[5], Data[4], Data[3], Data[2] }, 0);
            int dataLength = (int)BitConverter.ToInt64(combine, 0);
            return new SFrameMaskData(dataLength, 10, dataLength + 14, (EOpcodeType)opcode);
        }

        // error
        return new SFrameMaskData(0, 0, 0, 0);
    }

    /// <summary>Gets the decoded frame data from the given byte array</summary>
    /// <param name="Data">The byte array to decode</param>
    /// <returns>The decoded data</returns>
    public static string GetDataFromFrame(byte[] Data)
    {
        // Get the frame data
        SFrameMaskData frameData = GetFrameData(Data);

        // Get the decode frame key from the frame data
        byte[] decodeKey = new byte[4];
        for (int i = 0; i < 4; i++) decodeKey[i] = Data[frameData.KeyIndex + i];

        int dataIndex = frameData.KeyIndex + 4;
        int count = 0;

        // Decode the data using the key
        for (int i = dataIndex; i < frameData.TotalLenght; i++)
        {
            Data[i] = (byte)(Data[i] ^ decodeKey[count % 4]);
            count++;
        }

        // Return the decoded message 
        return Encoding.Default.GetString(Data, dataIndex, frameData.DataLength);
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