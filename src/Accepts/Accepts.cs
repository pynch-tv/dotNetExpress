using System.Collections.Specialized;

namespace dotNetExpress.Lookup;

internal class Accepts
{
    private readonly Negotiator _negociator;

    private readonly NameValueCollection _headers;

    private string[] _types;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="req"></param>
    internal Accepts(Request req)
    {
        _headers = req.Headers;
        _negociator = new Negotiator(req);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="types"></param>
    /// <returns></returns>
    internal IEnumerable<string> Types(string[] types)
    {
        // no types, return all requested types
        if (types.Length == 0)
            return _negociator.MediaTypes();

        // no accept header, return first given type
        if (null == _headers["accept"])
            return new string[] { types[0] };

        
        var mimes = types.Select(extToMime).ToArray();
        var accepts = this._negociator.MediaTypes(mimes.Where(validMime));
        if (null == accepts) return null;
        if (!accepts.Any()) return null;

        var first = accepts[0];
        var tt = Array.IndexOf(mimes, first);

        return new string[] { types[tt] };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private string extToMime(string type)
    {
        return type.IndexOf("/") == -1
            ? MimeTypes.Lookup(type)
            : type;
    }

    private bool validMime(string type)
    {
        return !string.IsNullOrEmpty(type);
    }

}