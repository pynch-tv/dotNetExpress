﻿using System.Net;
using System.Collections.Specialized;
using dotNetExpress.Options;
using dotNetExpress.Delegates;
using dotNetExpress.Exceptions;
using dotnetExpress.Middlewares.BodyParser;
using dotnetExpress.Middlewares.ServerStatic;

namespace dotNetExpress;

public class Express : IDisposable
{
    #region Properties

    /// <summary>
    /// The app.locals object has properties that are local variables within the application,
    /// and will be available in templates rendered with res.render.
    /// </summary>
    public NameValueCollection locals => _locals;

    /// <summary>
    /// The app.MountPath property contains one or more path patterns on which a sub-app was mounted.
    /// </summary>
    public string? MountPath;

    /// <summary>
    /// The application’s in-built instance of router. This is created lazily, on first access.
    /// </summary>
    public Router? router => _router;

    /// <summary>
    /// 
    /// </summary>
    private readonly Router _router = new();

    /// <summary>
    /// 
    /// </summary>
    internal readonly Dictionary<string, RenderEngineCallback> _engines = [];

    /// <summary>
    /// 
    /// </summary>
    private readonly NameValueCollection _settings = [];

    /// <summary>
    /// 
    /// </summary>
    private readonly NameValueCollection _locals = [];

    /// <summary>
    /// 
    /// </summary>
    internal Server? Listener;

    /// <summary>
    /// 
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// KeepALive allows HTTP clients to re-use connections for multiple requests, and relies on timeout configurations
    /// on both the client and target server to decide when to close open TCP sockets.
    ///
    /// There is overhead in establishing a new TCP connection (DNS lookups, TCP handshake, SSL/TLS handshake, etc).
    /// Without a keep-alive, every HTTP request has to establish a new TCP connection, and then close the connection
    /// once the response has been sent/received. A keep-alive allows an existing TCP connection to be re-used for
    /// multiple requests/responses, thus avoiding all of that overhead. That is what makes the connection "persistent".
    /// </summary>
    //public bool KeepAlive = true;

    /// <summary>
    /// An integer that is the time in seconds that the host will allow an idle connection to remain open before it is closed.
    /// A connection is idle if no data is sent or received by a host. A host may keep an idle connection open for longer than
    /// timeout seconds, but the host should attempt to retain a connection for at least timeout seconds.
    /// </summary>
    public int KeepAliveTimeout = 2;

    /// <summary>
    /// 
    /// </summary>
    public int MaxConcurrentListeners = 10;

    #endregion

    #region Constructor & Destructor

    /// <summary>
    /// Constructor
    /// </summary>
    public Express()
    {
        // Enable case sensitivity. When enabled, "/Foo" and "/foo" are different routes.
        // When disabled, "/Foo" and "/foo" are treated the same.
        Set("case sensitive routing", "");

        // Environment mode. Be sure to set to "production" in a production environment;
        // see Production best practices: performance and reliability.	
#if DEBUG
        Set("env", "development");
#else
        Set("env", "production");
#endif

        // Set the ETag response header. For possible values, see the etag options table.
        Set("etag", "weak");

        // Specifies the default JSONP callback name.	
        Set("jsonp callback name", "callback");

        // Enable escaping JSON responses from the res.json, res.jsonp, and res.send APIs.
        // This will escape the characters <, >, and & as Unicode escape sequences in JSON.
        // The purpose of this it to assist with mitigating certain types of persistent XSS
        // attacks when clients sniff responses for HTML.
        Set("json escape", "");

        // The 'replacer' argument used by `JSON.stringify`.
        Set("json replacer", "");

        // The 'space' argument used by `JSON.stringify`. This is typically set to the
        // number of spaces to use to indent prettified JSON.
        Set("json spaces", "");

        // Disable query parsing by setting the value to false, or set the query parser to use
        // either “simple” or “extended” or a custom query string parsing function.
        Set("query parser", "extended");

        // Enable strict routing. When enabled, the router treats "/foo" and "/foo/" as different.
        // Otherwise, the router treats "/foo" and "/foo/" as the same.
        Set("strict routing", "");

        // The number of dot-separated parts of the host to remove to access subdomain.	
        Set("subdomain offset", "2");

        // Indicates the app is behind a front-facing proxy, and to use the X-Forwarded-* headers
        // to determine the connection and the IP address of the client. NOTE: X-Forwarded-* headers
        // are easily spoofed and the detected IP addresses are unreliable.
        //
        // When enabled, Express attempts to determine the IP address of the client connected through
        // the front-facing proxy, or series of proxies. The `req.ips` property, then contains an array
        // of IP addresses the client is connected through. To enable it, use the values described in
        // the trust proxy options table.
        Set("trust proxy", "false");

        // A directory or an array of directories for the application's views. If an array,
        // the views are looked up in the order they occur in the array.	
        Set("views", "");

        // Enables view template compilation caching.
        Set("view cache", "true");

        // The default engine extension to use when omitted.
        Set("view engine", "");
    }

