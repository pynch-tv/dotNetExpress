using System.Net;

namespace dotNetExpress.Tools;

public static class SseFactory
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    public static bool IsServerSentEventRequest(Request req)
    {
        return string.Equals(req.Get("accept"), "text/event-stream", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="res"></param>
    public static void SendResponse(Request _, Response res)
    {
        res.Set("Content-Type", "text/event-stream");
        res.Set("Connection", "keep-alive");
        res.Set("Cache-Control", "no-cache");
        res.Set("Keep-Alive");

        res.WriteHead(HttpStatusCode.OK);
        res.End();
    }
}