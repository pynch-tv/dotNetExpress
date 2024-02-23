namespace dotNetExpress.examples;

internal partial class Examples
{
    internal static void Middleware()
    {
        void middleware1(Request req, Response res, NextCallback? next = null)
        {
            next();
        }

        void middleware2(Request req, Response res, NextCallback? next = null)
        {
            next();
        }

        void middleware3(Request req, Response res, NextCallback? next = null)
        {
            next();
        }

        var app = new Express();
        const int port = 8080;

        // add a single middleware
        app.Use(middleware1);

        app.Use((req, res, next) =>
        {
            Console.WriteLine("hello");
            next();
        });

        app.Get("/", middleware2, null, middleware3);

        app.Listen(port, () =>
        {
            Console.WriteLine($"Example app listening on port {port}");
        });
    }
}
