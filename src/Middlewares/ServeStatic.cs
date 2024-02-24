using dotNetExpress.Delegates;
using dotNetExpress.Options;

namespace dotNetExpress.Middlewares
{
    public class ServeStatic
    {
        private readonly string _root;

        private readonly StaticOptions _options;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="root"></param>
        /// <param name="options"></param>
        public ServeStatic(string root, StaticOptions? options)
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
        public void Serve(Request req, Response res, NextCallback? next = null)
        {
            Console.WriteLine($"in ServeStatic {_root} {_options.Fallthrough}");

            next?.Invoke(null);
        }
    }
}
