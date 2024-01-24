namespace dotNetExpress.examples
{
    internal partial class Examples
    {
        internal static void BasicRouting()
        {
            var app = new HTTPServer.Express();
            const int port = 8080;

            app.get("/", (req, res, next) =>
            {
                res.send("Hello World!");
            });

            app.post("/", (req, res, next) =>
            {
                res.send("Got a POST request");
            });

            app.put("/user", (req, res, next) =>
            {
                res.send("Got a POST request");
            });

            app.remove("/user", (req, res, next) =>
            {
                res.send("Got a POST request");
            });

            app.listen(port, () =>
            {
                Console.WriteLine($"Example app listening on port {port}");
            });
        }
    }
}
