using static System.Net.Mime.MediaTypeNames;

namespace dotNetExpress.examples
{
    internal partial class Examples
    {

        internal static void HelloWorld()
        {
            var app = new Express();
            const int port = 8080;

            app.Get("/", (req, res, next) =>
            {
                res.Send("Hello World");
            });

            //app.All("*", (req, res, next) => {
            //    res.Redirect("http://www.mysite.com/");
            //});

            app.Listen(port, () =>
            {
                Console.WriteLine($"Example app listening on port {port}");
            });
        }
    }
}
