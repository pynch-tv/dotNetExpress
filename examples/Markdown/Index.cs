using System.Collections.Specialized;

namespace dotNetExpress.examples;

internal partial class Examples
{
    internal static void Markdown()
    {
        var app = new Express();
        const int port = 8080;

        var __dirname = Directory.GetCurrentDirectory();

        //app.Engine("md", path =>
        //{
        //    var text = File.ReadAllText("textFile");
        //    //    var html = marked.parse(str).replace(/\{ ([^}]+)\}/g, function(_, name)
        //});

        app.Set("views", Path.Combine(__dirname, "views"));

        // make it the default, so we don't need .md
        app.Set("view engine", "md");

        app.Get("/", (req, res, next) =>
        {
            res.Render("index", new NameValueCollection() { { "title", "Markdown Example" } });
        });

        app.Listen(port, () =>
        {
            Console.WriteLine($"Example app listening on port {port}");
        });
    }
}
