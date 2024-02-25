using System;
using System.Text;
using dotNetExpress.Delegates;

namespace dotNetExpress.Middlewares
{
    public class BodyParser
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="req"></param>
        /// <param name="res"></param>
        /// <param name="next"></param>
        public static void ParseJson(Request req, Response res, NextCallback? next = null)
        {
            if (req.Body is { Length: > 0 })
            {   //  already parsed
                next?.Invoke(null);
                return;
            }

            if (req.Get("content-type")!.Equals("application/json", StringComparison.OrdinalIgnoreCase))
            {
                var contentLength = int.Parse(req.Get("Content-Length") ?? string.Empty);

                var sb = new byte[contentLength];
                req.StreamReader.ReadExactly(sb, 0, sb.Length);

                req.Body = Encoding.UTF8.GetString(sb);
            }

            next?.Invoke(null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="req"></param>
        /// <param name="res"></param>
        /// <param name="next"></param>
        public static void ParseText(Request req, Response res, NextCallback? next = null)
        {
            if (req.Body is { Length: > 0 })
            {
                //  already parsed
                next?.Invoke(null);
                return;
            }

            if (req.Get("content-type")!.Equals("application/json", StringComparison.OrdinalIgnoreCase))
            {
                var contentLength = int.Parse(req.Get("Content-Length") ?? string.Empty);
            }

            next?.Invoke(null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="req"></param>
        /// <param name="res"></param>
        /// <param name="next"></param>
        public static void ParseRaw(Request req, Response res, NextCallback? next = null)
        {
            if (req.Body is { Length: > 0 })
            {
                //  already parsed
                next?.Invoke(null);
                return;
            }

            if (req.Get("content-type")!.Equals("application/json", StringComparison.OrdinalIgnoreCase))
            {
                var contentLength = int.Parse(req.Get("Content-Length") ?? string.Empty);

                var sb = new byte[contentLength];
                var bytesRead = req.StreamReader.Read(sb, 0, sb.Length);
            }

            next?.Invoke(null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="req"></param>
        /// <param name="res"></param>
        /// <param name="next"></param>
        public static void ParseUrlencoded(Request req, Response res, NextCallback? next = null)
        {
            if (req.Body is { Length: > 0 })
            {
                //  already parsed
                next?.Invoke(null);
                return;
            }

            if (req.Get("content-type")!.Equals("application/json", StringComparison.OrdinalIgnoreCase))
            {
                var contentLength = int.Parse(req.Get("Content-Length") ?? string.Empty);
            }

            next?.Invoke(null);
        }
    }
}
