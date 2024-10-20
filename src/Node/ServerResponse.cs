using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace dotNetExpress.Options;

public class ServerResponse
{
    protected readonly Stream _stream;

    protected bool _sendDate = true;

    protected bool _chunked;

    protected HttpStatusCode _httpStatusCode = HttpStatusCode.OK;

    protected readonly NameValueCollection _headers = new();

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
    protected NameValueCollection GetHeader()
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
    protected bool Write(string buffer, Encoding encoding = null)
    {
        // The first time response.write() is called, it will send the buffered header
        // information and the first chunk of the body to the client
        if (!HeadersSent)
            WriteHead(_httpStatusCode, _httpStatusCode.ToString());

        encoding ??= Encoding.UTF8;

        var body = encoding.GetBytes(buffer);
        var bodyLength = encoding.GetByteCount(buffer);

        if (bodyLength <= 0) return true;

        _stream.Write(body, 0, bodyLength);
        _stream.Flush();

        // Returns true if the entire data was flushed successfully to the kernel buffer.
        // Returns false if all or part of the data was queued in user memory
        return true;
    }

    protected bool Write(byte[] buffer, int length)
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

            WriteHead(_httpStatusCode, _httpStatusCode.ToString());
        }

        if (_chunked)
            Write($"{length:X}\r\n");

        _stream.Write(buffer, 0, length);

        if (_chunked)
            _stream.Write(new byte[] {0xd, 0xa}, 0, 2);

      //  _stream.Flush();

        // Returns true if the entire data was flushed successfully to the kernel buffer.
        // Returns false if all or part of the data was queued in user memory
        return true;
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

    public virtual void WriteHead(HttpStatusCode statusCode)
    {
        WriteHead(statusCode, statusCode.ToString());
    }

    public virtual void WriteHead(HttpStatusCode statusCode, NameValueCollection headers = null)
    {
        WriteHead(statusCode, statusCode.ToString(), headers);
    }

    public virtual void WriteHead(HttpStatusCode statusCode, string statusMessage, NameValueCollection headers = null)
    {
        var headerContent = new StringBuilder();

        if (string.IsNullOrEmpty(statusMessage))
            statusMessage = _httpStatusCode.ToString();

        // First line of HTTP
        headerContent.AppendLine($"HTTP/1.1 {(int)statusCode} {Regex.Replace(statusMessage, "(?<=[a-z])([A-Z])", " $1", RegexOptions.Compiled)}");

        // construct/append headers
        if (_sendDate)
            SetHeader("Date", DateTime.Now.ToUniversalTime().ToString("r"));

        #region stringify headers

        foreach (string key in _headers)
            headerContent.AppendLine($"{key}: {_headers[key]}");

        if (null != headers)
            foreach (string key in headers)
                headerContent.AppendLine($"{key}: {_headers[key]}");

        // last header line is empty
        headerContent.AppendLine();

        #endregion

        // Prepare to send it out
        var headerString = headerContent.ToString();
        var header = Encoding.UTF8.GetBytes(headerString);
        var headerLength = Encoding.UTF8.GetByteCount(headerString);

        // write out the headers in 1 write
        _stream.Write(header, 0, headerLength);
 //       _stream.Flush();

        HeadersSent = true;
    }

    /// <summary>
    /// This method signals to the server that all of the response headers and body have been sent;
    /// that server should consider this message complete. The method, response.end(),
    /// MUST be called on each response.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="encoding"></param>
    public void End(string data, Encoding encoding = null)
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
    }

}
