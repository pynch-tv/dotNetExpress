using System.Net;
using System.Net.Sockets;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using dotNetExpress.src;

// ReSharper disable InconsistentNaming

namespace HTTPServer
{
    #region Delegates
    public delegate void NextCallback(Exception? err = null);

    public delegate void ErrorCallback(Exception err, Request request, Response response, NextCallback? next = null);

    public delegate void MiddlewareCallback(Request request, Response response, NextCallback? next = null);

    public delegate void ListenCallback();

    public delegate void RenderEngineCallback(string path);
    #endregion

    public enum HttpMethod
    {
        /// <summary>
        /// HTTP GET.
        /// </summary>
        [EnumMember(Value = "GET")]
        // ReSharper disable once InconsistentNaming
        GET,

        /// <summary>
        /// HTTP HEAD.
        /// </summary>
        [EnumMember(Value = "HEAD")]
        // ReSharper disable once InconsistentNaming
        HEAD,

        /// <summary>
        /// HTTP PUT.
        /// </summary>
        [EnumMember(Value = "PUT")]
        // ReSharper disable once InconsistentNaming
        PUT,

        /// <summary>
        /// HTTP POST.
        /// </summary>
        [EnumMember(Value = "POST")]
        // ReSharper disable once InconsistentNaming
        POST,

        /// <summary>
        /// HTTP DELETE.
        /// </summary>
        [EnumMember(Value = "DELETE")]
        // ReSharper disable once InconsistentNaming
        DELETE,

        /// <summary>
        /// HTTP PATCH.
        /// </summary>
        [EnumMember(Value = "PATCH")]
        // ReSharper disable once InconsistentNaming
        PATCH,

        /// <summary>
        /// HTTP CONNECT.
        /// </summary>
        [EnumMember(Value = "CONNECT")]
        // ReSharper disable once InconsistentNaming
        CONNECT,

        /// <summary>
        /// HTTP OPTIONS.
        /// </summary>
        [EnumMember(Value = "OPTIONS")]
        // ReSharper disable once InconsistentNaming
        OPTIONS,

        /// <summary>
        /// HTTP TRACE.
        /// </summary>
        [EnumMember(Value = "TRACE")]
        // ReSharper disable once InconsistentNaming
        TRACE
    }

    internal class Express
    {
        private readonly Router _router = new();

        private TcpListener? _listener;

        private readonly List<MiddlewareCallback> _callbacks = new();

        private readonly Dictionary<string, RenderEngineCallback> _engines = new();

        private readonly NameValueCollection _settings = new();

        /// <summary>
        /// This is a built-in middleware function in Express. It serves static files and is based on serve-static.
        ///
        /// The root argument specifies the root directory from which to serve static assets. The function determines
        /// the file to serve by combining req.url with the provided root directory. When a file is not found, instead
        /// of sending a 404 response, it instead calls next() to move on to the next middleware, allowing for stacking
        /// and fall-backs.
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public static MiddlewareCallback Static(string root)
        {
            throw new NotImplementedException();
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static MiddlewareCallback json() => parseJson;

        public static MiddlewareCallback text() => parseText;

        public static MiddlewareCallback raw() => parseRaw;

        public static MiddlewareCallback urlencoded() => parseUrlencoded;

        private static void parseJson(Request req, Response res, NextCallback? next = null)
        {
        }

        private static void parseText(Request req, Response res, NextCallback? next = null)
        {
        }

        private static void parseRaw(Request req, Response res, NextCallback? next = null)
        {
        }

        private static void parseUrlencoded(Request req, Response res, NextCallback? next = null)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal Router Router()
        {
            return _router;
        }

        /// <summary>
        /// Returns true if the Boolean setting name is disabled (false), where name is one of
        /// the properties from the app settings table.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool disabled(string key) => (_settings[key]!.Equals("false", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Sets the Boolean setting name to false, where name is one of the properties from
        /// the app settings table. Calling app.set('foo', false) for a Boolean property
        /// is the same as calling app.disable('foo').
        /// </summary>
        /// <param name="key"></param>
        public void disable(string key) => _settings[key] = "false";

        /// <summary>
        /// Returns true if the setting name is enabled (true), where name is one of the
        /// properties from the app settings table.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool enabled(string key) => (_settings[key]!.Equals("true", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Sets the Boolean setting name to true, where name is one of the properties from
        /// the app settings table. Calling app.set('foo', true) for a Boolean property is
        /// the same as calling app.enable('foo').
        /// </summary>
        /// <param name="key"></param>
        public void enable(string key) => _settings[key] = "true";

        /// <summary>
        /// Assigns setting name to value. You may store any value that you want, but certain
        /// names can be used to configure the behavior of the server. These special names are
        /// listed in the app settings table.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void set(string key, string value) => _settings[key] = value;

        /// <summary>
        /// Returns the rendered HTML of a view via the callback function. It accepts an optional parameter
        /// that is an object containing local variables for the view. It is like res.render(), except it
        /// cannot send the rendered view to the client on its own.
        /// </summary>
        public void render(string name, NameValueCollection options)
        {
            var view = this.get("views");

            var viewEngine = this.get("view engine");
        }

        /// <summary>
        /// Returns the value of name app setting, where name is one of the strings in the
        /// app settings table. For example:
        /// </summary>
        /// <param name="key"></param>
        public string? get(string key)
        {
            return _settings[key];
        }

        public void use(string mountPath)
        {
            _router.MountPath = mountPath;
        }

        public void use(string mountPath, Router router)
        {
            _router.use(mountPath, router);
        }

        public void use(string path, MiddlewareCallback callback)
        {
            _router.use(path, callback);
        }

        public void use(MiddlewareCallback callback)
        {
            _router.use(callback);
        }

        public void use(ErrorCallback callback)
        {
            _router.use(callback);
        }

        public void use(List<MiddlewareCallback> callback)
        {
            _router.use(callback);
        }

        public void get(string path, params MiddlewareCallback[] args)
        {
            _router.get(path, args);
        }

        public void post(string path, params MiddlewareCallback[] args)
        {
            _router.post(path, args);
        }

        public void put(string path, params MiddlewareCallback[] args)
        {
            _router.put(path, args);
        }

        public void remove(string path, params MiddlewareCallback[] args)
        {
            _router.remove(path, args);
        }

        public void patch(string path, params MiddlewareCallback[] args)
        {
            _router.patch(path, args);
        }

        public List<MiddlewareCallback> Middlewares() => _callbacks;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ext"></param>
        /// <param name="engine"></param>
        public void engine(string ext, RenderEngineCallback engine)
        {
            if (!ext.StartsWith("."))
                ext = "." + ext;

            _engines[ext] = engine;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="callback"></param>
        public void listen(int port, ListenCallback? callback = null)
        {
            listen(port, string.Empty, null, callback);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="host"></param>
        /// <param name="backLog"></param>
        /// <param name="callback"></param>
        public void listen(int port, string? host = "", object? backLog = null, ListenCallback? callback = null)
        {
            var task = Task.Run(() =>
            {
                var maxThreadsCount = Environment.ProcessorCount * 4;
                ThreadPool.SetMaxThreads(maxThreadsCount, maxThreadsCount);
                ThreadPool.SetMinThreads(2, 2);

                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();

                callback?.Invoke();

                while (true)
                {
                    _ = ThreadPool.QueueUserWorkItem(Utils.ClientThread!, Utils.Parameters.CreateInstance(this, _listener.AcceptTcpClient()));
                }

            });
        }


    }
}