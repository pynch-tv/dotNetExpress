using System.Net;

namespace dotNetExpress.examples;
internal partial class Examples
{
    internal static async Task Favicon()
    {
        var app = new Express();
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

        app.Get("/", async Task (req, res, next) => {
            await res.Send("Hello World!");
        });

        app.Get("/favicon.ico", async Task (req, res, next) =>
        {
            res.Status(HttpStatusCode.OK);
            res.Set("Content-Length", favicon.Length.ToString());
            res.Set("Content-Type", "image/x-icon");
        });

        await app.Listen(port, () =>
        {
            Console.WriteLine($"Example app listening on port {port}");
        });
    }
}
