using System.Collections.Specialized;
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

    protected readonly NameValueCollection _headers = [];

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
    public Socket Socket;

    /// <summary>
    /// 
    /// </summary>
    public Socket Connection { get { return Socket; } }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="field"></param>
    /// <returns></returns>
    protected string GetHeader(string field)
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
    protected void SetHeader(string field, string value)
    {
        _headers[field] = value;
    }

    /// <summary>
    /// Returns a shallow copy of the current outgoing headers. Since a shallow copy is used,
    /// array values may be mutated without additional calls to various header-related http
    /// module methods. The keys of the returned object are the header names and the values
    /// are the respective header values. All header names are lowercase.
    /// </summary>
    /// <returns>NameValueCollection</returns>
    public NameValueCollection GetHeaders()
    {
        return _headers;
    }

    /// <summary>
    /// Returns an array containing the unique names of the current outgoing headers.
    /// All header names are lowercase.
    /// </summary>
    /// <returns></returns>
    protected string[] GetHeaderNames()
    {
        return _headers.AllKeys;
    }

    /// <summary>
    /// Returns true if the header identified by name is currently set in the outgoing headers.
    /// The header name matching is case-insensitive.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    protected bool HasHeader(string name)
    {
        return _headers.Cast<string>().Any(header => header.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="encoding"></param>
    public async Task<bool> Write(string buffer, Encoding encoding = null)
    {
        // The first time response.write() is called, it will send the buffered header
        // information and the first chunk of the body to the client
        if (!HeadersSent)
            await WriteHead(_httpStatusCode, _httpStatusCode.ToString());

        encoding ??= Encoding.UTF8;

        var body = encoding.GetBytes(buffer);
        var bodyLength = encoding.GetByteCount(buffer);

        if (bodyLength <= 0) return true;

        await _stream.WriteAsync(body.AsMemory(0, bodyLength));
        await _stream.FlushAsync();

        // Returns true if the entire data was flushed successfully to the kernel buffer.
        // Returns false if all or part of the data was queued in user memory
        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    protected async Task<bool> Write(byte[] buffer, int length)
    {
        // The first time response.write() is called, it will send the buffered header
        // information and the first chunk of the body to the client
        if (!HeadersSent)
        {
            if (!HasHeader("Content-Length"))
            {
                _chunked = true;
                SetHeader("Transfer-Encoding", "chunked");
            }

            await WriteHead(_httpStatusCode, _httpStatusCode.ToString());
        }

        if (_chunked)
            await Write($"{length:X}\r\n");

        await _stream.WriteAsync(buffer, 0, length);

        if (_chunked)
            await _stream.WriteAsync(new byte[] { 0xd, 0xa }, 0, 2);

        await _stream.FlushAsync();

        // Returns true if the entire data was flushed successfully to the kernel buffer.
        // Returns false if all or part of the data was queued in user memory
        return true;
    }

    /// <summary>
    /// Sends a response header to the request. The Status code is a 3-digit HTTP Status code,
    /// like 404. The last argument, headers, are the response headers. Optionally one can give
    /// a human-readable statusMessage as the second argument.
    /// </summary>
    private async Task WriteHead()
    {
        await WriteHead(_httpStatusCode, _httpStatusCode.ToString());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="statusCode"></param>
    public virtual async Task WriteHead(HttpStatusCode statusCode)
    {
        await WriteHead(statusCode, statusCode.ToString());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="headers"></param>
    public virtual async Task WriteHead(HttpStatusCode statusCode, NameValueCollection headers = null)
    {
        await WriteHead(statusCode, statusCode.ToString(), headers);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="statusMessage"></param>
    /// <param name="headers"></param>
    public virtual async Task WriteHead(HttpStatusCode statusCode, string statusMessage, NameValueCollection headers = null)
    {
        if (null != headers)
            foreach (string key in headers)
                SetHeader(key, headers[key]);

        if (string.IsNullOrEmpty(statusMessage))
            statusMessage = _httpStatusCode.ToString();

        var headerContent = new StringBuilder();

        // First line of HTTP
        headerContent.AppendLine($"HTTP/1.1 {(int)statusCode} {Regex.Replace(statusMessage, "(?<=[a-z])([A-Z])", " $1", RegexOptions.Compiled)}");

        // construct/append headers
        if (_sendDate)
            if (!HasHeader("Date"))
                SetHeader("Date", DateTime.Now.ToUniversalTime().ToString("r"));

        #region Stringify headers

        foreach (string key in _headers)
            headerContent.AppendLine($"{key}: {_headers[key]}");

        // last header line is empty
        headerContent.AppendLine();

        #endregion

        #region Write StringBuilder

        // Prepare to send it out
        var headerString = headerContent.ToString();
        var header = Encoding.UTF8.GetBytes(headerString);
        var headerLength = Encoding.UTF8.GetByteCount(headerString);

        // write out the headers in 1 write
        await _stream.WriteAsync(header.AsMemory(0, headerLength));
        await _stream.FlushAsync();

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
    public async Task End(string data, Encoding encoding = null)
    {
        await Write(data, encoding ?? Encoding.UTF8);
        await End();
    }

    /// <summary>
    /// This method signals to the server that all of the response headers and body have been sent;
    /// that server should consider this message complete. The method, response.end(),
    /// MUST be called on each response.
    /// </summary>
    public async Task End()
    {
        if (_chunked)
            await Write($"0\r\n\r\n");

        if (!HeadersSent)
            await WriteHead();
    }

}
