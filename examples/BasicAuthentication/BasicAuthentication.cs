using HTTPServer;
using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace dotNetExpress.examples
{

    internal partial class Examples
    {
        private class BasicAuth
        {
            private static NameValueCollection _users = new();

            private static bool _challenge;


            public static MiddlewareCallback basicAuth(NameValueCollection users, bool challenge = true)
            {
                _users = users;
                _challenge = challenge;

                return auth;
            }

            private static void auth(Request req, Response res, NextCallback next = null)
            {
                var basicAuth = req.get("authorization");

                if (!string.IsNullOrEmpty(basicAuth))
                {

                    var authenticated = false;
                    if (basicAuth.StartsWith("Basic"))
                    {
                        basicAuth = basicAuth.Substring(6);
                        basicAuth.Trim();

                        foreach (string name in _users)
                        {
                            var sum = name + ":" + _users[name];

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
                        if (_challenge)
                            res.set("WWW-Authenticate", "Basic");
                        res.status(HttpStatusCode.Unauthorized);
                        return;
                    }
                }

                next();
            }
        }

        private static void BasicAuthentication()
        {
            var app = new HTTPServer.Express();
            const int port = 8080;

            NameValueCollection users = new() { { "admin", "supersecret123" } };
            app.use(BasicAuth.basicAuth(users));

            app.get("/", (req, res, next) =>
            {
                res.send("Hello World");
            });

            app.listen(port, () =>
            {
                Console.WriteLine($"Example app listening on port {port}");
            });
        }
    }
}
