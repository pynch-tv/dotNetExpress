namespace dotNetExpress.examples
{
    internal partial class Examples
    {
        internal static void Error()
        {
            var app = new HTTPServer.Express();
            const int port = 8080;

            app.get("/", (req, res, next) => throw new Exception("broken"));

            app.get("/next", (req, res, next) =>
            {
                next?.Invoke(new Exception("BROKEN"));
            });

            app.listen(port, () =>
            {
                Console.WriteLine($"Example app listening on port {port}");
            });
        }
    }
}
