using System.Collections.Specialized;

namespace dotNetExpress.Options;

public class DownloadOptions
{
    /// <summary>
    /// Set the max-age property of the Cache-Control header in milliseconds or a string in ms format.	
    /// </summary>
    public int MaxAge = 0;

    /// <summary>
    /// Set the Last-Modified header to the last modified date of the file on the OS.
    /// </summary>
    public bool LastModified = true;

    /// <summary>
    /// Object containing HTTP headers to serve with the file.
    /// </summary>
    public NameValueCollection Headers = [];

    /// <summary>
    /// Option for serving dotfiles. Possible values are “allow”, “deny”, “ignore”.
    /// Default is "ignore"
    /// </summary>
    public string DotFiles = "ignore";

    /// <summary>
    /// Enable or disable accepting ranged requests.
    /// </summary>
    public bool AcceptRanges = true;

    /// <summary>
    /// Enable or disable setting Cache-Control response header.
    /// </summary>
    public bool CacheControl = true;
    
    /// <summary>
    /// Enable or disable the immutable directive in the Cache-Control response header. If enabled, the maxAge option should also be specified to enable caching. The immutable directive will prevent supported clients from making conditional requests during the life of the maxAge option to check if the file has changed.	
    /// </summary>
    public bool Immutable = false;
}
