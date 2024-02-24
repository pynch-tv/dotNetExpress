using System.Collections.Specialized;
using System.Net;
using dotNetExpress.Options;

namespace dotNetExpress.examples
{
    internal partial class Examples
    {

        internal static void SendFile()
        {
            var app = new Express();
            const int port = 8080;

            app.Get("/v1", (req, res, next) =>
            {
                res.Status(HttpStatusCode.OK).End();
            });

            app.Get("/:file", (req, res, next) =>
            {
                var options = new SendFileOptions();
                options.Root = Path.Combine("d:/", "public");
                options.DotFiles = "deny";
                options.Headers = new NameValueCollection { { "x-timestamp", "" }, { "x-sent", "true" } };

                var fileName = req.Params["file"];
                res.SendFile(fileName ?? string.Empty, options);
            });

            app.Listen(port, () =>
            {
                Console.WriteLine($"Example app listening on port {port}");
            });
        }
    }
}