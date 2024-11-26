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
        var i = 0;
        for (; i < buffer.Length; i++)
        {
            var b = inner.ReadByte();
            if (b == -1)
                throw new IOException("inner.ReadByte returns -1");

            buffer[i] = (byte)b;
            if (buffer[i] == '\n')
                break;
        }

        if (i == buffer.Length) return string.Empty;
        if (i == 0) return string.Empty;

        if (buffer[i - 1] == '\r')
            i--;

        return Encoding.UTF8.GetString(buffer, 0, i);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    public override void Write(byte[] buffer, int offset, int count) { }
}