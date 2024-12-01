![example workflow](https://github.com/lathoub/dotNetExpress/actions/workflows/dotnet.yml/badge.svg)
# .net Express

.net Express is a minimal and flexible .NET 8 web application framework that provides a robust set of features for web and mobile applications.

## Key features
- same look and feel as node.js Express
- fast
- use WebSockets and SSE over the same port

## Getting started

### Hello world example

```cs
static void HelloWorld()
{
    var app = new Express();
    const int port = 8080;

    app.Get("/", (req, res, next) =>
    {
        res.Send("Hello World");
    });

    app.Listen(port, () =>
    {
        Console.WriteLine($"Example app listening on port {port}");
    });
}
```

### Multirouter example

Demonstrates multiple routes, use of Params and Query

```cs
namespace dotNetExpress.examples;

internal partial class Examples
{
    internal static async Task MultiRouter()
    {
        var app = new Express();
        const int port = 8080;

        var apiv1 = new Router();
        var apiv2 = new Router();

        app.Use("/api/v1", apiv1);
        app.Use("/api/v2", apiv2);

        apiv1.Get("/", async Task (req, res, next) =>
        {
            var serverId = req.Params["serverId"];
            Console.WriteLine($"serverId {serverId}");

            var sLimit = req.Query["limit"];
            var sOffset = req.Query["offset"];
            Console.WriteLine($"Pagination {sOffset} {sLimit}");

            await res.Send("Hello World from api v1.");
        });

        apiv2.Get("/", async Task (req, res, next) =>
        {
            await res.Send("Hello World from api v2.");
        });


        app.Get("/", async Task (req, res, next) =>
        {
            await res.Send("Hello World from root route.");
        });

        await app.Listen(port, () =>
        {
            Console.WriteLine($"Example app listening on port {port}");
        });
    }
}
```

More examples in the [Examples](https://github.com/lathoub/dotNetExpress/tree/main/dotnetExpress.Examples) folder

## Optional Dependencies

For Multipart POST commands

- Multipart Parser from [Http-Multipart-Data-Parser](https://github.com/Http-Multipart-Data-Parser/Http-Multipart-Data-Parser)
- Microsoft.IO.RecyclableMemoryStream
