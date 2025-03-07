using dotNetExpress.Delegates;

namespace dotNetExpress.examples;

internal class Post
    {

        internal static async Task List(Request req, Response res, NextCallback? next)
        {
            await res.Send("todo");
        }

    }
