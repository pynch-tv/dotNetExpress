using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using HTTPServer;
using System.Text.RegularExpressions;

// ReSharper disable InconsistentNaming

namespace HTTPServer
{
    public delegate void NextCallback(Error? err = null);

    public delegate void ErrorCallback(Error err, Request request, Response response, NextCallback? next = null);

    public delegate void MiddlewareCallback(Request request, Response response, NextCallback? next = null);

    public delegate void ListenCallback();

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

    public class Request
    {
        /// <summary>
        /// Contains a string corresponding to the HTTP method of the request: GET, POST, PUT, and so on.
        /// </summary>
        public HttpMethod Method;

        /// <summary>
        /// Contains the host derived from the Host HTTP header.
        /// </summary>
        public string Host;

        /// <summary>
        /// Contains the host derived from the HostName HTTP header.
        /// </summary>
        public string HostName;

        /// <summary>
        /// This property is much like req.url; however, it retains the original request URL,
        /// allowing you to rewrite req.url freely for internal routing purposes. For example,
        /// the “mounting” feature of app.use() will rewrite req.url to strip the mount point.
        /// </summary>
        public string OriginalUrl;

        /// <summary>
        /// The URL path on which a router instance was mounted.
        /// The req.baseUrl property is similar to the mountpath property of the app object,
        /// except app.mountpath returns the matched path pattern(s).
        /// </summary>
        public string BaseUrl;

        /// <summary>
        /// Contains the path part of the request URL.
        /// </summary>
        public string Path;

        /// <summary>
        /// This property is an object containing a property for each query string parameter
        /// in the route. When query parser is set to disabled, it is an empty object {},
        /// otherwise it is the result of the configured query parser.
        /// </summary>
        public string Query = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        public string Protocol;

        /// <summary>
        /// 
        /// </summary>
        public NameValueCollection Headers = new();

        /// <summary>
        /// This property is an object containing properties mapped to the named route “parameters”.
        /// For example, if you have the route /user/:name, then the “name” property is available
        /// as req.params.name. This object defaults to {}.
        /// </summary>
        public NameValueCollection Params = new();

        public static bool TryParse(string bytes, out Request request)
        {
            request = new Request();

            var headerLines = bytes.Split("\r\n");

            var requestLine = headerLines[0];
            var requestLineParts = requestLine.Split(' ');

            if (!Enum.TryParse(requestLineParts[0], out request.Method))
                return false;

            request.OriginalUrl = requestLineParts[1];
            var idx = request.OriginalUrl.LastIndexOf('?');
            if (idx > -1)
            {
                request.Query = request.OriginalUrl[(idx + 1)..];
                request.Path = request.OriginalUrl[..idx];
            }
            else
                request.Path = request.OriginalUrl;

            idx = requestLineParts[2].IndexOf('/');
            request.Protocol = requestLineParts[2][..idx].ToLower();

            request.Headers.Clear();
            for (var i = 1; i < headerLines.Length; i++)
            {
                var headerLine = headerLines[i];
                var headerPair = headerLine.Split(":", 2, StringSplitOptions.TrimEntries);
                if (headerPair.Length != 2) continue; // basic checking
                // header in case insensitive (see 
                request.Headers.Add(headerPair[0].ToLower(), headerPair[1]);
            }

            request.Host = request.Headers["host"];
            request.HostName = request.Host.Split(':')[0];

            return true;
        }
    }

    public class Response
    {
        public HttpMethod HttpMethod;

        private readonly NameValueCollection _headers = new();

        public readonly NameValueCollection _locals = new();

        private HttpStatusCode _httpStatusCode = HttpStatusCode.OK;

        private readonly TcpClient _client;

        private string _body = string.Empty;

        private bool _headersSent = false;

        internal Response(TcpClient client)
        {
            _headersSent = false;
            _client = client;
        }

        public Response status(HttpStatusCode code)
        {
            _httpStatusCode = code;

            return this;
        }

        public Response set(string field, string value)
        {
            _headers[field] = value;

            return this;
        }

        public string? get(string field)
        {
            return _headers[field];
        }

        public bool headersSent() => _headersSent;

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
            _client.GetStream().Write(Encoding.UTF8.GetBytes("HTTP/1.1 "));
            _client.GetStream().Write(Encoding.UTF8.GetBytes($"{(int)_httpStatusCode} {Regex.Replace(_httpStatusCode.ToString(), "(?<=[a-z])([A-Z])", " $1", RegexOptions.Compiled)}" + "\r\n"));

            if (!string.IsNullOrEmpty(_body))
                _headers["content-length"] = _body.Length.ToString();
            _headers["connection"] = "close";

