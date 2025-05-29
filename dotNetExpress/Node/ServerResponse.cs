using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace dotNetExpress.Node;

public class ServerResponse
{
    protected readonly Stream _stream;

    protected bool _sendDate = true;

    protected bool _chunked;

    protected HttpStatusCode _httpStatusCode = HttpStatusCode.OK;

    protected Dictionary<string, string> _headers = new(StringComparer.OrdinalIgnoreCase);

    private static readonly ReadOnlyMemory<byte> CrLf = "\r\n"u8.ToArray();

    #region Events

    public event EventHandler<Dictionary<string, string>>? WriteHeaders;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    protected virtual void RaiseWriteHeaders(Dictionary<string, string> e)
    {
        WriteHeaders?.Invoke(this, e);
    }

    #endregion

    /// <summary>
    /// 
    /// </summary>
    public Socket? Socket = null;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="stream"></param>
    protected ServerResponse(Stream stream)
    {
        _stream = stream;
        HeadersSent = false;
        _chunked = false;
    }

    /// <summary>
    /// 
    /// </summary>
    public Socket? Connection { get { return Socket; } }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="field"></param>
    /// <returns></returns>
    protected string? GetHeader(string field)
    {
        return _headers[field];
    }

    /// <summary>
    /// Boolean property that indicates if the app sent HTTP headers for the response.
    /// Boolean (read-only). True if headers were sent, false otherwise.
    /// </summary>
    protected bool HeadersSent { get; set; }

    /// <summary>
    /// When true, the Date header will be automatically generated and sent in
    /// the response if it is not already present in the headers. Defaults to true.
    /// </summary>
    protected void SendDate(bool sendDate = true)
    {
        _sendDate = sendDate;
    }

    /// <summary>
    /// Sets the response’s HTTP header field to value.
    /// </summary>
    /// <param name="field"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public void Set(string field, string? value = null)
    {
        if (_headers.TryGetValue(field, out var currentValue))
        {
            if (string.IsNullOrEmpty(value)) return;
            else
            {
                var content = currentValue?.Split(',');
                if (content != null && !content.Contains(value))
                    _headers[field] += ", " + value;
            }
        }
        else
            if (null != value)
                _headers[field] = value;
    }

    /// <summary>
    /// Sets the response’s HTTP header field to value. To set multiple fields at once, pass an object as the parameter.
    /// </summary>
    /// <param name="collection"></param>
    public void Set(Dictionary<string, string> collection)
    {
        foreach (var kvp in collection)
            _headers.Add(kvp.Key, kvp.Value);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="field"></param>
    /// <param name="value"></param>
    public void Set(string field, int value)
    {
        _headers[field] = value.ToString();
    }

    public void Set(string field, long value)
    {
        _headers[field] = value.ToString();
    }

    /// <summary>
    /// Returns the HTTP response header specified by field. The match is case-insensitive.
    /// </summary>
    /// <param name="field"></param>
    /// <returns></returns>
    public string? Get(string field)
    {
        if (_headers.TryGetValue(field, out var value)) return value;
        return "";
    }

    /// <summary>
    /// Returns true if the header identified by name is currently set in the outgoing headers.
    /// The header name matching is case-insensitive.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    protected bool HasHeader(string key)
    {
        if (_headers.TryGetValue(key, out _)) return true;
        else return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="encoding"></param>
    public bool Write(string buffer, Encoding? encoding = null)
    {
        // The first time response.write() is called, it will send the buffered header
        // information and the first chunk of the body to the client
        if (!HeadersSent)
            WriteHead(_httpStatusCode, _httpStatusCode.ToString());

        encoding ??= Encoding.UTF8;

        var body = encoding.GetBytes(buffer);
        var bodyLength = encoding.GetByteCount(buffer);

        if (bodyLength <= 0) return true;

        _stream.WriteAsync(body.AsMemory(0, bodyLength));

        // Returns true if the entire data was flushed successfully to the kernel buffer.
        // Returns false if all or part of the data was queued in user memory
        return false;
    }

    protected bool Write(ReadOnlyMemory<byte> buffer)
    {
        // The first time response.write() is called, it will send the buffered header
        // information and the first chunk of the body to the client
        if (!HeadersSent)
        {
            if (!HasHeader("Content-Length"))
            {
                _chunked = true;
                Set("Transfer-Encoding", "chunked");
            }

            WriteHead(_httpStatusCode, _httpStatusCode.ToString());
        }

        if (_chunked)
            Write($"{buffer.Length:X}\r\n");

        _stream.WriteAsync(buffer);

        if (_chunked)
            _stream.WriteAsync(CrLf);

        // Returns true if the entire data was flushed successfully to the kernel buffer.
        // Returns false if all or part of the data was queued in user memory
        return false;
    }

    /// <summary>
    /// Sends a response header to the request. The Status code is a 3-digit HTTP Status code,
    /// like 404. The last argument, headers, are the response headers. Optionally one can give
    /// a human-readable statusMessage as the second argument.
    /// </summary>
    private void WriteHead()
    {
        WriteHead(_httpStatusCode, _httpStatusCode.ToString());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="statusCode"></param>
    public virtual void WriteHead(HttpStatusCode statusCode)
    {
        WriteHead(statusCode, statusCode.ToString());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="statusMessage"></param>
    /// <param name="headers"></param>
    public virtual void WriteHead(HttpStatusCode statusCode, string statusMessage, Dictionary<string, string>? headers = null)
    {
        if (null != headers)
            foreach (var hdr in headers)
                Set(hdr.Key, hdr.Value);

        RaiseWriteHeaders(_headers);

        if (string.IsNullOrEmpty(statusMessage))
            statusMessage = _httpStatusCode.ToString();

        var headerContent = new StringBuilder();

        // First line of HTTP
        headerContent.AppendLine($"HTTP/1.1 {(int)statusCode} {Regex.Replace(statusMessage, "(?<=[a-z])([A-Z])", " $1", RegexOptions.Compiled)}");

        // construct/append headers
        if (_sendDate)
            if (!HasHeader("Date"))
                Set("Date", DateTime.Now.ToUniversalTime().ToString("r"));

        #region Stringify headers

        foreach (var header2 in _headers)
            headerContent.AppendLine($"{header2.Key}: {header2.Value}");

        // last header line is empty
        headerContent.AppendLine();

        #endregion

        #region Write StringBuilder

        // Prepare to send it out
        var headerString = headerContent.ToString();
        var header = Encoding.UTF8.GetBytes(headerString);
        var headerLength = Encoding.UTF8.GetByteCount(headerString);

        // write out the headers in 1 write
        _stream.WriteAsync(header.AsMemory(0, headerLength));
        _stream.FlushAsync();

        #endregion

        HeadersSent = true;
    }

    /// <summary>
    /// This method signals to the server that all of the response headers and body have been sent;
    /// that server should consider this message complete. The method, response.end(),
    /// MUST be called on each response.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="encoding"></param>
    public void End(string data, Encoding? encoding = null)
    {
        Write(data, encoding ?? Encoding.UTF8);

        End();
    }

    /// <summary>
    /// This method signals to the server that all of the response headers and body have been sent;
    /// that server should consider this message complete. The method, response.end(),
    /// MUST be called on each response.
    /// </summary>
    public void End()
    {
        if (_chunked)
            Write($"0\r\n\r\n");

        if (!HeadersSent)
            WriteHead();

        _stream.FlushAsync();
    }

}
