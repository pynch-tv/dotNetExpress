using dotNetExpress;
using dotNetExpress.Delegates;
using dotNetExpress.Options;

namespace dotnetExpress.Middlewares.ServerStatic;

/// <summary>
/// Constructor
/// </summary>
/// <param name="root"></param>
/// <param name="options"></param>
public class ServeStatic(string root, StaticOptions options)
{
    private readonly string _root = root;

    private readonly StaticOptions _options = options ?? new();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="req"></param>
    /// <param name="res"></param>
    /// <param name="next"></param>
    public async Task Serve(Request req, Response res, NextCallback? next = null)
    {
        var resource = Path.Combine(_root, req.Path?.TrimStart('/'));
        if (File.Exists(resource))
        {
            res.SendFile(resource);
            return; // do not evaluate next
        }

        next?.Invoke(null);
    }
}