using System.Text.RegularExpressions;
using dotNetExpress.Delegates;
using dotNetExpress.Exceptions;
using dotNetExpress.Options;

namespace dotNetExpress;

public class Router
{
    public string MountPath = string.Empty;

    private Router? _parent;

    private readonly List<ErrorCallback> _errorHandler = [];

    private readonly List<MiddlewareCallback> _middlewares = [];

    private readonly List<Route> _routes = [];

    private readonly Dictionary<string, Router> _routers = [];

    private MiddlewareCallback? _catchAll;

    private readonly RouterOptions _options;

    private bool _gotoNext;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="options"></param>
    public Router(RouterOptions? options = null)
    {
        options ??= new RouterOptions();
        _options = options;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="leftPath"></param>
    /// <param name="req"></param>
    /// <returns></returns>
    private bool Match(string leftPath, Request req)
    {
        if (_options.Strict && req.Path.EndsWith($"/")) return false;

        // RemoveEmptyEntries also remove blank entries at the start of the SubDirs array,
        // making the parsing slightly faster.
        var leftSubDirs  = leftPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var rightSubDirs = req.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (leftSubDirs.Length != rightSubDirs.Length) return false;

        for (var i = 0; i < leftSubDirs.Length; i++)
        {
            if (leftSubDirs[i].Contains(':'))
            {
                var leftIndex = leftSubDirs[i].IndexOf(':');
                if (string.Compare(leftSubDirs[i], 0, rightSubDirs[i], 0, leftIndex) != 0) return false;

                var rightStart = rightSubDirs[i].Remove(leftIndex);
                var rightEnd = rightSubDirs[i].Remove(0, leftIndex);

                if (leftSubDirs[i][..leftIndex] != rightStart) return false;

                req.Params[leftSubDirs[i][++leftIndex..]] = Uri.UnescapeDataString(rightEnd);
            }
            else if (leftSubDirs[i].IndexOfAny(['?', '*', '$', '(', ')']) >= 0) // check for regular expression pattern 
            {
                if (!Regex.IsMatch(rightSubDirs[i], WildCardToRegular(leftSubDirs[i])))
                    return false;
            }
            else if (_options.CaseSensitive ? !leftSubDirs[i].Equals(rightSubDirs[i], StringComparison.Ordinal) : !leftSubDirs[i].Equals(rightSubDirs[i], StringComparison.CurrentCultureIgnoreCase)) 
                return false;
        }

        return true;

        static string WildCardToRegular(string value)
        {
            // TODO: needs additional work
            return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="req"></param>
    /// <param name="res"></param>
    /// <returns></returns>
    public async Task<bool> Dispatch(Request req, Response res)
    {
        req.BaseUrl = MountPath;

        _gotoNext = true;
        foreach (var middleware in _middlewares)
        {
            _gotoNext = false;
            try
            {
                await middleware(req, res, next =>
                {
                    if (null != next)
                        throw next;
                    _gotoNext = true;
                });
                if (!_gotoNext)
                    break;
            }
            catch (ExpressException e)
            {
                foreach (var errorCallback in _errorHandler)
                {
                    await errorCallback(e, req, res, next => { _gotoNext = true; });
                    if (!_gotoNext) break;
                }

                await res.Status(e.Status).Json(new { status = e.Status, title = e.Title, detail = e.Detail });
                return true;
            }
            catch (Exception e)
            {
                foreach (var errorCallback in _errorHandler)
                {
                    await errorCallback(e as ExpressException, req, res, next => { _gotoNext = true; });
                    if (!_gotoNext) break;
                }

                await res.Status(500).Json(new { status = 500, title = "Exception", description = e.Message });
                return true;
            }
        }
        if (!_gotoNext) return true;

        foreach (var route in _routes.Where(route => (route.Method == req.Method || route.Method == null)).Where(route => Match(route.Path, req)))
        {
            foreach (var middleware in route.Middlewares)
            {
                _gotoNext = false;
                try
                {
                    await middleware(req, res, next =>
                    {
                        if (null != next)
                            throw next; // throw the error to the error handler
                        _gotoNext = true;
                    });

                    if (!_gotoNext)
                        break;
                }
                catch (ExpressException e)
                {
                    foreach (var errorCallback in _errorHandler)
                    {
                        await errorCallback(e, req, res, next => { _gotoNext = true; });
                        if (!_gotoNext) break;
                    }

                    await res.Status(e.Status).Json(new { status = e.Status, title = e.Title, detail = e.Detail });
                    return true;
                }
                catch (Exception e)
                {
                    foreach (var errorCallback in _errorHandler)
                    {
                        await errorCallback(e as ExpressException, req, res, next => { _gotoNext = true; });
                        if (!_gotoNext) break;
                    }

                    await res.Status(500).Json(new { status = 500, title = "Exception", description = e.Message });
                    return true;
                }
            }

            return true;
        }

        foreach (var router in _routers.Values)
        {
            if (req.OriginalUrl.StartsWith(router.MountPath))
            {
                req.BaseUrl = router.MountPath;

                if (await router.Dispatch(req, res))
                    return true;
            }

        }
        await res.Status(404).Json(new { code = 404, title = "Path not found", detail = $"Given Path {req.OriginalUrl} not found" });


        if (null == _catchAll) return false;

        await _catchAll(req, res);

        return true;
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
    public void All(string path, params MiddlewareCallback[] middlewares)
    {
        // paths should not end with / (unless it is the landing page)
        if (path.EndsWith("/") && path != "/")
            return;

        if (path == "/") path = "";
        if (MountPath == "/") MountPath = "";

        if (!path.StartsWith('/')) path = "/" + path;

        path = MountPath + path;
        path = path.Trim();

        var route = new Route(path, middlewares);

        _routes.Add(route);
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
        // paths should not end with / (unless it is the landing page)
        if (path.EndsWith("/") && path != "/")
            return;
        
        if (path == "/") path = "";
        if (MountPath == "/") MountPath = "";

        path = MountPath + (string.IsNullOrEmpty(path) ? "" : path);
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
        METHOD(HttpMethod.Head, path, middlewares);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="middlewares"></param>
    public void Get(string path, params MiddlewareCallback[] middlewares)
    {
        METHOD(HttpMethod.Get, path, middlewares);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="middlewares"></param>
    public void Post(string path, params MiddlewareCallback[] middlewares)
    {
        METHOD(HttpMethod.Post, path, middlewares);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="middlewares"></param>
    public void Put(string path, params MiddlewareCallback[] middlewares)
    {
        METHOD(HttpMethod.Put, path, middlewares);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="middlewares"></param>
    public void Delete(string path, params MiddlewareCallback[] middlewares)
    {
        METHOD(HttpMethod.Delete, path, middlewares);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="middleware"></param>
    public void Patch(string path, params MiddlewareCallback[] middleware)
    {
        METHOD(HttpMethod.Patch, path, middleware);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="middlewares"></param>
    public void Options(string path, params MiddlewareCallback[] middlewares)
    {
        METHOD(HttpMethod.Options, path, middlewares);
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
    /// <param name="middleware"></param>
    public void Use(MiddlewareCallback middleware)
    {
        _middlewares.Add(middleware);
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
    public void Use(string path, MiddlewareCallback middleware)
    {
//        _middlewares.Add(path, middleware);
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