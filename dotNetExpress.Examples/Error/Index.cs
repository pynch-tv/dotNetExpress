﻿namespace dotNetExpress.examples;

internal partial class Examples
{
    internal static async Task Error()
    {
        var app = new Express();
        const int port = 8080;

        app.Get("/", (req, res, next) => throw new Exception("broken"));

        app.Get("/next", (req, res, next) =>
        {
        });

        await app.Listen(port, () =>
        {
            Console.WriteLine($"Example app listening on port {port}");
        });
    }
}
