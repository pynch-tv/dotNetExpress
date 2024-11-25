using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using dotNetExpress.Lookup;
using dotNetExpress.Options;

namespace dotNetExpress;

public class Response : ServerResponse
{
    public HttpMethod HttpMethod;

    private readonly NameValueCollection _locals = [];

    /// <summary>
    /// 
    /// </summary>
    /// <param name="app"></param>
    /// <param name="stream"></param>
    internal Response(Express app, Stream stream) : base(stream)
    {
        App = app;
        HeadersSent = false;
    }

    #region Properties

    /// <summary>
    /// This property holds a reference to the instance of the Express application that is using the middleware.
    /// res.app is identical to the req.app property in the request object.
    /// </summary>
    /// <returns></returns>
    public Express App { get; }

    /// <summary>
    /// Use this property to set variables accessible in templates rendered with res.render.
    /// The variables set on res.locals are available within a single request-response cycle,
    /// and will not be shared between requests.
    /// </summary>
    public Dictionary<string, dynamic> Locals = [];

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
    /// Transfers the file at path as an “attachment”. Typically, browsers will prompt
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
    public async Task Download(string path, string filename = null, DownloadOptions options = null) // todo: error callback
    {
        options ??= new DownloadOptions();
        filename ??= Path.GetFileName(path);
        var ext = Path.GetExtension(path);
        var type = Mime.GetType(ext);

        // set Content-Disposition when file is sent
        Attachment(path);

        throw new NotImplementedException();
        /*
                const int bufferSize = 16 * 1024; // 16KB buffer size
                var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                try
                {
                    int bytesRead;
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        this.Write(buffer, bytesRead);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
        */
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

    #region Json Serialization helpers
    class IPAddressConverter : JsonConverter<IPAddress>
    {
        public override IPAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return IPAddress.TryParse(reader.GetString(), out var ip) ? ip : null;
        }
        public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    class TimeSpanConverter : JsonConverter<System.TimeSpan>
    {
        public override System.TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return System.TimeSpan.TryParse(reader.GetString(), out var ts) ? ts : System.TimeSpan.Zero;
        }
        public override void Write(Utf8JsonWriter writer, System.TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    #endregion

    /// <summary>
    /// Sends a JSON response. This method sends a response (with the correct content-type)
    /// that is the parameter converted to a JSON string.
    ///
    /// The parameter can be any JSON type, including object, array, string, Boolean, number,
    /// or null, and you can also use it to convert other values to JSON.
    /// </summary>
    /// <param name="body"></param>
    public async Task Json(dynamic body)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new IPAddressConverter(), new TimeSpanConverter() }
        };

        var jsonString = JsonSerializer.Serialize(body, options);

        Type("application/json");

        await Send(jsonString);
    }

    /// <summary>
    /// Sends a JSON response with JSONP support. This method is identical to res.json(),
    /// except that it opts-in to JSONP callback support.
    /// </summary>
    /// <param name="body"></param>
    /// <exception cref="NotSupportedException"></exception>
    public async Task Jsonp(dynamic body)
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
            Set("Link", value);
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
    /// Redirects to the URL derived from the specified path, with specified Status,
    /// a positive integer that corresponds to an HTTP Status code . If not specified,
    /// Status defaults to “302 “Found”.
    /// </summary>
    /// <param name="path"></param>
    public async Task Redirect(string path)
    {
        await Redirect(HttpStatusCode.Found, path);
    }

    public async Task Redirect(HttpStatusCode code, string path)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Renders a view and sends the rendered HTML string to the client.
    /// Optional parameters:
    /// </summary>
    /// <param name="view"></param>
    /// <param name="locals"></param>
    public async Task Render(string view, dynamic locals)
    {
        var html = App.Render(view, locals);
        await Status(HttpStatusCode.OK).Send(html);
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
    public async Task Send(string body = null)
    {
        if (body == null) return;

        var bytes = Encoding.Default.GetBytes(body);

        if (!HasHeader("Content-Length"))
            Set("Content-Length", bytes.Length);

        await Send(bytes);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="body"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public async Task Send(object body)
    {
        throw new NotSupportedException();
        await End();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="body"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public async Task Send(bool body)
    {
        throw new NotSupportedException();
        await End();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="buffer"></param>
    /// <returns></returns>
    public async Task Send(byte[] buffer)
    {
        await Write(buffer, buffer.Length);

        await End();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public async Task Send(Stream stream)
    {
        await stream.CopyToAsync(this._stream);

        await End();
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
    public async Task SendFile(string filename, SendFileOptions options = null) // TODO callback
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

        await WriteHead(_httpStatusCode);

        var fileStream = File.OpenRead(path);
        await Send(fileStream);

        //fileStream.CopyTo(this._stream);
        //fileStream.Close();
    }

    /// <summary>
    /// Sets the response HTTP Status code to statusCode and sends the registered Status
    /// message as the text response body. If an unknown Status code is specified,
    /// the response body will just be the code number.
    /// </summary>
    /// <param name="code"></param>
    /// <exception cref="NotSupportedException"></exception>
    public async Task SendStatus(HttpStatusCode code)
    {
        _httpStatusCode = code;

        if (code != HttpStatusCode.NoContent)
        {
            var statusMessage = code.ToString();
            var body = Regex.Replace(statusMessage, "(?<=[a-z])([A-Z])", " $1", RegexOptions.Compiled);

            if (!HasHeader("Content-Length"))
                Set("Content-Length", body.Length);

            await Send(Encoding.Default.GetBytes(body));
        }

        await End();
    }

    /// <summary>
    /// Sets the HTTP Status for the response. It is a chainable alias
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    public Response Status(HttpStatusCode code)
    {
        _httpStatusCode = code;

        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
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
            _headers[field] = value;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="field"></param>
    /// <param name="value"></param>
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
        var parts = type.Split("/", StringSplitOptions.RemoveEmptyEntries);
        switch (parts.Length)
        {
            case 0:
                return;
            case 1:
                var ext = Path.GetExtension(parts[0]).Trim('.');
                type = MimeTypes.Lookup(ext);
                break;
            case 2:
                break;
            default:
                return;
        }

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

    #region Override methods

    /// <summary>
    /// 
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="statusMessage"></param>
    /// <param name="headers"></param>
    /// <returns></returns>
    public override async Task WriteHead(HttpStatusCode statusCode, string statusMessage = "", NameValueCollection headers = null)
    {
        if (App.Get("x-powered-by")!.Equals("true", StringComparison.OrdinalIgnoreCase))
            SetHeader("X-Powered-By", "dotNetExpress");

        if (statusCode == HttpStatusCode.SwitchingProtocols || (Get("Content-Type") != null && Get("Content-Type").Equals("text/event-stream")))
        {
        }
        else
        {
            //if (App.KeepAlive)
            //{
            //}
            //else
            //    SetHeader("Connection", "Close");
        }

        await base.WriteHead(statusCode, statusMessage, headers);
    }

    #endregion
}