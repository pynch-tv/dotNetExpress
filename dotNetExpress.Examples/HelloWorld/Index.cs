namespace dotNetExpress.examples;

internal partial class Examples
{
    internal static async Task HelloWorld()
    {
        var app = new Express();
        const int port = 8080;

        app.Get("/", (req, res, next) =>
        {
            res.Send("Hello World");
        });

        await app.Listen(port, () =>
        {
            Console.WriteLine($"Example app listening on port {port}");
        });
    }
}