using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using static System.Net.Mime.MediaTypeNames;
using System.Text.RegularExpressions;

namespace HTTPServer
{
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
                var headerPair = headerLine.Split(new[] { ':' }, 2);
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

        public NameValueCollection Headers = new NameValueCollection();

        public HttpStatusCode Code = HttpStatusCode.OK;

        private readonly TcpClient _client;

        private string _body = String.Empty;

        internal Response(TcpClient client)
        {
            _client = client;
        }

        public Response Status(HttpStatusCode code)
        {
            Code = code;

            return this;
        }

        public Response Send(string body)
        {
            _body = body;

            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Send()
        {
            _client.GetStream().Write(Encoding.UTF8.GetBytes("HTTP/1.1 "));
            _client.GetStream().Write(Encoding.UTF8.GetBytes($"200 OK" + "\r\n"));

            if (!string.IsNullOrEmpty(_body))
                Headers["content-length"] = _body.Length.ToString();
            Headers["X-Powered-By"] = "Nexa";
            Headers["connection"] = "close";

            foreach (string key in Headers)
            {
                _client.GetStream().Write(Encoding.UTF8.GetBytes($"{key}: "));
                _client.GetStream().Write(Encoding.UTF8.GetBytes($"{Headers[key]}\r\n"));
            }

            _client.GetStream().Write(Encoding.UTF8.GetBytes("\r\n"));

            if (!string.IsNullOrEmpty(_body))
                _client.GetStream().Write(Encoding.UTF8.GetBytes(_body));

            _client.Close();
            _client.Dispose();
        }
    }

    internal class Route
    {
        public HttpMethod Method { get; }
        public string Path { get; }
        public List<Callback> Middlewares { get; }

        public List<string> Params = new();

        public Route(HttpMethod method, string path, List<Callback> middlewares)
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

        private readonly List<Callback> _middlewares = new();

        private readonly List<Route> _routes = new();

        private readonly Dictionary<string, Router> _routers = new();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="leftPath"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        private static bool Match(string leftPath, Request req)
        {
            var leftSubDirs = leftPath.Split('/');
            var rightSubDirs = req.Path.Split('/');

            if (leftSubDirs.Length != rightSubDirs.Length) return false;

            for (var i = 1; i < leftSubDirs.Length; i++)
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
                    middleware(req, res);
                }

                return true;
            }

            foreach (var router in _routers.Values)
            {
                if (router.Evaluate(req, res))
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
            foreach (var middleware in _middlewares)
            {
                middleware(req, res);
            }

            Evaluate(req, res);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="method"></param>
        /// <param name="path"></param>
        /// <param name="middlewares"></param>
        // ReSharper disable once InconsistentNaming
        public void METHOD(HttpMethod method, string path, List<Callback> middlewares)
        {
            if (path == "/") path = "";
            if (MountPath == "/") MountPath = "";

            path = MountPath + path;
            path = path.Trim();

            var route = new Route(method, path, middlewares);

            // Check for Parameters
            var subDirectories = path.Split('/').Skip(1).ToArray();
            foreach (var subDirectory in subDirectories)
            {
                if (subDirectory.StartsWith(':'))
                {
                    route.Params.Add(subDirectory.Substring(1));
                }
            }

            _routes.Add(route);
        }

        public void Get(string path, Callback callback)
        {
            METHOD(HttpMethod.GET, path, new List<Callback>() { callback });
        }
    }

    public delegate void Callback(Request request, Response response, Callback? callback = null);

    public delegate void ListenCallback();

    internal class Parameters
    {
        public Express Express { get; }
        public TcpClient TcpClient { get; }

        public Parameters(Express express, TcpClient tcpClient)
        {
            Express = express;
            TcpClient = tcpClient;
        }
    }

    internal class Express
    {
        private readonly Router _router = new();

        private TcpListener? _listener;

        private readonly List<Callback> _callbacks = new();

        public void Use(string mountPath)
        {
            _router.MountPath = mountPath;
        }

        public void Use(string mountPath, Router router)
        {
            throw new NotImplementedException();
        }

        public void Get(string path, Callback callback)
        {
            _router.Get(path, callback);
        }

        public List<Callback> Middlewares() => _callbacks;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stateInfo"></param>
        private static void ClientThread(object stateInfo)
        {
            var aa = stateInfo as Parameters;
            var tcpClient = aa.TcpClient;
            var express = aa.Express;

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

            var res = new Response(tcpClient);

            express._router.Dispatch(req, res);

            res.Send();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="callback"></param>
        public void Listen(int port, ListenCallback? callback = null)
        {
            Listen(port, string.Empty, null, callback);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="host"></param>
        /// <param name="backLog"></param>
        /// <param name="callback"></param>
        public void Listen(int port, string? host = "", object? backLog = null, ListenCallback? callback = null)
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

internal class Express
{
    private static void Main(string[] _)
    {
        var app = new HTTPServer.Express();
        const int port = 8080;

        app.Use("/v1");

        //app.Get("/conformance", (req, res, next) =>
        //{
        //    res.Status(HttpStatusCode.OK).Send("conformance");
        //});

        app.Get("/servers/:serverId", (req, res, next) =>
        {
            res.Status(HttpStatusCode.OK).Send($"with para {req.Params["serverId"]}");
        });

        app.Listen(port, () =>
        {
            Console.WriteLine($"Example app listening on port {port}");
        });

        Console.ReadLine();
    }
}

