using HTTPServer;

namespace dotNetExpress.examples
{
    internal class User
    {

        internal static void List(Request req, Response res, NextCallback? next)
        {
            res.send("todo");
        }

        internal static void View(Request req, Response res, NextCallback? next)
        {
            res.send("todo");
        }

        internal static void Edit(Request req, Response res, NextCallback? next)
        {
            res.send("todo");
        }

        internal static void Update(Request req, Response res, NextCallback? next)
        {
            res.send("todo");
        }

    }
}