using dotNetExpress.Delegates;

namespace dotNetExpress.examples;
internal class Site
    {

        internal static void Index(Request req, Response res, NextCallback? next)
        {
            res.Send("todo");
        }

    }
