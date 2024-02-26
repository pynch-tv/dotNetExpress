using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using dotNetExpress.Lookup;
using dotNetExpress.Options;

namespace dotNetExpress;

public class Response
{
    public HttpMethod HttpMethod;

    private readonly NameValueCollection _headers = new();

    private readonly NameValueCollection _locals = new();

    private HttpStatusCode _httpStatusCode = HttpStatusCode.OK;

    private readonly Stream _stream;

    private readonly Express _app;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="app"></param>
    /// <param name="stream"></param>
    internal Response(Express app, Stream stream)
    {
        _app = app;
        HeadersSent = false;
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
    public bool HeadersSent;

    /// <summary>
    /// Use this property to set variables accessible in templates rendered with res.render.
    /// The variables set on res.locals are available within a single request-response cycle,
    /// and will not be shared between requests.
    /// </summary>
    public Dictionary<string, dynamic> Locals = new();

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
        var ext = Path.GetExtension(filename);
        var type = Mime.GetType(ext);

        Set("Content-Disposition", $"attachment; filename=\"{filename}\"");
        Type(type);
    }

    /// <summary>
    /// Sets cookie name to value. The value parameter may be a string or object converted to JSON.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <exception cref="NotImplementedException"></exception>
    /// <exception cref="NotImplementedException"></exception>Set
    public void Cookie(string name, string value, CookieOptions options = null)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Clears the cookie specified by name. For details about the options object, see res.cookie().
    /// </summary>
    public void ClearCookie(string name, CookieOptions options = null)
    {
        options ??= new CookieOptions();

        throw new NotImplementedException();
    }

