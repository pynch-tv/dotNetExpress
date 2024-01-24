namespace dotNetExpress.examples
{
    internal partial class Examples
    {

        internal static void RouteSeparation()
        {
            var app = new HTTPServer.Express();
            const int port = 8080;

            // General

            app.get("/", Site.Index);

            // User

            app.get("/users", User.List);
//            app.all("/user/:id/:op?", user.load);
            app.get("/user/:id", User.View);
            app.get("/user/:id/view", User.View);
            app.get("/user/:id/edit", User.Edit);
            app.put("/user/:id/edit", User.Update);

            // Posts

            app.get("/posts", Post.List);



            app.listen(port, () =>
            {
                Console.WriteLine($"Example app listening on port {port}");
            });
        }

    }
}