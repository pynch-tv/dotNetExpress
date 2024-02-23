using System.Collections.Specialized;
using System.Net;

namespace dotNetExpress;

public class Request
{
    private readonly NameValueCollection _headers = new();

    internal StreamReader StreamReader;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="app"></param>
    public Request(Express app)
    {
        this.App = app;
    }

    #region Properties

    /// <summary>
    /// This property holds a reference to the instance of the Express application that is using the middleware.
    /// res.app is identical to the req.app property in the request object.
    /// </summary>
    /// <returns></returns>
    public Express App { get; private set; }
    
    /// <summary>
    /// The URL path on which a router instance was mounted.
    /// The req.baseUrl property is similar to the mountpath property of the app object,
    /// except app.mountpath returns the matched path pattern(s).
    /// </summary>
    public string BaseUrl;

    /// <summary>
    /// Contains key-value pairs of data submitted in the request body. By default,
    /// it is undefined, and is populated when you use body-parsing middleware such as
    /// express.json() or express.urlencoded().
    /// </summary>
    public string? Body = null;

    /// <summary>
    /// When using cookie-parser middleware, this property is an object that contains
    /// cookies sent by the request. If the request contains no cookies, it defaults to {}.
    /// </summary>
    public string Cookies = "";

    /// <summary>
    /// When the response is still “fresh” in the client’s cache true is returned, otherwise\
    /// false is returned to indicate that the client cache is now stale and the full response
    /// should be sent.
    ///
    /// When a client sends the Cache-Control: no-cache request header to indicate an end-to-end
    /// reload request, this module will return false to make handling these requests transparent.
    ///
    /// Further details for how cache validation works can be found in the HTTP/1.1 Caching Specification.
    /// </summary>
    public bool Fresh;

    /// <summary>
    /// Contains the host derived from the HostName HTTP header.
    /// </summary>
    public string Hostname;

    /// <summary>
    /// Contains the remote IP address of the request.
    ///
    /// When the trust proxy setting does not evaluate to false, the value of this property is
    /// derived from the left-most entry in the X-Forwarded-For header. This header can be set
    /// by the client or by the proxy.
    /// </summary>
    public IPAddress Ip;

    /// <summary>
    /// When the trust proxy setting does not evaluate to false, this property contains an
    /// array of IP addresses specified in the X-Forwarded-For request header. Otherwise, it
    /// contains an empty array. This header can be set by the client or by the proxy.
    ///
    /// For example, if X-Forwarded-For is client, proxy1, proxy2, req.ips would be
    /// ["client", "proxy1", "proxy2"], where proxy2 is the furthest downstream.
    /// </summary>
    public IPAddress[] Ips;

    /// <summary>
    /// Contains a string corresponding to the HTTP method of the request: GET, POST, PUT, and so on.
    /// </summary>
    public HttpMethod Method;

    /// <summary>
    /// This property is much like req.url; however, it retains the original request URL,
    /// allowing you to rewrite req.url freely for internal routing purposes. For example,
    /// the “mounting” feature of app.use() will rewrite req.url to strip the mount point.
    /// </summary>
    public string OriginalUrl;

    /// <summary>
    /// This property is an object containing properties mapped to the named route “parameters”.
    /// For example, if you have the route /user/:name, then the “name” property is available
    /// as req.params.name. This object defaults to {}.
    /// </summary>
    public NameValueCollection Params = new();

    /// <summary>
    /// Contains the path part of the request URL.
    /// </summary>
    public string Path;

    /// <summary>
    /// Contains the request protocol string: either http or (for TLS requests) https.
    ///
    /// When the trust proxy setting does not evaluate to false, this property will use
    /// the value of the X-Forwarded-Proto header field if present. This header can be
    /// set by the client or by the proxy.
    /// </summary>
    public string Protocol;

    /// <summary>
    /// This property is an object containing a property for each query string parameter
    /// in the route. When query parser is set to disabled, it is an empty object {},
    /// otherwise it is the result of the configured query parser.
    /// </summary>
    public string Query = string.Empty;

    /// <summary>
    /// This property holds a reference to the response object that relates to this request object.
    /// </summary>
    public Response Res;

    /// <summary>
    /// Contains the currently-matched route, a string.
    /// </summary>
    public dynamic Route;

