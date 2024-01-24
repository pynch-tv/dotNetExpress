namespace HTTPServer;

// ReSharper disable InconsistentNaming
internal class Route
{
    public HttpMethod Method { get; }
    public string Path { get; }
    public List<MiddlewareCallback?> Middlewares { get; }

    public List<string> Params = new();

    public Route(HttpMethod method, string path, List<MiddlewareCallback> middlewares)
    {
        Method = method;
        Path = path;
        Middlewares = middlewares;
    }
}