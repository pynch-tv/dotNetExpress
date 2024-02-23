namespace dotNetExpress.examples;

internal partial class Examples
{
    internal static void BodyParser()
    {
        var app = new Express();
        const int port = 8080;

//        app.Use(Express.Json());

        app.Post("/v1/servers/XT2/subscribe", Express.Json(),  (req, res, next) =>
        {
            res.Send("Hello World");
        });

        app.Listen(port, () =>
        {
            Console.WriteLine($"Example app listening on port {port}");
        });
    }
}
