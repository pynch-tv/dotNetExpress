using dotNetExpress.Delegates;

namespace dotNetExpress.examples;
internal class Site
    {

        internal static async Task Index(Request req, Response res, NextCallback? next)
        {
            await res.Send("todo");
        }

    }
