using dotNetExpress.Delegates;

namespace dotNetExpress.examples;

internal class User
{

    internal static async Task List(Request req, Response res, NextCallback? next)
    {
        await res.Send("todo");
    }

    internal static async Task View(Request req, Response res, NextCallback? next)
    {
        await res.Send("todo");
    }

    internal static async Task Edit(Request req, Response res, NextCallback? next)
    {
        await res.Send("todo");
    }

    internal static async Task Update(Request req, Response res, NextCallback? next)
    {
        await res.Send("todo");
    }

}