using System.Collections.Specialized;

namespace HTTPServer;

public class Request
{
    /// <summary>
    /// Contains a string corresponding to the HTTP method of the request: GET, POST, PUT, and so on.
    /// </summary>
    public HttpMethod Method;

    /// <summary>
    /// Contains the host derived from the Host HTTP header.
    /// </summary>
    public string Host;

    /// <summary>
    /// Contains the host derived from the HostName HTTP header.
    /// </summary>
    public string HostName;

    /// <summary>
    /// This property is much like req.url; however, it retains the original request URL,
    /// allowing you to rewrite req.url freely for internal routing purposes. For example,
    /// the “mounting” feature of app.use() will rewrite req.url to strip the mount point.
    /// </summary>
    public string OriginalUrl;

    /// <summary>
    /// The URL path on which a router instance was mounted.
    /// The req.baseUrl property is similar to the mountpath property of the app object,
    /// except app.mountpath returns the matched path pattern(s).
    /// </summary>
    public string BaseUrl;

    /// <summary>
    /// Contains the path part of the request URL.
    /// </summary>
    public string Path;

    /// <summary>
    /// This property is an object containing a property for each query string parameter
    /// in the route. When query parser is set to disabled, it is an empty object {},
    /// otherwise it is the result of the configured query parser.
    /// </summary>
    public string Query = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    public string Protocol;

    /// <summary>
    /// 
    /// </summary>
    private readonly NameValueCollection _headers = new();

    /// <summary>
    /// This property is an object containing properties mapped to the named route “parameters”.
    /// For example, if you have the route /user/:name, then the “name” property is available
    /// as req.params.name. This object defaults to {}.
    /// </summary>
    public NameValueCollection Params = new();

    public string? get(string key) => _headers[key];

    /// <summary>
    /// 
    /// </summary>
    /// <param name="headerLines"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    public static bool TryParse(string[] headerLines, out Request request)
    {
        request = new Request();

        var requestLine = headerLines[0];
        var requestLineParts = requestLine.Split(' ');

        if (!Enum.TryParse(requestLineParts[0], out request.Method))
            return false;

        request.OriginalUrl = requestLineParts[1];
        var idx = request.OriginalUrl.LastIndexOf('?');
        if (idx > -1)
        {
            request.Query = request.OriginalUrl[(idx + 1)..];
            request.Path = request.OriginalUrl[..idx];
        }
        else
            request.Path = request.OriginalUrl;

        idx = requestLineParts[2].IndexOf('/');
        request.Protocol = requestLineParts[2][..idx].ToLower();

        request._headers.Clear();
        for (var i = 1; i < headerLines.Length; i++)
        {
            var headerLine = headerLines[i];
            var headerPair = headerLine.Split(":", 2, StringSplitOptions.TrimEntries);
            if (headerPair.Length != 2) continue; // basic checking
            // header in case insensitive (see 
            request._headers.Add(headerPair[0].ToLower(), headerPair[1]);
        }

        request.Host = request._headers["host"];
        request.HostName = request.Host.Split(':')[0];

        return true;
    }
}