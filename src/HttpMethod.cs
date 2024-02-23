using System.Runtime.Serialization;

namespace dotNetExpress;

public enum HttpMethod
{
    /// <summary>
    /// HTTP GET.
    /// </summary>
    [EnumMember(Value = "GET")]
    // ReSharper disable once InconsistentNaming
    GET,

    /// <summary>
    /// HTTP HEAD.
    /// </summary>
    [EnumMember(Value = "HEAD")]
    // ReSharper disable once InconsistentNaming
    HEAD,

    /// <summary>
    /// HTTP PUT.
    /// </summary>
    [EnumMember(Value = "PUT")]
    // ReSharper disable once InconsistentNaming
    PUT,

    /// <summary>
    /// HTTP POST.
    /// </summary>
    [EnumMember(Value = "POST")]
    // ReSharper disable once InconsistentNaming
    POST,

    /// <summary>
    /// HTTP DELETE.
    /// </summary>
    [EnumMember(Value = "DELETE")]
    // ReSharper disable once InconsistentNaming
    DELETE,

    /// <summary>
    /// HTTP PATCH.
    /// </summary>
    [EnumMember(Value = "PATCH")]
    // ReSharper disable once InconsistentNaming
    PATCH,

    /// <summary>
    /// HTTP CONNECT.
    /// </summary>
    [EnumMember(Value = "CONNECT")]
    // ReSharper disable once InconsistentNaming
    CONNECT,

    /// <summary>
    /// HTTP OPTIONS.
    /// </summary>
    [EnumMember(Value = "OPTIONS")]
    // ReSharper disable once InconsistentNaming
    OPTIONS,

    /// <summary>
    /// HTTP TRACE.
    /// </summary>
    [EnumMember(Value = "TRACE")]
    // ReSharper disable once InconsistentNaming
    TRACE
}