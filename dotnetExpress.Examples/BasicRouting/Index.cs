namespace dotNetExpress.examples;

internal partial class Examples
{
    internal static async Task BasicRouting()
    {
        var app = new Express();
        const int port = 8080;

        app.Get("/", async Task (req, res, next) =>
        {
            await res.Send("Hello World!");
        });

        app.Post("/", async Task (req, res, next) =>
        {
            await res.Send("Got a POST request");
        });

        app.Put("/user", async Task (req, res, next) =>
        {
            await res.Send("Got a POST request");
        });

        app.Delete("/user", async Task (req, res, next) =>
        {
            await res.Send("Got a POST request");
        });

        await app.Listen(port, () =>
        {
            Console.WriteLine($"Example app listening on port {port}");
        });
    }
}
