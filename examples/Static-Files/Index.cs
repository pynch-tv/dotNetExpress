namespace dotNetExpress.examples;

internal partial class Examples
{
    internal static void StaticFiles()
    {
        var app = new Express();
        const int port = 8080;

        var __dirname = Directory.GetCurrentDirectory();

        // express on its own has no notion
        // of a "file". The express.static()
        // middleware checks for a file matching
        // the `req.path` within the directory
        // that you pass it. In this case "GET /js/app.js"
        // will look for "./public/js/app.js".
        app.Use(Express.Static(Path.Combine(__dirname, "public"), new StaticOptions { Immutable = false}));

        // if you wanted to "prefix" you may use
        // the mounting feature of Connect, for example
        // "GET /static/js/app.js" instead of "GET /js/app.js".
        // The mount-path "/static" is simply removed before
        // passing control to the express.static() middleware,
        // thus it serves the file correctly by ignoring "/static"
        app.Use("/static", Express.Static(Path.Combine(__dirname, "public")));

        // if for some reason you want to serve files from
        // several directories, you can use express.static()
        // multiple times! Here we're passing "./public/css",
        // this will allow "GET /style.css" instead of "GET /css/style.css":
        app.Use(Express.Static(Path.Combine(__dirname, "public", "css")));

        app.Listen(port, () =>
        {
            Console.WriteLine($"Example app listening on port {port}");
        });
    }
}