    /// <summary>
    ///  Transfers the file at path as an “attachment”. Typically, browsers will prompt
    /// the user for download. By default, the Content-Disposition header “filename=”
    /// parameter is derived from the path argument, but can be overridden with the
    /// filename parameter. If path is relative, then it will be based on the current
    /// working directory of the process or the root option, if provided.
    ///
    /// TODO: The method invokes the callback function fn(err) when the transfer is complete
    /// or when an error occurs. If the callback function is specified and an error
    /// occurs, the callback function must explicitly handle the response process either
    /// by ending the request-response cycle, or by passing control to the next route.
    /// </summary>
    public void Download(string path, string filename = null, DownloadOptions options = null) // todo: error callback
    {
        options ??= new DownloadOptions();
        filename ??= Path.GetFileName(path);
        var ext = Path.GetExtension(path);
        var type = Mime.GetType(ext);

        // set Content-Disposition when file is sent
        var headers = new NameValueCollection();
        headers["Content-Disposition"] = $"attachment; filename=\"{filename}\"";
        headers["Content-Type"] = type;

        // merge user-provided headers
        options.Headers.Add(headers);

        var opts = SendFileOptions.From(options);

        this.SendFile(path, opts);
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
    public string Get(string field)
    {
        return _headers[field];
    }

    /// <summary>
    /// Sends a JSON response. This method sends a response (with the correct content-type)
    /// that is the parameter converted to a JSON string.
    ///
    /// The parameter can be any JSON type, including object, array, string, Boolean, number,
    /// or null, and you can also use it to convert other values to JSON.
    /// </summary>
    /// <param name="body"></param>
    public void Json(dynamic body)
    {
        var jsonString = JsonSerializer.Serialize(body);

        Type("application/json");

        Send(jsonString);
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
    public void Links(IEnumerable<dynamic> links)
    {
        // Get some info from the dynamic object to construct the link
        foreach (var link in links)
        {
            var uriProperty = string.Empty;
            var linkDictionary = Dynamic.ToDictionary(link);
            foreach (KeyValuePair<string, dynamic> pair in linkDictionary)
            {
                if (pair.Value.GetType() == typeof(Uri))
                {
                    uriProperty = pair.Key;
                    break;
                }

                if (pair.Value.GetType() != typeof(string)) continue;
                if (!Uri.IsWellFormedUriString(pair.Value, UriKind.Absolute)) continue;

                uriProperty = pair.Key;
                break;
            }

            if (string.IsNullOrEmpty(uriProperty)) return;

            var value = $"<{linkDictionary[uriProperty]}>";

            foreach (KeyValuePair<string, dynamic> pair in linkDictionary)
            {
                if (pair.Key == uriProperty) continue;
                value += $"; {pair.Key}=\"{pair.Value}\"";
            }

            // Set of append the Link header
            this.Set("Link", value);
        }
    }

    /// <summary>
    /// Sets the response Location HTTP header to the specified path parameter.
    ///
    /// A path value of “back” has a special meaning, it refers to the URL specified
    /// in the Referer header of the request. If the Referer header was not specified, it refers to “/”.
    /// </summary>
    public void Location(string path)
    {
        if (path.Equals("Back"))
        {
            throw new NotSupportedException();
        }
        else
            Set("Location", path);
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
    public void Render(string view, Dictionary<string, dynamic> locals)
    {
        var html = _app.Render(view, locals);
        Status(HttpStatusCode.OK).Send(html);
    }

    /// <summary>
    /// Sends the HTTP response.
    ///
    /// The body parameter can be a Buffer object, a String, an object, Boolean, or an Array.
    ///
    /// This method performs many useful tasks for simple non-streaming responses: For example,
    /// it automatically assigns the Content-Length HTTP response header field (unless previously
    /// defined) and provides automatic HEAD and HTTP cache freshness support.
    /// </summary>
    /// <param name="body"></param>
    /// <returns></returns>
    public void Send(string body = null)
    {
        End(body);
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
    /// 
    /// TODO: The method invokes the callback function fn(err) when the transfer is complete or
    /// when an error occurs. If the callback function is specified and an error occurs,
    /// the callback function must explicitly handle the response process either by ending
    /// the request-response cycle, or by passing control to the next route.
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="options"></param>
    public void SendFile(string filename, SendFileOptions options = null) // TODO callback
    {
        options ??= new SendFileOptions();

        if (options.DotFiles.Equals("deny", StringComparison.OrdinalIgnoreCase) && filename.StartsWith("."))
        {
            // TODO call error handler
        }

        var path = Path.Combine(options.Root, filename);
        if (!File.Exists(path))
        {
            // TODO call error handler
        }

        var file = new FileInfo(path);

        // Headers
        _headers.Add(options.Headers);
        if (options.LastModified)
            _headers.Add(new NameValueCollection() { { "Last-Modified", file.LastWriteTime.ToUniversalTime().ToString("r") } });

        var encoding = Encoding.UTF8;

        var headerContent = new StringBuilder();

        // First line of HTTP
        headerContent.AppendLine($"HTTP/1.1 {(int)_httpStatusCode} {Regex.Replace(_httpStatusCode.ToString(), "(?<=[a-z])([A-Z])", " $1", RegexOptions.Compiled)}");

        // construct/append headers
        if (_app.Get("x-powered-by")!.Equals("true", StringComparison.OrdinalIgnoreCase))
            Set("X-Powered-By", "dotNetExpress");
        Set("Date", DateTime.Now.ToUniversalTime().ToString("r"));
        if (_app.Listener.KeepAlive)
        {
            Set("Connection", "keep-alive");
            Set("Keep-Alive", $"timeout={_app.Listener.KeepAliveTimeout}"); // Keep-Alive is in seconds
        }
        else
            Set("Connection", "close");
        Set("Content-Length", file.Length.ToString());

        // stringy headers
        foreach (string key in _headers)
            headerContent.AppendLine($"{key}: {_headers[key]}");
        // last header line is empty
        headerContent.AppendLine();

        // Prepare to send it out
        var headerString = headerContent.ToString();
        var header = encoding.GetBytes(headerString);
        var headerLength = encoding.GetByteCount(headerString);

        // send headers
        _stream.Write(header, 0, headerLength);
        HeadersSent = true;

        // send content (if any)
        if (file.Length <= 0) return;

        var fileStream = File.OpenRead(path);
        fileStream.CopyTo(this._stream);
        fileStream.Close();
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

        End();
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

    public Response Status(int code)
    {
        return Status((HttpStatusCode)code);
    }

    /// <summary>
    /// Sets the response’s HTTP header field to value.
    /// </summary>
    /// <param name="field"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public void Set(string field, string value)
    {
        if (!string.IsNullOrEmpty(_headers[field]))
            _headers[field] += ", " + value;
        else
            _headers[field] += value;
    }

    public void Set(string field, int value)
    {
        Set(field, value.ToString());
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
        Set("Content-Type",  type);
    }

    /// <summary>
    /// Adds the field to the Vary response header, if it is not there already.
    /// </summary>
    public Response Vary(string field)
    {
        Set("Vary",field);

        return this;
    }

    #endregion

    #region Internal methods

    /// <summary>
    /// Ends the response process.
    ///
    /// Use to quickly end the response without any data. If you need to respond with data,
    /// instead use methods such as res.send() and res.json().
    /// </summary>
    public void End(string data = null, Encoding encoding = null)
    {
        encoding ??= Encoding.UTF8;
        data ??= string.Empty;

        var headerContent = new StringBuilder();

        // First line of HTTP
        headerContent.AppendLine($"HTTP/1.1 {(int)_httpStatusCode} {Regex.Replace(_httpStatusCode.ToString(), "(?<=[a-z])([A-Z])", " $1", RegexOptions.Compiled)}");

        if (_httpStatusCode != HttpStatusCode.SwitchingProtocols)
        {
            // construct/append headers
            if (_app.Get("x-powered-by")!.Equals("true", StringComparison.OrdinalIgnoreCase))
                Set("X-Powered-By", "dotNetExpress");
            Set("Date", DateTime.Now.ToUniversalTime().ToString("r"));
            if (_app.Listener.KeepAlive)
            {
                Set("Connection", "keep-alive");
                Set("Keep-Alive", $"timeout={_app.Listener.KeepAliveTimeout}"); // Keep-Alive is in seconds
            }
            else
                Set("Connection", "Close");

            Set("Content-Length", data.Length.ToString());
        }
        
        // stringy headers
        foreach (string key in _headers)
            headerContent.AppendLine($"{key}: {_headers[key]}");
        // last header line is empty
        headerContent.AppendLine();

        // Prepare to send it out
        var headerString = headerContent.ToString();
        var header = encoding.GetBytes(headerString);
        var headerLength = encoding.GetByteCount(headerString);

        // send headers
        _stream.Write(header, 0, headerLength);
        HeadersSent = true;

        // send content (if any)
        if (data.Length <= 0) return;

        var body = encoding.GetBytes(data);
        var bodyLength = encoding.GetByteCount(data);
        _stream.Write(body, 0, bodyLength);
    }

    #endregion
}