    /// <summary>
    /// A Boolean property that is true if a TLS connection is established.
    /// Equivalent to: req.protocol == "https"
    /// </summary>
    public bool Secure => Protocol.Equals("https");

    /// <summary>
    /// When using cookie-parser middleware, this property contains signed cookies sent by the request,
    /// unsigned and ready for use. Signed cookies reside in a different object to show developer intent;
    /// otherwise, a malicious attack could be placed on req.cookie values (which are easy to spoof).
    /// Note that signing a cookie does not make it “hidden” or encrypted; but simply prevents tampering
    /// (because the secret used to sign is private).
    /// </summary>
    public int SignedCookies;

    /// <summary>
    /// Indicates whether the request is “stale,” and is the opposite of req.fresh.
    /// For more information, see req.fresh.
    /// </summary>
    public bool Stale => !Fresh;

    /// <summary>
    /// An array of subdomains in the domain name of the request.
    /// </summary>
    public string[] Subdomains;

    /// <summary>
    /// A Boolean property that is true if the request’s X-Requested-With header field is “XMLHttpRequest”,
    /// indicating that the request was issued by a client library such as jQuery.
    /// </summary>
    public bool Xhr;

    #endregion

    #region Methods

    /// <summary>
    /// Checks if the specified content types are acceptable, based on the request’s Accept HTTP header field.
    /// The method returns the best match, or if none of the specified content types is acceptable,
    /// returns false (in which case, the application should respond with 406 "Not Acceptable").
    /// </summary>
    /// <returns></returns>
    public string Accepts()
    {
        return "";
    }

    /// <summary>
    /// Returns the first accepted charset of the specified character sets, based on the request’s
    /// Accept-Charset HTTP header field. If none of the specified charsets is accepted, returns false.
    /// </summary>
    /// <param name="charsSet"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public string AcceptsCharsets(string charsSet)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Returns the first accepted encoding of the specified encodings, based on the request’s
    /// Accept-Encoding HTTP header field. If none of the specified encodings is accepted, returns false.
    /// </summary>
    /// <param name="encoding"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public string AcceptsEncodings(string encoding)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Returns the first accepted language of the specified languages, based on the request’s
    /// Accept-Language HTTP header field. If none of the specified languages is accepted, returns false.
    /// </summary>
    /// <param name="encoding"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public string AcceptsLanguages(string lang)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Returns the specified HTTP request header field (case-insensitive match).
    /// The Referrer and Referer fields are interchangeable.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public string? Get(string key) => _headers[key];

    /// <summary>
    /// Returns the matching content type if the incoming request’s “Content-Type” HTTP header
    /// field matches the MIME type specified by the type parameter. If the request has no body,
    /// returns null. Returns false otherwise.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public string Is(string type)
    {
        return "";
    }

    /// <summary>
    /// Deprecated. Use either req.params, req.body or req.query, as applicable.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public string Param(string name, string defaultValue)
    {
        return "";
    }

    /// <summary>
    /// Range header parser.
    ///
    /// The size parameter is the maximum size of the resource.
    ///
    /// The options parameter is an object that can have the following properties.
    /// </summary>
    /// <returns></returns>
    public Range Range(int size, RangeOptions options = null)
    {
        return new Range();
    }

    #endregion

    /// <summary>
    /// 
    /// </summary>

    /// <summary>
    /// 
    /// </summary>
    /// <param name="app"></param>
    /// <param name="headerLines"></param>
    /// <param name="client"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    internal static bool TryParse(Express app, string[] headerLines, out Request request)
    {
        // create out variable
        request = new Request(app);

        #region First line : Method url Protocol
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
        #endregion

        #region Headers
        request._headers.Clear();
        for (var i = 1; i < headerLines.Length; i++)
        {
            var headerLine = headerLines[i];
            var headerPair = headerLine.Split(":", 2, StringSplitOptions.TrimEntries);
            if (headerPair.Length != 2) continue; // basic checking
            // header in case insensitive (see 
            request._headers.Add(headerPair[0].ToLower(), headerPair[1]);
        }

        request.Hostname = request._headers["host"];
        //request.Hostname = request.Hostname.Split(':')[0];

        if (null != request._headers["X-Requested-With"])
            request.Xhr = request._headers["X-Requested-With"]!.Equals("XMLHttpRequest");

        #endregion

        return true;
    }
}
