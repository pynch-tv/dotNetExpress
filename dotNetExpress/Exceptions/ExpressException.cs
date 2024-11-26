using System.Net;

namespace dotNetExpress.Exceptions;

public class ExpressException(HttpStatusCode status, string title, string detail) : Exception
{
    public HttpStatusCode Status = status;
    public string Title = title != "" ? title : status.ToString();
    public string Detail = detail;

    public ExpressException(uint status, string title, string detail) : this((HttpStatusCode)status, title, detail)
    {
    }

    public ExpressException(HttpStatusCode status, string detail) : this(status, "", detail)
    {
    }

    public dynamic toJson()
    {
        return new { status = Status, title = Title, detail = Detail };
    }
}
