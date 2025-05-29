using System.Text;

namespace dotNetExpress.Overrides;

/// <summary>
/// 
/// </summary>
/// <param name="inner"></param>
/// <param name="bufferSize"></param>
public class MessageBodyStreamReader(Stream inner, long bufferSize = 256 * 1024) : Stream
{
    private long _length;

    public string FileName;

    public override bool CanRead => inner.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => _length;

    public override long Position { get; set; }

    public override void Flush() => inner.Flush();

    public override long Seek(long offset, SeekOrigin origin) { return 0; }

    public override void SetLength(long value) => _length = value;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="offset"></param>
    /// <param name="bytesToRead"></param>
    /// <returns></returns>
    public override int Read(byte[] buffer, int offset, int bytesToRead)
    {
        if (Position + bytesToRead > Length)
            bytesToRead = (int)(Length - Position);

        if (bytesToRead == 0)
            return 0;

        var bytesRead = inner.Read(buffer, offset, bytesToRead);

        Position += bytesRead;

        return bytesRead;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string ReadLine()
    {
        var buffer = new byte[bufferSize];
        int i = 0;

        for (; i < buffer.Length; i++)
        {
            int b = inner.ReadByte(); // Has a timeout and will raise SocketException if the connection idles
            if (b == -1)
            {
                if (i == 0)
                    throw new IOException("inner.ReadByte returns -1 before reading any data");
                break;
            }

            buffer[i] = (byte)b;
            if (b == '\n')
                break;
        }

        if (i == 0)
            return string.Empty;

        int length = i;
        if (buffer[length - 1] == '\n')
            length--;

        if (length > 0 && buffer[length - 1] == '\r')
            length--;

        return Encoding.UTF8.GetString(buffer, 0, length);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    public override void Write(byte[] buffer, int offset, int count) { }
}