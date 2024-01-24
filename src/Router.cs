using System.Net;

namespace HTTPServer;

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
        for (var index = 0; index < _routes.Count; index++)
        {
            var route = _routes[index];

            if (route.Method != req.Method) continue;
            if (!Match(route.Path, req)) continue;

            foreach (var middleware in route.Middlewares)
            {
                gotoNext = false;
                try
                {
                    middleware?.Invoke(req, res, next =>
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
                        errorCallback(e as Exception, req, res, next => { gotoNext = true; });
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