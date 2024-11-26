namespace dotNetExpress.Options;

public class CookieOptions
{
    /// <summary>
    /// Domain name for the cookie. Defaults to the domain name of the app.
    /// </summary>
    public string Domain;

    /// <summary>
    /// A synchronous function used for cookie value encoding. Defaults to encodeURIComponent.
    /// </summary>
    public object Encode;

    /// <summary>
    /// Expiry date of the cookie in GMT. If not specified or set to 0, creates a session cookie.
    /// </summary>
    public DateTime Expires;

    /// <summary>
    /// Flags the cookie to be accessible only by the web server.
    /// </summary>
    public bool HttpOnly;

    /// <summary>
    /// Convenient option for setting the expiry time relative to the current time in milliseconds.
    /// </summary>
    public int MaxAge;

    /// <summary>
    /// Path for the cookie. Defaults to “/”.
    /// </summary>
    public string Path;

    /// <summary>
    /// Value of the “Priority” Set-Cookie attribute.
    /// </summary>
    public string Priority;

    /// <summary>
    /// Marks the cookie to be used with HTTPS only.
    /// </summary>
    public bool Secure;

    /// <summary>
    /// Indicates if the cookie should be signed.
    /// </summary>
    public bool Signed;

    /// <summary>
    /// Value of the “SameSite” Set-Cookie attribute.
    /// More information at https://tools.ietf.org/html/draft-ietf-httpbis-cookie-same-site-00#section-4.1.1.
    /// </summary>
    public bool SameSite;
}
