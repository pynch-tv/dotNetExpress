using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using dotNetExpress.Delegates;
using dotNetExpress.Options;

namespace dotNetExpress;

public class Response
{
    public HttpMethod HttpMethod;

    private readonly NameValueCollection _headers = new();

    private readonly NameValueCollection _locals = new();

    private HttpStatusCode _httpStatusCode = HttpStatusCode.OK;

    private readonly Stream _stream;

    private string _body = string.Empty;

    private bool _headersSent = false;

    private readonly Express _app;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="app"></param>
    /// <param name="stream"></param>
    internal Response(Express app, Stream stream)
    {
        _app = app;
        _headersSent = false;
        _stream = stream;
    }

    #region Properties

    /// <summary>
    /// This property holds a reference to the instance of the Express application that is using the middleware.
    /// res.app is identical to the req.app property in the request object.
    /// </summary>
    /// <returns></returns>
    public Express App => _app;


    /// <summary>
    /// Boolean property that indicates if the app sent HTTP headers for the response.
    /// </summary>
    /// <returns></returns>
    public bool HeadersSent() => _headersSent;

    /// <summary>
    /// Use this property to set variables accessible in templates rendered with res.render.
    /// The variables set on res.locals are available within a single request-response cycle,
    /// and will not be shared between requests.
    /// </summary>
    public NameValueCollection Locals = new();

    #endregion

    #region Methods

    /// <summary>
    /// Appends the specified value to the HTTP response header field. If the header is not already set,
    /// it creates the header with the specified value. The value parameter can be a string or an array.
    ///
    /// Note: calling res.set() after res.append() will reset the previously-set header value.
    /// </summary>
    /// <returns></returns>
    public void Append(string field, string value)
    {
        _headers[field] += value;
    }

    /// <summary>
    /// Sets the HTTP response Content-Disposition header field to “attachment”. If a filename is given,
    /// then it sets the Content-Type based on the extension name via res.type(), and sets the
    /// Content-Disposition “filename=” parameter.
    /// </summary>
    /// <param name="filename"></param>
    public void Attachment(string filename)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Sets cookie name to value. The value parameter may be a string or object converted to JSON.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void Cookie(string name, string value, CookieOptions options = null)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Clears the cookie specified by name. For details about the options object, see res.cookie().
    /// </summary>
    public void ClearCookie(string name, CookieOptions options = null)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///  Transfers the file at path as an “attachment”. Typically, browsers will prompt
    /// the user for download. By default, the Content-Disposition header “filename=”
    /// parameter is derived from the path argument, but can be overridden with the
    /// filename parameter. If path is relative, then it will be based on the current
    /// working directory of the process or the root option, if provided.
    /// </summary>
    public void Download(string path, string filename = "", string options = "", NextCallback err = null)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Ends the response process. This method actually comes from Node core, specifically
    /// the response.end() method of http.ServerResponse.
    /// </summary>
    public void End(string data = "", string encoding = "")
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Performs content-negotiation on the Accept HTTP header on the request object, when present.
    /// It uses req.accepts() to select a handler for the request, based on the acceptable types
    /// ordered by their quality values. If the header is not specified, the first callback is
    /// invoked. When no match is found, the server responds with 406 “Not Acceptable”,
    /// or invokes the default callback.
    ///
    /// The Content-Type response header is set when a callback is selected. However, you may
    /// alter this within the callback using methods such as res.set() or res.type().
    /// </summary>
    public void Format(object obj)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Returns the HTTP response header specified by field. The match is case-insensitive.
    /// </summary>
    /// <param name="field"></param>
    /// <returns></returns>
    public string? Get(string field)
    {
        return _headers[field];
    }

    /// <summary>
    /// Sends a JSON response. This method sends a response (with the correct content-type)
    /// that is the parameter converted to a JSON string using JSON.stringify().
    ///
    /// The parameter can be any JSON type, including object, array, string, Boolean, number,
    /// or null, and you can also use it to convert other values to JSON.
    /// </summary>
    /// <param name="body"></param>
    public void Json(dynamic body)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Sends a JSON response with JSONP support. This method is identical to res.json(),
    /// except that it opts-in to JSONP callback support.
    /// </summary>
    /// <param name="body"></param>
    /// <exception cref="NotSupportedException"></exception>
    public void Jsonp(dynamic body)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Joins the links provided as properties of the parameter to populate the response’s
    /// Link HTTP header field.
    /// </summary>
    public void Links(object links)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Sets the response Location HTTP header to the specified path parameter.
    ///
    /// A path value of “back” has a special meaning, it refers to the URL specified
    /// in the Referer header of the request. If the Referer header was not specified, it refers to “/”.
    /// </summary>
    public void Location(string path)
    {
        if (path.Equals("back"))
        {
            throw new NotSupportedException();
        }
        else
            _headers["location"] = path;
    }