            foreach (string key in _headers)
            {
                _client.GetStream().Write(Encoding.UTF8.GetBytes($"{key}: "));
                _client.GetStream().Write(Encoding.UTF8.GetBytes($"{_headers[key]}\r\n"));
            }

            _client.GetStream().Write(Encoding.UTF8.GetBytes("\r\n"));

            _headersSent = true;

            if (!string.IsNullOrEmpty(_body))
                _client.GetStream().Write(Encoding.UTF8.GetBytes(_body));

            _client.Close();
            _client.Dispose();
        }
    }

    public class Error : Exception
    {
        public Error(string message) : base(message)
        {
        }
    }

    internal class Route
    {
        public HttpMethod Method { get; }
        public string Path { get; }
        public List<MiddlewareCallback> Middlewares { get; }

        public List<string> Params = new();

        public Route(HttpMethod method, string path, List<MiddlewareCallback> middlewares)
        {
            Method = method;
            Path = path;
            Middlewares = middlewares;
        }
    }

    internal class Router
    {
        public string MountPath = string.Empty;

        private Router _parent = null;

        private readonly List<ErrorCallback> _errorHandler = new();

        private readonly List<MiddlewareCallback> _middlewares = new();

        private readonly List<Route> _routes = new();

        private readonly Dictionary<string, Router> _routers = new();

        private MiddlewareCallback? _catchAll = null;

        private bool gotoNext;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="leftPath"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        private static bool Match(string leftPath, Request req)
        {
            var leftSubDirs = leftPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var rightSubDirs = req.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (leftSubDirs.Length != rightSubDirs.Length) return false;

            for (var i = 0; i < leftSubDirs.Length; i++)
            {
                if (leftSubDirs[i].StartsWith(':'))
                    req.Params[leftSubDirs[i][1..]] = rightSubDirs[i];
                else
                    if (leftSubDirs[i] != rightSubDirs[i]) return false;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="req"></param>
        /// <param name="res"></param>
        /// <returns></returns>
        private bool Evaluate(Request req, Response res)
        {
            foreach (var route in _routes)
            {
                if (route.Method != req.Method) continue;
                if (!Match(route.Path, req)) continue;

                foreach (var middleware in route.Middlewares)
                {
                    gotoNext = false;
                    try
                    {
                        if (null != middleware)
                            middleware(req, res, next =>
                            {
                                if (null != next)
                                    throw next; 
                                gotoNext = true;
                            });
                    }
                    catch (Exception e)
                    {
                        res.status(HttpStatusCode.InternalServerError);

                        foreach (var errorCallback in _errorHandler)
                        {
                            errorCallback(e as Error, req, res, next => { gotoNext = true; });
                            if (!gotoNext) break;
                        }

                        res.send(e.Message);
                        return false;
                    }
                }

                return true;
            }

            foreach (var router in _routers.Values)
            {
                if (router.Evaluate(req, res))
                    return true;
            }

            if (null != _catchAll)
            {
                _catchAll(req, res);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="req"></param>
        /// <param name="res"></param>
        public void Dispatch(Request req, Response res)
        {
            gotoNext = true;
            foreach (var middleware in _middlewares)
            {
                gotoNext = false;
                middleware(req, res, next =>
                {
                    if (null != next)
                        throw next;
                    gotoNext = true;
                });
                if (!gotoNext) break;
            }

            if (gotoNext)
                Evaluate(req, res);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="method"></param>
        /// <param name="path"></param>
        /// <param name="middlewares"></param>
        // ReSharper disable once InconsistentNaming
        public void METHOD(HttpMethod method, string path, List<MiddlewareCallback> middlewares)
        {
            if (path == "/") path = "";
            if (MountPath == "/") MountPath = "";

            if (!path.StartsWith('/')) path = "/" + path;

            path = MountPath + path;
            path = path.Trim();

            var route = new Route(method, path, middlewares);

            _routes.Add(route);
        }

        public void head(string path, params MiddlewareCallback[] middleware)
        {
            METHOD(HttpMethod.HEAD, path, middleware.ToList() );
        }

        public void get(string path, params MiddlewareCallback[] middleware)
        {
            METHOD(HttpMethod.GET, path, middleware.ToList());
        }

        public void post(string path, params MiddlewareCallback[] middleware)
        {
            METHOD(HttpMethod.POST, path, middleware.ToList());
        }

        public void put(string path, params MiddlewareCallback[] middleware)
        {
            METHOD(HttpMethod.PUT, path, middleware.ToList());
        }

        public void remove(string path, params MiddlewareCallback[] middleware)
        {
            METHOD(HttpMethod.DELETE, path, middleware.ToList());
        }

        public void patch(string path, params MiddlewareCallback[] middleware)
        {
            METHOD(HttpMethod.PATCH, path, middleware.ToList() );
        }

        public void any(MiddlewareCallback middleware)
        {
            _catchAll = middleware;
        }

        public void use(ErrorCallback callback)
        {
            _errorHandler.Add(callback);
        }

        public void use(List<ErrorCallback> callbacks)
        {
            _errorHandler.AddRange(callbacks);
        }

        public void use(MiddlewareCallback callback)
        {
            _middlewares.Add(callback);
        }

        public void use(List<MiddlewareCallback> middlewares)
        {
            foreach (var middleware in middlewares)
                _middlewares.Add(middleware);
        }

        public void use(string path, MiddlewareCallback middlewareCallback)
        {
        }

        public void use(string mountPath, Router anotherRouter)
        {
            anotherRouter.MountPath = mountPath;
            anotherRouter._parent = this;
            _routers[anotherRouter.MountPath] = anotherRouter;
        }

        public void use(string mountPath)
        {
            MountPath = mountPath;
        }
    }

    internal class Express
    {
        private class Parameters
        {
            public Express Express { get; }
            public TcpClient TcpClient { get; }

            public Parameters(Express express, TcpClient tcpClient)
            {
                Express = express;
                TcpClient = tcpClient;
            }
        }

        private readonly Router _router = new();

        private TcpListener? _listener;

        private readonly List<MiddlewareCallback> _callbacks = new();

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
        public Router Router()
        {
            return _router;
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


        private static readonly List<TcpClient> _webSockets = new();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stateInfo"></param>
        private static void ClientThread(object stateInfo)
        {
            static string HashKey(string key)
            {
                const string handshakeKey = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                var longKey = key + handshakeKey;

                var sha1 = SHA1.Create();
                var hashBytes = sha1.ComputeHash(Encoding.ASCII.GetBytes(longKey));

                return Convert.ToBase64String(hashBytes);
            }

            var aa = stateInfo as Parameters;
            var tcpClient = aa.TcpClient;
            var express = aa.Express;

            // TODO: read until \r\n\r\n and leave body untouched

            var content = "";
            var buffer = new byte[1024];
            int count;
            while ((count = tcpClient.GetStream().Read(buffer, 0, buffer.Length)) > 0)
            {
                content += Encoding.ASCII.GetString(buffer, 0, count);
                if (content.IndexOf("\r\n\r\n", StringComparison.Ordinal) >= 0 || content.Length > 4096)
                    break;
            }

            var headerLength = content.IndexOf("\r\n\r\n", StringComparison.Ordinal);

            var header = content[..headerLength];
            var body = content[(headerLength + 4)..];

            if (!Request.TryParse(header, out var req))
            {
                // error - return
            }

            if (string.Equals(req.Headers["connection"], "Upgrade", StringComparison.OrdinalIgnoreCase) 
             && string.Equals(req.Headers["upgrade"], "websocket", StringComparison.OrdinalIgnoreCase))
            {
                var key = req.Headers["sec-websocket-key"];

                var res = new Response(tcpClient);
                res.set("Upgrade", "WebSocket");
                res.set("Connection", "Upgrade");
                res.set("Sec-WebSocket-Accept", HashKey(key));

                res.send();

                lock (_webSockets)
                {
                    _webSockets.Add(tcpClient);
                }
            }
            else
            {
                var res = new Response(tcpClient);

                express._router.Dispatch(req, res);

                res.send();
            }

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
                    _ = ThreadPool.QueueUserWorkItem(ClientThread!,
                        new Parameters(this, _listener.AcceptTcpClient()));
                }

            });
        }
    }
}

internal class Program
{
    private static void Error()
    {
        var app = new Express();
        const int port = 8080;

        app.get("/", (req, res, next) => throw new Exception("broken"));

        app.get("/next", (req, res, next) =>
        {
            next?.Invoke(new Error("BROKEN"));
        });

        app.listen(port, () =>
        {
            Console.WriteLine($"Example app listening on port {port}");
        });
    }

    private static void Favicon()
    {
        var app = new Express();
        const int port = 8080;

        var favicon = Convert.FromBase64String(
                       "AAABAAEAEBAQAAAAAAAoAQAAFgAAACgAAAAQAAAAIAAAAAEABAAAAAAAgAA" +
                         "AAAAAAAAAAAAAEAAAAAAAAAAAAAAA/" +
                         "4QAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
                         "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAERE" +
                         "QAAAAAAEAAAEAAAAAEAAAABAAAAEAAAAAAQAAAQAAAAABAAAAAAAAAAAAAA" +
                         "AAAAAAAAAAAAEAABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
                         "AAAAAAAAAAAAAD//wAA//8AAP//AAD8HwAA++8AAPf3AADv+wAA7/sAAP//" +
                         "AAD//wAA+98AAP//AAD//wAA//8AAP//AAD//wAA");

        app.get("/", (req, res, next) => {
            res.send("Hello World!");
        });

        app.get("/favicon.ico", (req, res, next) =>
        {
            res.status(HttpStatusCode.OK);
            res.set("Content-Length", favicon.Length.ToString());
            res.set("Content-Type", "image/x-icon");
        });

        app.listen(port, () =>
        {
            Console.WriteLine($"Example app listening on port {port}");
        });
    }

    private static void BodyParser()
    {
        var app = new Express();
        const int port = 8080;

        //    app.use(Express.json());

        app.get("/", Express.json());

        app.listen(port, () =>
        {
            Console.WriteLine($"Example app listening on port {port}");
        });
    }

    private static void Middleware()
    {
        void middleware1(Request req, Response res, NextCallback? next = null)
        {
            next();
        }

        void middleware2(Request req, Response res, NextCallback? next = null)
        {
            next();
        }

        void middleware3(Request req, Response res, NextCallback? next = null)
        {
            next();
        }

        var app = new Express();
        const int port = 8080;

        // add a single middleware
        app.use(middleware1);

        app.use((req, res, next) =>
        {
            Console.WriteLine("hello");
            next();
        });

        app.get("/", middleware2, null, middleware3);

        app.listen(port, () =>
        {
            Console.WriteLine($"Example app listening on port {port}");
        });
    }

    private static void StaticFiles()
    {
        var app = new Express();
        const int port = 8080;

        var __dirname = Directory.GetCurrentDirectory();

        // express on its own has no notion
        // of a "file". The express.static()
        // middleware checks for a file matching
        // the `req.path` within the directory
        // that you pass it. In this case "GET /js/app.js"
        // will look for "./public/js/app.js".
        app.use(Express.Static(Path.Combine(__dirname, "public")));

        // if you wanted to "prefix" you may use
        // the mounting feature of Connect, for example
        // "GET /static/js/app.js" instead of "GET /js/app.js".
        // The mount-path "/static" is simply removed before
        // passing control to the express.static() middleware,
        // thus it serves the file correctly by ignoring "/static"
        app.use("/static", Express.Static(Path.Combine(__dirname, "public")));

        // if for some reason you want to serve files from
        // several directories, you can use express.static()
        // multiple times! Here we're passing "./public/css",
        // this will allow "GET /style.css" instead of "GET /css/style.css":
        app.use(Express.Static(Path.Combine(__dirname, "public", "css")));

        app.listen(port, () =>
        {
            Console.WriteLine($"Example app listening on port {port}");
        });
    }

    private static void MultiRouter()
    {
        var app = new Express();
        const int port = 8080;

        var apiv1 = new Router();
        var apiv2 = new Router();

        app.use("/api/v1", apiv1);
        app.use("/api/v2", apiv2);

        apiv1.get("/", (req, res, next) =>
        {
            res.send("Hello World from api v1.");
        });

        apiv2.get("/", (req, res, next) =>
        {
            res.send("Hello World from api v2.");
        });


        app.get("/", (req, res, next) =>
        {
            res.send("Hello World from root route.");
        });

        app.listen(port, () =>
        {
            Console.WriteLine($"Example app listening on port {port}");
        });
    }

    private static void HelloWorld()
    {
        var app = new Express();
        const int port = 8080;

        app.get("/", (req, res, next) =>
        {
            res.send("Hello World");
        });

        app.listen(port, () =>
        {
            Console.WriteLine($"Example app listening on port {port}");
        });
    }

    private static void BasicRouting()
    {
        var app = new Express();
        const int port = 8080;

        app.get("/", (req, res, next) =>
        {
            res.send("Hello World!");
        });

        app.post("/", (req, res, next) =>
        {
            res.send("Got a POST request");
        });

        app.put("/user", (req, res, next) =>
        {
            res.send("Got a POST request");
        });

        app.remove("/user", (req, res, next) =>
        {
            res.send("Got a POST request");
        });

        app.listen(port, () =>
        {
            Console.WriteLine($"Example app listening on port {port}");
        });
    }

    private static void Main(string[] _)
    {
        Middleware();

        Console.ReadLine();
    }
}

