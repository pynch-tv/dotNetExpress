using dotNetExpress.Delegates;
using dotNetExpress.Options;

namespace dotNetExpress.Middlewares
{
    public class ServeStatic
    {
        private string _root;
        private StaticOptions? _options;

        public ServeStatic(string root, StaticOptions? options)
        {
            _root = root;
            _options = options;
        }

        public MiddlewareCallback Middleware()
        {
            return Serve;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="req"></param>
        /// <param name="res"></param>
        /// <param name="next"></param>
        public void Serve(Request req, Response res, NextCallback? next = null)
        {
            Console.WriteLine($"in ServeStatic {_root}");

            next?.Invoke(null);
        }
    }
}
