using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace HTTPServer;

public class Response
{
    public HttpMethod HttpMethod;

    private readonly NameValueCollection _headers = new();

    private readonly NameValueCollection _locals = new();

    private HttpStatusCode _httpStatusCode = HttpStatusCode.OK;

    private readonly TcpClient _client;

    private string _body = string.Empty;

    private bool _headersSent = false;

    private Express _app;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="app"></param>
    /// <param name="client"></param>
    internal Response(Express app, TcpClient client)
    {
        _app = app;
        _headersSent = false;
        _client = client;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    public Response status(HttpStatusCode code)
    {
        _httpStatusCode = code;

        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    internal Express app()
    {
        return _app;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="field"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Response set(string field, string value)
    {
        _headers[field] = value;

        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="field"></param>
    /// <returns></returns>
    public string? get(string field)
    {
        return _headers[field];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bool headersSent() => _headersSent;

    /// <summary>
    /// Renders a view and sends the rendered HTML string to the client.
    /// Optional parameters:
    /// </summary>
    public void render(string view, NameValueCollection locals)
    {
        _app.render(view, null);
    }

    /// <summary>
    /// Sends the HTTP response.
    ///
    /// The body parameter.
    /// </summary>
    /// <param name="body"></param>
    /// <returns></returns>
    public Response send(string body)
    {
        _body = body;

        return this;
    }
        
    /// <summary>
    /// 
    /// </summary>
    public void send()
    {
        // _client must still be alive!
        
        // finalize headers, based on content
        if (!string.IsNullOrEmpty(_body))
            _headers["content-length"] = _body.Length.ToString();
        _headers["connection"] = "close";

        // Build string
        var headerContent = new StringBuilder();
        headerContent.AppendLine($"HTTP/1.1 {(int)_httpStatusCode} {Regex.Replace(_httpStatusCode.ToString(), "(?<=[a-z])([A-Z])", " $1", RegexOptions.Compiled)}");
        if (!string.IsNullOrEmpty(_body))
            _headers["content-length"] = _body.Length.ToString();

        foreach (string key in _headers)
            headerContent.AppendLine($"{key}: {_headers[key]}");
        headerContent.AppendLine();

        var headerString = headerContent.ToString();
        var header = Encoding.UTF8.GetBytes(headerString);
        var headerLength = Encoding.UTF8.GetByteCount(headerString);

        var body = Encoding.UTF8.GetBytes(_body);
        var bodyLength = Encoding.UTF8.GetByteCount(_body);


        var stream = _client.GetStream();
        {
            stream.Write(header, 0, headerLength);
            _headersSent = true;
            stream.Write(body, 0, bodyLength);
        }

        _client.Close();
        _client.Dispose();
    }
}