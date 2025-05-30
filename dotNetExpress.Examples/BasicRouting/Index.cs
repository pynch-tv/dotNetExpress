namespace dotNetExpress.examples;

internal partial class Examples
{
    internal static async Task BasicRouting()
    {
        var app = new Express();
        const int port = 8080;

        app.Get("/",  (req, res, next) =>
        {
            res.Send("Hello World!");
        });

        app.Post("/", (req, res, next) =>
        {
            res.Send("Got a POST request");
        });

        app.Put("/user", (req, res, next) =>
        {
            res.Send("Got a POST request");
        });

        app.Delete("/user", (req, res, next) =>
        {
            res.Send("Got a POST request");
        });

        await app.Listen(port, () =>
        {
            Console.WriteLine($"Example app listening on port {port}");
        });
    }
}
