using HTTPServer;

namespace dotNetExpress.examples
{
    internal class Site
    {

        internal static void Index(Request req, Response res, NextCallback? next)
        {
            res.send("todo");
        }

    }
}