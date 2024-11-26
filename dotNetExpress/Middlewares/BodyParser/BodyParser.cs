using System.Collections.Specialized;
using System.Text;
using dotNetExpress;
using dotNetExpress.Delegates;

namespace dotnetExpress.Middlewares.BodyParser;

public class BodyParser
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="req"></param>
    /// <param name="res"></param>
    /// <param name="next"></param>
    public static async Task ParseJson(Request req, Response res, NextCallback next = null)
    {
        if (null != req.Body)
        {   //  already parsed
            next?.Invoke(null);
            return;
        }

        if (null == req.Get("Content-Type"))
        {
            next?.Invoke(null);
            return;
        }

        if (req.Get("Content-Type")!.Equals("application/json", StringComparison.OrdinalIgnoreCase))
        {
            var contentLength = int.Parse(req.Get("Content-Length") ?? "0");
            if (contentLength > 0)
            {
                var sb = new byte[contentLength];
                req.StreamReader.ReadExactly(sb, 0, sb.Length);

                req.Body = Encoding.UTF8.GetString(sb);
            }
        }

        next?.Invoke(null);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="req"></param>
    /// <param name="res"></param>
    /// <param name="next"></param>
    public static async Task ParseText(Request req, Response res, NextCallback next = null)
    {
        if (null != req.Body)
        {
            //  already parsed
            next?.Invoke(null);
            return;
        }

        if (null == req.Get("Content-Type"))
        {
            next?.Invoke(null);
            return;
        }

        if (req.Get("Content-Type")!.Equals("application/json", StringComparison.OrdinalIgnoreCase))
        {
            var contentLength = int.Parse(req.Get("Content-Length") ?? "0");
        }

        next?.Invoke(null);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="req"></param>
    /// <param name="res"></param>
    /// <param name="next"></param>
    public static async Task ParseRaw(Request req, Response res, NextCallback next = null)
    {
        if (null != req.Body)
        {
            //  already parsed
            next?.Invoke(null);
            return;
        }

        if (null == req.Get("Content-Type"))
        {
            next?.Invoke(null);
            return;
        }

        if (req.Get("Content-Type")!.Equals("application/json", StringComparison.OrdinalIgnoreCase))
        {
            var contentLength = int.Parse(req.Get("Content-Length") ?? "0");
            if (contentLength > 0)
            {
                var sb = new byte[contentLength];
                var bytesRead = req.StreamReader.Read(sb, 0, sb.Length);
            }
        }

        next?.Invoke(null);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="req"></param>
    /// <param name="res"></param>
    /// <param name="next"></param>
    public static async Task ParseUrlEncoded(Request req, Response res, NextCallback next = null)
    {
        if (null != req.Body)
        {
            //  already parsed
            next?.Invoke(null);
            return;
        }

        if (null == req.Get("Content-Type"))
        {
            next?.Invoke(null);
            return;
        }

        if (req.Get("Content-Type")!.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
        {
            var contentLength = int.Parse(req.Get("Content-Length") ?? "0");
            if (contentLength > 0)
            {
                var sb = new byte[contentLength];
                req.StreamReader.ReadExactly(sb, 0, sb.Length);

                req.Body = new NameValueCollection();

                var body = Encoding.UTF8.GetString(sb);
                var bodyParts = body.Split("&");
                foreach (var bodyPart in bodyParts)
                {
                    var kvp = bodyPart.Split('=');
                    if (kvp.Length == 2)
                        req.Body[kvp[0]] = Uri.UnescapeDataString(kvp[1]);
                }


            }
        }

        next?.Invoke(null);
    }


}