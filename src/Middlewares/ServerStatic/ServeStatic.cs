using dotNetExpress;
using dotNetExpress.Delegates;
using dotNetExpress.Options;
using System.IO;

namespace Pynch.Nexa.Tools.Express.Middlewares.ServerStatic;

public class ServeStatic
{
    private readonly string _root;

    private readonly StaticOptions _options;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="root"></param>
    /// <param name="options"></param>
    public ServeStatic(string root, StaticOptions options)
    {
        _root = root;
        _options = options ?? new();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="req"></param>
    /// <param name="res"></param>
    /// <param name="next"></param>
    public void Serve(Request req, Response res, NextCallback next = null)
    {

        var resource = Path.Combine(_root, req.Path.TrimStart('/'));
        if (File.Exists(resource))
        {
            res.SendFile(resource);
            return; // do not evaluate next
        }

        next?.Invoke(null);
    }
}