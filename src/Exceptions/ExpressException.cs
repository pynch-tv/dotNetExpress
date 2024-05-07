using System;
using System.Net;

namespace dotNetExpress.Exceptions;

public class ExpressException : Exception
{
    public HttpStatusCode StatusCode;
    public string Description;

    public ExpressException(HttpStatusCode statusCode, string description)
    {
        StatusCode = statusCode;
        Description = description;
    }

    public ExpressException(uint statusCode, string description)
    {
        StatusCode = (HttpStatusCode)statusCode;
        Description = description;
    }

    public dynamic toJson()
    {
        return new { code = StatusCode, description = Description };
    }
}
