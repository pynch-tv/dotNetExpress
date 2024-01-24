namespace dotNetExpress.examples
{
    internal partial class Examples
    {
        internal static void HelloWorld()
        {
            var app = new HTTPServer.Express();
            const int port = 8080;

            app.get("/", (req, res, next) =>
            {
                res.send("Hello World");
            });

            app.listen(port, () =>
            {
                Console.WriteLine($"Example app listening on port {port}");
            });
        }
    }
}
