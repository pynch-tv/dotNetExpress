namespace dotNetExpress.examples;

internal partial class Examples
{

    internal static void CookieSession()
    {
        var app = new Express();
        const int port = 8080;

        // add req.session cookie support
        app.Use(cookieSession({ secret: 'manny is cool' }));

        app.Get("/", (req, res, next) =>
        {
            req.Session.count = (req.Session.count || 0) + 1
            res.Send('viewed ' + req.Session.count + ' times\n')
        });

        app.Listen(port, () =>
        {
            Console.WriteLine($"Example app listening on port {port}");
        });
    }
}