    /// <summary>
    /// 
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            Listener.End();
        }

        _disposed = true;
    }

    #endregion

    #region Express methods

    /// <summary>
    /// 
    /// </summary>
    /// <param name="req"></param>
    /// <param name="res"></param>
    internal async Task Dispatch(Request req, Response res)
    {
        if (await _router.Dispatch(req, res))
            await res.End();
    }

    /// <summary>
    /// This is a built-in middleware function in Express. It parses incoming requests with JSON payloads and is based on body-parser.
    ///
    /// Returns middleware that only parses JSON and only looks at requests where the Content-Type header matches the type option.
    /// This parser accepts any Unicode encoding of the body and supports automatic inflation of gzip and deflate encodings.
    ///
    /// A new body object containing the parsed data is populated on the request object after the middleware(i.e.req.body),
    /// or an empty object ({}) if there was no body to parse, the Content-Type was not matched, or an error occurred.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static MiddlewareCallback Json(jsonOptions options = null)
    {
        return BodyParser.ParseJson;
    }

    /// <summary>
    /// This is a built-in middleware function in Express. It parses 
    /// incoming requests with urlencoded payloads and is based on body-parser.
    ///
    /// Returns middleware that only parses urlencoded bodies and only looks 
    /// at requests where the Content-Type header matches the type option. This
    /// parser accepts only UTF-8 encoding of the body and supports automatic 
    /// inflation of gzip and deflate encodings.
    /// 
    /// A new body object containing the parsed data is populated on the request
    /// object after the middleware(i.e.req.body), or an empty object ({}) if
    /// there was no body to parse, the Content-Type was not matched, or an
    /// error occurred.This object will contain key-value pairs, where the value
    /// can be a string or array(when extended is false), or any type(when extended is true).
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public static MiddlewareCallback UrlEncoded(urlEncodedOptions options = null)
    {
        return BodyParser.ParseUrlEncoded;
    }

    /// <summary>
    /// This is a built-in middleware function in Express. It serves static files and is based on serve-static.
    ///
    /// The root argument specifies the root directory from which to serve static assets. The function determines
    /// the file to serve by combining req.url with the provided root directory. When a file is not found, instead
    /// of sending a 404 response, it instead calls next() to move on to the next middleware, allowing for stacking
    /// and fall-backs.
    /// </summary>
    /// <param name="root"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static MiddlewareCallback Static(string root, StaticOptions? options = null)
    {
        var serveStatic = new ServeStatic(root, options);

        return serveStatic.Serve;
    }

    /// <summary>
    /// Creates a new router object.
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    internal virtual Router Router(RouterOptions? options = null) => new(options);

    /// <summary>
    /// This is a built-in middleware function in Express. It parses incoming request payloads into a Buffer and is based on body-parser.
    ///
    /// Returns middleware that parses all bodies as a Buffer and only looks at requests where the Content-Type header matches the type
    /// option.This parser accepts any Unicode encoding of the body and supports automatic inflation of gzip and deflate encodings.
    ///
    /// A new body Buffer containing the parsed data is populated on the request object after the middleware(i.e.req.body), or an
    /// empty object ({}) if there was no body to parse, the Content-Type was not matched, or an error occurred.
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public static MiddlewareCallback Raw(jsonOptions? options = null)
    {
        return BodyParser.ParseRaw;
    }

    /// <summary>
    /// This is a built-in middleware function in Express. It parses incoming requests with urlencoded payloads and is based on body-parser.
    ///
    /// Returns middleware that only parses urlencoded bodies and only looks at requests where the Content-Type header matches the type
    /// option.This parser accepts only UTF-8 encoding of the body and supports automatic inflation of gzip and deflate encodings.
    ///
    /// A new body object containing the parsed data is populated on the request object after the middleware(i.e.req.body), or an
    /// empty object ({}) if there was no body to parse, the Content-Type was not matched, or an error occurred.This object will contain
    /// key-value pairs, where the value can be a string or array(when extended is false), or any type(when extended is true).
    /// </summary>
    /// <returns></returns>
    public static MiddlewareCallback Urlencoded()
    {
        return BodyParser.ParseUrlEncoded;
    }

    #endregion

    #region Application Methods

    /// <summary>
    /// This method is like the standard app.METHOD() methods, except it matches all HTTP verbs.
    /// </summary>
    public void All(string path, params MiddlewareCallback[] args)
    {
        _router.All(path, args);
    }

    /// <summary>
    /// Routes HTTP DELETE requests to the specified path with the specified callback functions. For more information, see the routing guide.
    /// </summary>
    /// <param name="path"></param>
    public void Delete(string path, params MiddlewareCallback[] args)
    {
        _router.Delete(path, args);
    }

    /// <summary>
    /// Sets the Boolean setting name to false, where name is one of the properties from
    /// the app settings table. Calling app.set('foo', false) for a Boolean property
    /// is the same as calling app.disable('foo').
    /// </summary>
    /// <param name="key"></param>
    public void Disable(string key) => _settings[key] = "false";

    /// <summary>
    /// Returns true if the Boolean setting name is disabled (false), where name is one of
    /// the properties from the app settings table.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool Disabled(string key) => (_settings[key]!.Equals("false", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Sets the Boolean setting name to true, where name is one of the properties from
    /// the app settings table. Calling app.set('foo', true) for a Boolean property is
    /// the same as calling app.enable('foo').
    /// </summary>
    /// <param name="key"></param>
    public void Enable(string key) => _settings[key] = "true";

    /// <summary>
    /// Returns true if the setting name is enabled (true), where name is one of the
    /// properties from the app settings table.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool Enabled(string key) => (_settings[key]!.Equals("true", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ext"></param>
    /// <param name="engine"></param>
    public void Engine(string ext, RenderEngineCallback engine)
    {
        _engines[ext] = engine;
    }

    /// <summary>
    /// Returns the value of name app setting, where name is one of the strings in the
    /// app settings table. 
    /// </summary>
    /// <param name="key"></param>
    public string? Get(string key) => _settings[key];

    /// <summary>
    /// Routes HTTP GET requests to the specified path with the specified callback functions.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="args"></param>
    public void Get(string path, params MiddlewareCallback[] args)
    {
        _router.Get(path, args);
    }

    /// <summary>
    /// Binds and listens for connections on the specified host and port. This method is identical to Node’s http.Server.listen().
    /// 
    /// If port is omitted or is 0, the operating system will assign an arbitrary unused port, 
    /// which is useful for cases like automated tasks
    ///
    /// </summary>
    /// <param name="port"></param>
    /// <param name="callback"></param>
    public async Task<Server> Listen(int port, ListenCallback? callback = null)
    {
        return await Listen(port, string.Empty, 20, callback);
    }

    /// <summary>
    /// Binds and listens for connections on the specified host and port. This method is identical to Node’s http.Server.listen().
    /// 
    /// If port is omitted or is 0, the operating system will assign an arbitrary unused port, 
    /// which is useful for cases like automated tasks
    /// 
    /// backLog: It specifies the max length of the queue of pending connections. You can specify the backlog if and only if you
    /// have already specified the port and host.
    ///
    /// </summary>
    /// <param name="port"></param>
    /// <param name="host"></param>
    /// <param name="backLog"></param>
    /// <param name="callback"></param>
    public async Task<Server> Listen(int port, string host = "", int backLog = 20, ListenCallback? callback = null)
    {
        IPAddress ipAddress = IPAddress.Any;
        if (!string.IsNullOrEmpty(host))
            ipAddress = IPAddress.Parse(host);

        //if (port == 0)
        //    port = 0;

        Listener = new Server(ipAddress, port);

        Listener.HandleConnection += async (sender, tcpClient) =>
        {
            Client client = new();
            await client.Connection(this, tcpClient);

            var connected = tcpClient.Connected ? "left open" : "closed";
//            Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] ({DateTime.Now:HH.mm.ss:ffff}) Connection handled, socket is {connected}");
        };

        await Listener.Begin(this);

        callback?.Invoke();

        return Listener;
    }

    ///// <summary>
    ///// Routes an HTTP request, where METHOD is the HTTP method of the request, such as GET, PUT, POST,
    ///// and so on. Thus, the actual methods are app.get(), app.post(), app.put(), and so on.
    ///// See Routing methods below for the complete list.
    ///// </summary>
    ///// <param name="method"></param>
    ///// <param name="path"></param>
    ///// <param name="middlewares"></param>
    //private void METHOD(HttpMethod method, string path, List<MiddlewareCallback> middlewares)
    //{
    //    throw new NotImplementedException();
    //}

    /// <summary>
    /// Add callback triggers to route parameters, where name is the name of the parameter or an array
    /// of them, and callback is the callback function. The parameters of the callback function are the
    /// request object, the response object, the next middleware, the value of the parameter and the name
    /// of the parameter, in that order.
    ///
    /// If name is an array, the callback trigger is registered for each parameter declared in it, in
    /// the order in which they are declared. Furthermore, for each declared parameter except the last
    /// one, a call to next inside the callback will call the callback for the next declared parameter.
    /// For the last parameter, a call to next will call the next middleware in place for the route currently
    /// being processed, just like it would if name were just a string.
    ///
    /// For example, when :user is present in a route path, you may map user loading logic to automatically
    /// provide req.user to the route, or perform validations on the parameter input.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    public void Param(string name, MiddlewareCallback callback)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="args"></param>
    public void Patch(string path, params MiddlewareCallback[] args)
    {
        _router.Patch(path, args);
    }

    /// <summary>
    /// Returns the canonical path of the app, a string.
    ///
    /// The behavior of this method can become very complicated in complex cases of mounted apps: it is
    /// usually better to use req.baseUrl to get the canonical path of the app.
    /// </summary>
    /// <returns></returns>
    public string Path()
    {
        return _router.MountPath;
    }

    /// <summary>
    /// Routes HTTP POST requests to the specified path with the specified callback functions.
    /// For more information, see the routing guide.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="args"></param>
    public void Post(string path, params MiddlewareCallback[] args)
    {
        _router.Post(path, args);
    }

    /// <summary>
    /// Routes HTTP PUT requests to the specified path with the specified callback functions.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="args"></param>
    public void Put(string path, params MiddlewareCallback[] args)
    {
        _router.Put(path, args);
    }

    /// <summary>
    /// Returns the rendered HTML of a view via the callback function. It accepts an optional parameter
    /// that is an object containing local variables for the view. It is like res.render(), except it
    /// cannot send the rendered view to the client on its own.
    /// </summary>
    /// <exception cref="KeyNotFoundException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public string Render(string view, dynamic? locals = null)
    {
        var viewEngineName = Get("view engine");
        if (null == viewEngineName)
            throw new ArgumentNullException("view engine not Set");
        var viewEngine = _engines[viewEngineName];
        if (null == viewEngine)
            throw new ExpressException(HttpStatusCode.NotFound, "Engine not found", $"engine {viewEngine} not found");

        return viewEngine(view, locals);
    }

    /// <summary>
    /// Returns an instance of a single route, which you can then use to handle HTTP verbs with
    /// optional middleware. Use app.route() to avoid duplicate route names (and thus typo errors).
    /// </summary>
    /// <param name="path"></param>
    public Route Route(string path)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Assigns setting name to value. You may store any value that you want, but certain
    /// names can be used to configure the behavior of the server. These special names are
    /// listed in the app settings table.
    ///
    /// Calling app.set('foo', true) for a Boolean property is the same as calling app.enable('foo').
    /// Similarly, calling app.set('foo', false) for a Boolean property is the same as calling
    /// app.disable('foo').
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void Set(string key, string value) => _settings[key] = value;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="router"></param>
    public void Use(string path, Router router)
    {
        _router.Use(path, router);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="callback"></param>
    public void Use(string path, MiddlewareCallback callback)
    {
        var router = new Router();
        router.Use(callback);

        _router.Use(path, router);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="callback"></param>
    public void Use(MiddlewareCallback callback)
    {
        _router.Use(callback);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="callback"></param>
    public void Use(ErrorCallback callback)
    {
        _router.Use(callback);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="callback"></param>
    public void Use(MiddlewareCallback[] callback)
    {
        _router.Use(callback);
    }

    #endregion

}
