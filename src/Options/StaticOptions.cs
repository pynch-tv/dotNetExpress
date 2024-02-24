namespace dotNetExpress.Options;

public class StaticOptions
{
    /// <summary>
    /// Determines how dotfiles (files or directories that begin with a dot “.”) are treated.
    /// </summary>
    public string Dotfiles;

    /// <summary>
    /// Enable or disable etag generation
    /// </summary>
    public bool Etag = true;

    /// <summary>
    /// Sets file extension fallbacks: If a file is not found, search for files with the specified extensions and serve the first one found. Example: ['html', 'htm'].	
    /// </summary>
    public string Extensions;

    /// <summary>
    /// Let client errors fall-through as unhandled requests, otherwise forward a client error.
    /// </summary>
    public bool Fallthrough = true;

    /// <summary>
    /// Enable or disable the immutable directive in the Cache-Control response header. If enabled, the maxAge option should also be specified to enable caching. The immutable directive will prevent supported clients from making conditional requests during the life of the maxAge option to check if the file has changed.	
    /// </summary>
    public bool Immutable = false;

    /// <summary>
    /// Sends the specified directory index file. Set to false to disable directory indexing.	
    /// </summary>
    public string Index;

    /// <summary>
    /// Set the Last-Modified header to the last modified date of the file on the OS.
    /// </summary>
    public bool LastModified = true;

    /// <summary>
    /// Set the max-age property of the Cache-Control header in milliseconds or a string in ms format.	
    /// </summary>
    public int MaxAge = 0;

    /// <summary>
    /// Redirect to trailing “/” when the pathname is a directory.	
    /// </summary>
    public bool Redirect = true;

    /// <summary>
    /// Function for setting HTTP headers to serve with the file.
    /// </summary>
    public string SetHeaders;
}
