using dotNetExpress.Delegates;

namespace dotNetExpress;

internal class Route
{
    public HttpMethod Method { get; }
    public string Path { get; }
    public MiddlewareCallback?[] Middlewares { get; }

    public List<string> Params = new();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="method"></param>
    /// <param name="path"></param>
    /// <param name="middlewares"></param>
    public Route(HttpMethod method, string path, MiddlewareCallback[] middlewares)
    {
        Method = method;
        Path = path;
        Middlewares = middlewares;
    }
}