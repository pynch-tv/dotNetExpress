namespace dotNetExpress.Lookup;

internal class Negotiator
{
    private readonly Request _req;

    internal Negotiator(Request req)
    {
        _req = req;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    internal string MediaType(string available = null)
    {
        var mediaTypes = MediaTypes(new string[] { available });
        return mediaTypes.ElementAt(0);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    internal string[] MediaTypes(string[] available = null)
    {
        var mediaType = new MediaType();
        return mediaType.PreferredMediaTypes(_req.Get("Accept"), available);
    }

}

