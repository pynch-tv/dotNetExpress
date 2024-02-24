using System.Net;

namespace dotNetExpress.examples;

internal partial class Examples
{

    internal static void Download()
    {
        var app = new Express();
        const int port = 8080;

        app.Get("/v1", (req, res, next) =>
        {
            var file = "d:/public/hello.txt";
            res.Download(file); // Set disposition and send it.
        });

        app.Listen(port, () =>
        {
            Console.WriteLine($"Example app listening on port {port}");
        });
    }
}