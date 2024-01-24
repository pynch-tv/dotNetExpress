using System.Collections.Specialized;

namespace dotNetExpress.examples
{
    internal partial class Examples
    {
        internal static void Markdown()
        {
            var app = new HTTPServer.Express();
            const int port = 8080;

            var __dirname = Directory.GetCurrentDirectory();

            app.engine("md", path =>
            {
                var text = File.ReadAllText("textFile");
                //    var html = marked.parse(str).replace(/\{ ([^}]+)\}/g, function(_, name)
            });

            app.set("views", Path.Combine(__dirname, "views"));

            // make it the default, so we don't need .md
            app.set("view engine", "md");

            app.get("/", (req, res, next) =>
            {
                res.render("index", new NameValueCollection() { { "title", "Markdown Example" } });
            });

            app.listen(port, () =>
            {
                Console.WriteLine($"Example app listening on port {port}");
            });
        }
    }
}