    /// <summary>
    /// Redirects to the URL derived from the specified path, with specified status,
    /// a positive integer that corresponds to an HTTP status code . If not specified,
    /// status defaults to “302 “Found”.
    /// </summary>
    /// <param name="path"></param>
    public void Redirect(string path)
    {
        Redirect(HttpStatusCode.Found, path);
    }

    public void Redirect(HttpStatusCode code, string path)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Renders a view and sends the rendered HTML string to the client.
    /// Optional parameters:
    /// </summary>
    /// <param name="view"></param>
    /// <param name="locals"></param>
    public void Render(string view, NameValueCollection locals)
    {
        _app.Render(view, null);
    }

    /// <summary>
    /// Sends the HTTP response.
    ///
    /// The body parameter can be a Buffer object, a String, an object, Boolean, or an Array.For example:
    /// </summary>
    /// <param name="body"></param>
    /// <returns></returns>
    public void Send(string body = "")
    {
        _body = body;
    }

    public void Send(object body)
    {
        throw new NotSupportedException();
    }

    public void Send(bool body)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Transfers the file at the given path. Sets the Content-Type response HTTP header
    /// field based on the filename’s extension. Unless the root option is set in the options
    /// object, path must be an absolute path to the file.
    /// </summary>
    /// <param name="path"></param>
    /// <exception cref="NotSupportedException"></exception>
    public void SendFile(string path)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Sets the response HTTP status code to statusCode and sends the registered status
    /// message as the text response body. If an unknown status code is specified,
    /// the response body will just be the code number.
    /// </summary>
    /// <param name="code"></param>
    /// <exception cref="NotSupportedException"></exception>
    public void SendStatus(HttpStatusCode code)
    {
        _httpStatusCode = code;

        throw new NotSupportedException();
    }

    /// <summary>
    /// Sets the HTTP status for the response. It is a chainable alias
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    public Response Status(HttpStatusCode code)
    {
        _httpStatusCode = code;

        return this;
    }

    /// <summary>
    /// Sets the response’s HTTP header field to value. To set multiple fields at once, pass an object as the parameter.
    /// </summary>
    /// <param name="field"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public void Set(string field, string value)
    {
        _headers[field] = value;
    }

    /// <summary>
    /// Sets the Content-Type HTTP header to the MIME type as determined by the specified type.
    /// If type contains the “/” character, then it sets the Content-Type to the exact value of type,
    /// otherwise it is assumed to be a file extension and the MIME type is looked up in a mapping
    /// using the express.static.mime.lookup() method.
    /// </summary>
    /// <param name="type"></param>
    public void Type(string type)
    {
        _headers["Content-Type"] = type;
    }

    /// <summary>
    /// Adds the field to the Vary response header, if it is not there already.
    /// </summary>
    public Response Vary(string field)
    {
        _headers["Vary"] = field;

        return this;
    }

    #endregion

    #region Internal methods
    /// <summary>
    /// 
    /// </summary>
    internal void _send()
    {
        // _client must still be alive!
        
        // finalize header content, based on content
        if (!string.IsNullOrEmpty(_body))
            _headers["content-length"] = _body.Length.ToString();
        _headers["connection"] = "close";

        // Build string in memory and write out once
        var headerContent = new StringBuilder();
        headerContent.AppendLine($"HTTP/1.1 {(int)_httpStatusCode} {Regex.Replace(_httpStatusCode.ToString(), "(?<=[a-z])([A-Z])", " $1", RegexOptions.Compiled)}");
        if (!string.IsNullOrEmpty(_body))
            _headers["content-length"] = _body.Length.ToString();

        foreach (string key in _headers)
            headerContent.AppendLine($"{key}: {_headers[key]}");
        headerContent.AppendLine();

        // Prepare to send it out
        var headerString = headerContent.ToString();
        var header = Encoding.UTF8.GetBytes(headerString);
        var headerLength = Encoding.UTF8.GetByteCount(headerString);

        var body = Encoding.UTF8.GetBytes(_body);
        var bodyLength = Encoding.UTF8.GetByteCount(_body);

        {
            _stream.Write(header, 0, headerLength);
            _headersSent = true;
            _stream.Write(body, 0, bodyLength);
        }
    }

    #endregion
}