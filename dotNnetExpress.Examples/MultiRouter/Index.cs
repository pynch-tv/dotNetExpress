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
