namespace dotNetExpress.examples
{
    internal partial class Examples
    {
        internal static void BodyParser()
        {
            var app = new HTTPServer.Express();
            const int port = 8080;

            //    app.use(Express.json());

            app.get("/", HTTPServer.Express.json());

            app.listen(port, () =>
            {
                Console.WriteLine($"Example app listening on port {port}");
            });
        }
    }
}
