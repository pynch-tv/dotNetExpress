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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="req"></param>
        /// <param name="res"></param>
        /// <param name="next"></param>
        public static void Serve(Request req, Response res, NextCallback? next = null)
        {
            Console.WriteLine("in ServeStatic");

            next?.Invoke(null);
        }
    }
}
