using System.Net;

namespace dotNetExpress;

public class Router
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
                    res.Status(HttpStatusCode.InternalServerError);

                    foreach (var errorCallback in _errorHandler)
                    {
                        errorCallback(e as Exception, req, res, next => { gotoNext = true; });
                        if (!gotoNext) break;
                    }

                    res.Send(e.Message);
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

    #region Methods

    /// <summary>
    /// This method is just like the router.METHOD() methods, except that it matches all HTTP methods (verbs).
    ///
    /// This method is extremely useful for mapping “global” logic for specific path prefixes or arbitrary matches.
    /// For example, if you placed the following route at the top of all other route definitions, it would require
    /// that all routes from that point on would require authentication, and automatically load a user. Keep in mind
    /// that these callbacks do not have to act as end points; loadUser can perform a task, then call next() to
    /// continue matching subsequent routes.
    /// </summary>
    public void All(string path, params MiddlewareCallback[] args)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// The router.METHOD() methods provide the routing functionality in Express, where METHOD is one of the HTTP methods,
    /// such as GET, PUT, POST, and so on, in lowercase. Thus, the actual methods are router.get(), router.post(), router.put(), and so on.
    /// </summary>
    /// <param name="method"></param>
    /// <param name="path"></param>
    /// <param name="middlewares"></param>
    // ReSharper disable once InconsistentNaming
    private void METHOD(HttpMethod method, string path, MiddlewareCallback[] middlewares)
    {
        if (path == "/") path = "";
        if (MountPath == "/") MountPath = "";

        if (!path.StartsWith('/')) path = "/" + path;

        path = MountPath + path;
        path = path.Trim();

        var route = new Route(method, path, middlewares);

        _routes.Add(route);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="middlewares"></param>
    public void Head(string path, params MiddlewareCallback[] middlewares)
    {
        METHOD(HttpMethod.HEAD, path, middlewares);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="middlewares"></param>
    public void Get(string path, params MiddlewareCallback[] middlewares)
    {
        METHOD(HttpMethod.GET, path, middlewares);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="middlewares"></param>
    public void Post(string path, params MiddlewareCallback[] middlewares)
    {
        METHOD(HttpMethod.POST, path, middlewares);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="middlewares"></param>
    public void Put(string path, params MiddlewareCallback[] middlewares)
    {
        METHOD(HttpMethod.PUT, path, middlewares);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="middlewares"></param>
    public void Delete(string path, params MiddlewareCallback[] middlewares)
    {
        METHOD(HttpMethod.DELETE, path, middlewares);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="middleware"></param>
    public void Patch(string path, params MiddlewareCallback[] middleware)
    {
        METHOD(HttpMethod.PATCH, path, middleware );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="middleware"></param>
    public void Any(MiddlewareCallback middleware)
    {
        _catchAll = middleware;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="callback"></param>
    public void Use(ErrorCallback callback)
    {
        _errorHandler.Add(callback);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="callbacks"></param>
    public void Use(List<ErrorCallback> callbacks)
    {
        _errorHandler.AddRange(callbacks);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="callback"></param>
    public void Use(MiddlewareCallback callback)
    {
        _middlewares.Add(callback);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="middlewares"></param>
    public void Use(MiddlewareCallback[] middlewares)
    {
        foreach (var middleware in middlewares)
            _middlewares.Add(middleware);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="middlewareCallback"></param>
    public void Use(string path, MiddlewareCallback middlewareCallback)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="mountPath"></param>
    /// <param name="anotherRouter"></param>
    public void Use(string mountPath, Router anotherRouter)
    {
        anotherRouter.MountPath = mountPath;
        anotherRouter._parent = this;
        _routers[anotherRouter.MountPath] = anotherRouter;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="mountPath"></param>
    public void Use(string mountPath)
    {
        MountPath = mountPath;
    }

    #endregion

}