using HTTPServer;

namespace dotNetExpress.examples
{
    internal partial class Examples
    {
        internal static void MultiRouter()
        {
            var app = new HTTPServer.Express();
            const int port = 8080;

            var apiv1 = new Router();
            var apiv2 = new Router();

            app.use("/api/v1", apiv1);
            app.use("/api/v2", apiv2);

            apiv1.get("/", (req, res, next) =>
            {
                res.send("Hello World from api v1.");
            });

            apiv2.get("/", (req, res, next) =>
            {
                res.send("Hello World from api v2.");
            });


            app.get("/", (req, res, next) =>
            {
                res.send("Hello World from root route.");
            });

            app.listen(port, () =>
            {
                Console.WriteLine($"Example app listening on port {port}");
            });
        }
    }
}
