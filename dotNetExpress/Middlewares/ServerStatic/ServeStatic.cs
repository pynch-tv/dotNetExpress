using System.Diagnostics;
using System.Net;
using dotNetExpress;
using dotNetExpress.Delegates;
using dotNetExpress.Options;

namespace dotnetExpress.Middlewares.ServerStatic;

/// <summary>
/// Constructor
/// </summary>
/// <param name="root"></param>
/// <param name="options"></param>
public class ServeStatic
{
    private readonly string _root;

    private readonly SendFileOptions? sendFileOptions;

    private readonly StaticOptions? _options;

    private bool _fallthrough;

    private bool _redirect;

    private string _setHeaders;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="root"></param>
    /// <param name="options"></param>
    public ServeStatic(string root, StaticOptions? options)
    {
        if (string.IsNullOrEmpty(root))
            throw new ArgumentNullException(nameof(root), "Root directory cannot be null or empty.");

        // copy options object
        var opts = new StaticOptions(options ?? null);

        // fall-though
        _fallthrough = opts.Fallthrough;

        // default redirect
        _redirect = opts.Redirect;

        // headers listener
        _setHeaders = opts.SetHeaders;

        _root = Path.Combine(root);

        // construct directory listener ???
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="req"></param>
    /// <param name="res"></param>
    /// <param name="next"></param>
    public async Task Serve(Request req, Response res, NextCallback? next = null)
    {
        if (req.Method != HttpMethod.Get && req.Method != HttpMethod.Head)
        {
            if (_fallthrough)
            {
                next?.Invoke(null);
                return;
            }

            // method not allowed
            res.Set("Allow", "GET, HEAD");
            res.Set("Content-Length", "0");
            await res.SendStatus(HttpStatusCode.MethodNotAllowed);
            await res.End();

            return;
        }

        string path = req.OriginalUrl.StartsWith(req.BaseUrl) ? req.OriginalUrl[req.BaseUrl.Length..] : req.OriginalUrl;

        // strip leading / from path
        var relativePath = path?.TrimStart('/') ?? string.Empty;
        // Concat with root directory
        var absolutePath = Path.Combine(_root, relativePath);

        Debug.WriteLine($"===========================> ServeStatic: {relativePath}");

//        if (File.Exists(absolutePath))
        {
            await res.SendFile(absolutePath);
            return; // do not evaluate next
        }

//        next?.Invoke(null);
    }
}
