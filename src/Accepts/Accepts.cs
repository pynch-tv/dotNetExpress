using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace dotNetExpress.Lookup;

internal class Accepts
{
    private readonly Negotiator _negotiator;

    private readonly NameValueCollection _headers;

    private string[] _types;

    /**
     * Check if the given `type(s)` is acceptable, returning
     * the best match when true, otherwise `undefined`, in which
     * case you should respond with 406 "Not Acceptable".
     *
     * The `type` value may be a single mime type string
     * such as "application/json", the extension name
     * such as "json" or an array `["json", "html", "text/plain"]`. When a list
     * or array is given the _best_ match, if any is returned.
     *
     * Examples:
     *
     *     // Accept: text/html
     *     this.types('html');
     *     // => "html"
     *
     *     // Accept: text/*, application/json
     *     this.types('html');
     *     // => "html"
     *     this.types('text/html');
     *     // => "text/html"
     *     this.types('json', 'text');
     *     // => "json"
     *     this.types('application/json');
     *     // => "application/json"
     *
     *     // Accept: text/*, application/json
     *     this.types('image/png');
     *     this.types('png');
     *     // => undefined
     *
     *     // Accept: text/*;q=.5, application/json
     *     this.types(['html', 'json']);
     *     this.types('html', 'json');
     *     // => "json"
     *
     * @param {String|Array} types...
     * @return {String|Array|Boolean}
     * @public
     */

    /// <summary>
    /// 
    /// </summary>
    /// <param name="req"></param>
    internal Accepts(Request req)
    {
        _headers = req.Headers;
        _negotiator = new Negotiator(req);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="types"></param>
    /// <returns></returns>
    internal string[] Types(string[] types)
    {
        // no types, return all requested types
        if (types.Length == 0)
            return _negotiator.MediaTypes();

        // no accept header, return first given type
        if (null == _headers["accept"])
            return new string[] { types[0] };

        var mimes = types.Select(extToMime).ToArray();
        var accepts = _negotiator.MediaTypes(mimes.Where(validMime).ToArray());
        if (null == accepts) return null;
        if (!accepts.Any()) return null;
        var first = accepts.ElementAt(0);

        return null == first ? null : new[] { types[Array.IndexOf(mimes, first)]};
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="encodings"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    internal IEnumerable<string> Encodings(string[] encodings)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="charSets"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    internal IEnumerable<string> CharSets(string[] charSets)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="languages"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    internal IEnumerable<string> Languages(string[] languages)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private string extToMime(string type)
    {
        return type.IndexOf("/", StringComparison.Ordinal) == -1
            ? MimeTypes.Lookup(type)
            : type;
    }

    private static bool validMime(string type)
    {
        return !string.IsNullOrEmpty(type);
    }

}