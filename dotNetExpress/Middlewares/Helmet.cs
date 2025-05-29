using dotNetExpress.Delegates;

namespace dotNetExpress.Middlewares;

public class Helmet 
{
    static readonly Dictionary<string, string> _defaults = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public static MiddlewareCallback helmet(dynamic? options = null)
    {
        _defaults.Add("Content-Security-Policy", "default-src 'self';base-uri 'self';font-src 'self' https: data:;form-action 'self';frame-ancestors 'self';img-src 'self' data:;object-src 'none';script-src 'self';script-src-attr 'none';style-src 'self' https: 'unsafe-inline';upgrade-insecure-requests");
        _defaults.Add("Cross-Origin-Opener-Policy", "same-origin");
        _defaults.Add("Cross-Origin-Resource-Policy", "same-origin");
        _defaults.Add("Origin-Agent-Cluster", "?1");
        _defaults.Add("Referrer-Policy", "no-referrer");
        _defaults.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
        _defaults.Add("X-Content-Type-Options", "nosniff");
        _defaults.Add("X-DNS-Prefetch-Control", "off");
        _defaults.Add("X-Download-Options", "noopen");
        _defaults.Add("X-Frame-Options", "SAMEORIGIN");
        _defaults.Add("X-Permitted-Cross-Domain-Policies", "none");
        _defaults.Add("X-XSS-Protection", "0");

        return Checker;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="req"></param>
    /// <param name="res"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    private static async Task Checker(Request req, Response res, NextCallback? next)
    {
        res.Set(_defaults);

        next?.Invoke();
    }
}

