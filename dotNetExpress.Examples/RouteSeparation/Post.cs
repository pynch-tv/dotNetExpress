using dotNetExpress.Delegates;

namespace dotNetExpress.examples;

internal class Post
    {

        internal static void List(Request req, Response res, NextCallback? next)
        {
            res.Send("todo");
        }

    }
