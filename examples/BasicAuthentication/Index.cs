using System.Collections.Specialized;
using System.Net;
using System.Text;
using dotNetExpress.Delegates;
using dotNetExpress.Exceptions;

namespace dotNetExpress.examples;

internal partial class Examples
{
    private class BasicAuth
    {
        private static NameValueCollection _users = new();

        private static bool _challenge;

        private const string Basic = "Basic ";

        internal static MiddlewareCallback BasicAuthentication()
        {
            NameValueCollection users = new() { { "admin", "supersecret123" } };
            const string basic = "Basic ";

            return DoBasicAuthentication;

            // Role-Based Access Control 
            async Task DoBasicAuthentication(Request req, Response res, NextCallback next)
            {
                var basicAuth = req.Get("authorization");
                if (!string.IsNullOrEmpty(basicAuth))
                {
                    var authenticated = false;
                    if (basicAuth.StartsWith(basic))
                    {
                        basicAuth = basicAuth[basic.Length..];
                        basicAuth.Trim();

                        foreach (string name in users)
                        {
                            var sum = name + ":" + users[name];

                            var data = Convert.FromBase64String(basicAuth);
                            var decodedString = Encoding.UTF8.GetString(data);

                            if (decodedString == sum)
                            {
                                authenticated = true;
                                break;
                            }
                        }
                    }

                    if (!authenticated)
                    {
                        if (true) // challenge
                            res.Set("WWW-Authenticate", "Basic");
                        next(new ExpressException(HttpStatusCode.Unauthorized, "Your request lacks valid authentication credentials", "Provided username and password are incorrect."));
                    }
                }
                else
                {
                    res.Set("WWW-Authenticate", "Basic");
                    next(new ExpressException(HttpStatusCode.Unauthorized, "Your request lacks authentication credentials", "Basic Authentication information is missing"));
                }

                next();
            }
        }

    }

    internal static async Task BasicAuthentication()
    {
        var app = new Express();
        const int port = 8080;

        app.Use(BasicAuth.BasicAuthentication());

        app.Get("/v1", async Task (req, res, next) =>
        {
            await res.Send("Hello World");
        });

        await app.Listen(port, () =>
        {
            Console.WriteLine($"Example app listening on port {port}");
        });
    }
}
