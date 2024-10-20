using System;
using System.Net;

namespace dotNetExpress.Exceptions;

public class ExpressException(HttpStatusCode status, string title, string detail) : Exception
{
    public HttpStatusCode Status = status;
    public string Detail = detail;
    public string Title = title;

    public ExpressException(uint status, string title, string detail) : this((HttpStatusCode)status, title, detail)
    {
    }

    public dynamic toJson()
    {
        return new { status = Status, title = Title, detail = Detail };
    }
}
