namespace dotNetExpress.examples;

internal partial class Examples
{

    internal static void RouteSeparation()
    {
        var app = new Express();
        const int port = 8080;

        // General

        app.Get("/", Site.Index);

        // User

        app.Get("/users", User.List);
//            app.all("/user/:id/:op?", user.load);
        app.Get("/user/:id", User.View);
        app.Get("/user/:id/view", User.View);
        app.Get("/user/:id/edit", User.Edit);
        app.Put("/user/:id/edit", User.Update);

        // Posts

        app.Get("/posts", Post.List);

        app.Listen(port, () =>
        {
            Console.WriteLine($"Example app listening on port {port}");
        });
    }

}
