using System.Net;

namespace dotNetExpress.examples
{
    internal partial class Examples
    {
        internal static void Favicon()
        {
            var app = new HTTPServer.Express();
            const int port = 8080;

            var favicon = Convert.FromBase64String(
                "AAABAAEAEBAQAAAAAAAoAQAAFgAAACgAAAAQAAAAIAAAAAEABAAAAAAAgAA" +
                "AAAAAAAAAAAAAEAAAAAAAAAAAAAAA/" +
                "4QAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAERE" +
                "QAAAAAAEAAAEAAAAAEAAAABAAAAEAAAAAAQAAAQAAAAABAAAAAAAAAAAAAA" +
                "AAAAAAAAAAAAEAABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
                "AAAAAAAAAAAAAD//wAA//8AAP//AAD8HwAA++8AAPf3AADv+wAA7/sAAP//" +
                "AAD//wAA+98AAP//AAD//wAA//8AAP//AAD//wAA");

            app.get("/", (req, res, next) => {
                res.send("Hello World!");
            });

            app.get("/favicon.ico", (req, res, next) =>
            {
                res.status(HttpStatusCode.OK);
                res.set("Content-Length", favicon.Length.ToString());
                res.set("Content-Type", "image/x-icon");
            });

            app.listen(port, () =>
            {
                Console.WriteLine($"Example app listening on port {port}");
            });
        }
    }
}
