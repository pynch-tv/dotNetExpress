using HTTPServer;

namespace dotNetExpress.examples
{
    internal class Post
    {

        internal static void List(Request req, Response res, NextCallback? next)
        {
            res.send("todo");
        }

    }
}