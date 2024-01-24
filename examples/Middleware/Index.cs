using HTTPServer;

namespace dotNetExpress.examples
{
    internal partial class Examples
    {
        internal static void Middleware()
        {
            void middleware1(Request req, Response res, NextCallback? next = null)
            {
                next();
            }

            void middleware2(Request req, Response res, NextCallback? next = null)
            {
                next();
            }

            void middleware3(Request req, Response res, NextCallback? next = null)
            {
                next();
            }

            var app = new HTTPServer.Express();
            const int port = 8080;

            // add a single middleware
            app.use(middleware1);

            app.use((req, res, next) =>
            {
                Console.WriteLine("hello");
                next();
            });

            app.get("/", middleware2, null, middleware3);

            app.listen(port, () =>
            {
                Console.WriteLine($"Example app listening on port {port}");
            });
        }
    }
}
