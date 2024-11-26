namespace dotNetExpress.Options;

public class urlEncodedOptions
{
    /// <summary>
    /// Enables or disables handling deflated (compressed) bodies;
    /// when disabled, deflated bodies are rejected.	
    /// </summary>
    public bool Inflate = true;

    /// <summary>
    /// Controls the maximum request body size. If this is a number, then
    /// the value specifies the number of bytes;
    /// </summary>
    public int Limit = 100 * 1024;

    /// <summary>
    /// This option allows to choose between parsing the URL-encoded data with 
    /// the querystring library (when false) or the qs library (when true).
    /// The “extended” syntax allows for rich objects and arrays to be encoded 
    /// into the URL-encoded format, allowing for a JSON-like experience with 
    /// URL-encoded.	
    /// </summary>
    public bool Extended = false;

    /// <summary>
    /// This option controls the maximum number of parameters that are allowed 
    /// in the URL-encoded data. If a request contains more parameters than this 
    /// value, an error will be raised.	
    /// </summary>
    public int ParameterLimit = 100;

    /// <summary>
    /// This is used to determine what media type the middleware will parse. This
    /// option can be a string, array of strings, or a function. If not a function,
    /// type option is passed directly to the type-is library and this can be an extension
    /// name (like json), a mime type (like application/json), or a mime type with a
    /// wildcard (like */* or */json). If a function, the type option is called as fn(req)
    /// and the request is parsed if it returns a truthy value.	
    /// </summary>
    public string Type = "application/x-www-form-urlencoded";

    /// <summary>
    /// This option, if supplied, is called as verify(req, res, buf, encoding), where buf is
    /// a Buffer of the raw request body and encoding is the encoding of the request. The
    /// parsing can be aborted by throwing an error.	
    /// </summary>
    //public int Verify;

}
