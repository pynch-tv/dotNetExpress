﻿using dotNetExpress.Overrides;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;

namespace dotNetExpress;

internal class Client
{
    static int ReqNr = 0;


    public void Connection(Express express, TcpClient tcpClient)
    {

        var sw = Stopwatch.StartNew();

//        Debug.WriteLine($"[{ReqNr}] ({DateTime.Now:HH.mm.ss:ffff}) step 1 {sw.ElapsedMilliseconds}ms");

        try
        {
            if (!tcpClient.Connected)
                return;

//            Debug.WriteLine($"[{ReqNr}] ({DateTime.Now:HH.mm.ss:ffff}) step 2 {sw.ElapsedMilliseconds}ms");

            sw.Restart();

            var stream = tcpClient.GetStream();

            while (tcpClient.Connected)
            {
                ReqNr++;

//                Debug.WriteLine($"[{ReqNr}] ({DateTime.Now:HH.mm.ss:ffff}) step 3 {sw.ElapsedMilliseconds}ms");

                if (!GetRequest(express, tcpClient, out Request req))
                    throw new HttpProtocolException(500, "Unable to construct Request", new ProtocolViolationException("Unable to construct Request"));

                if (req == null || req.Method == null)
                    throw new HttpProtocolException(500, "Error while parsing reuqest", new ProtocolViolationException("Unable to construct Request"));

                req.Res.WriteHeaders += (sender, e) =>
                {
                    req.Res.Set("X-Response-Time", sw.ElapsedMilliseconds);
                };

                stream.ReadTimeout = express.KeepAliveTimeout * 1000; 

                if (req.Headers.TryGetValue("Content-Length", out var cl) && cl != null && !string.IsNullOrEmpty(cl))
                {
                    var contentLength = int.Parse(cl);

                    // When a content-length is available, a stream is provided in Request
                    req.StreamReader = new MessageBodyStreamReader(stream);
                    req.StreamReader.SetLength(contentLength);
                }

                express.Dispatch(req, req.Res);

                req.StreamReader?.Close();

                if (req.Headers.TryGetValue("Upgrade", out var upgrade) && upgrade != null && upgrade.Equals("WebSocket", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] ({DateTime.Now:HH.mm.ss:ffff}) websocket upgrade");
                    return; // websocket
                }
                else if (req.Headers.TryGetValue("Accept", out var accept) && accept != null && accept.Equals("text/event-stream", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] ({DateTime.Now:HH.mm.ss:ffff}) text/event-stream");
                    return; // Server-Sent Events
                }
                else if (req.Headers.TryGetValue("Connection", out var connection) && connection != null && connection.Equals("keep-alive", StringComparison.OrdinalIgnoreCase))
                {
                    // https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.tcplistener.accepttcpclient?view=net-8.0
                    // Remark: When you are through with the TcpClient, be sure to call its Close method. If you want greater
                    // flexibility than a TcpClient offers, consider using AcceptSocket.
                    //                    Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] ({DateTime.Now:HH.mm.ss:ffff}) Lets keep the connection open");

                    //Debug.WriteLine($"[{ReqNr}] ({DateTime.Now:HH.mm.ss:ffff}) step 999 {sw.ElapsedMilliseconds}ms");

                    continue;
                }
                else
                {
                    // Do not keep the connection alive, leave the while loop
//                    Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] ({DateTime.Now:HH.mm.ss:ffff}) Connection does not need to be kept open");
                    tcpClient.Close();
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine($"[{Environment.CurrentManagedThreadId}] ({DateTime.Now:HH.mm.ss:ffff}) Exception {e.Message}");
            tcpClient.Close();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="app"></param>
    /// <param name="tcpClient"></param>
    /// <param name="req"></param>
    /// <returns></returns>
    /// <exception cref="HttpProtocolException"></exception>
    /// <exception cref="UriFormatException"></exception>
    private static bool GetRequest(Express app, TcpClient tcpClient, out Request req)
    {
        // create out variable
        req = new Request(app);

        var lineNumber = 1;

        using (var streamReader = new MessageBodyStreamReader(tcpClient.GetStream()))
        {
            while (true)
            {
                // Readline is a blocking IO call. If it takes to long for data to come in,
                // an IOException is thrown.
                // Note: if KeepAlive is true, ReadTimeout will be set to KeepAliveTimeout
                var line = streamReader.ReadLine();
                if (string.IsNullOrEmpty(line))
                    break;

                if (lineNumber++ == 1)
                {
                    var requestLineParts = line.Split(' ');
                    if (requestLineParts.Length != 3)
                        throw new HttpProtocolException(500, "First line must consists of 3 parts",
                            new ProtocolViolationException("First line must consists of 3 parts"));

                    req.Method = HttpMethod.Parse(requestLineParts[0]);
                    req.OriginalUrl = requestLineParts[1];
                    var idx = requestLineParts[1].LastIndexOf('?');
                    if (idx > -1)
                    {
                        var queries = requestLineParts[1][(idx + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var query in queries)
                        {
                            var queryParts = query.Split('=', StringSplitOptions.RemoveEmptyEntries);
                            if (queryParts.Length < 2) throw new Exception($"Query part is malformed: {query}");

                            req.Query.Add(queryParts[0], Uri.UnescapeDataString(string.Join('=', queryParts, 1, queryParts.Length - 1)));
                        }

                        req.Path = requestLineParts[1][..idx];
                    }
                    else
                        req.Path = requestLineParts[1];

                    idx = requestLineParts[2].IndexOf('/');
                    req.Protocol = requestLineParts[2][..idx].ToLower();
                    req.HttpVersion = new Version(requestLineParts[2][++idx..]);
                }
                else
                {
                    var headerPair = line.Split(":", 2, StringSplitOptions.TrimEntries);
                    if (headerPair.Length != 2)
                        throw new HttpProtocolException(500, "HeaderLine must consist of 2 parts",
                            new ProtocolViolationException("HeaderLine must consist of 2 parts"));

                    // header in case insensitive (see 
                    req.Headers.Add(headerPair[0].Trim().ToLower(), headerPair[1].Trim());
                }
            }
        }

        //Debug.WriteLine($"[{ReqNr}] ({DateTime.Now:HH.mm.ss:ffff}) {req.Protocol} {req.HttpVersion} {req.Path}");
        //Debug.WriteLine($"[{ReqNr}] ({DateTime.Now:HH.mm.ss:ffff}) Headers:");
        //foreach (var header in req.Headers)
        //    Debug.WriteLine($"[{ReqNr}] ({DateTime.Now:HH.mm.ss:ffff}) \t{header.Key}: {header.Value}");

        req.Host = req.Headers["host"];
        req.Hostname = req.Headers["host"]?.Split(':')[0];

        if (req.Headers.TryGetValue("X-Requested-With", out var requestedWith) && requestedWith != null)
            req.Xhr = requestedWith!.Equals("XMLHttpRequest");

        // Construct Response whilst we are at it
        req.Res = new Response(app, tcpClient.GetStream());

        //
        if (req.HttpVersion.Equals(new Version("1.0")))
        {
            req.Res.Set("Connection", "close");
        }
        else if (req.HttpVersion.Equals(new Version("1.1")))
        {
            req.Res.Set("Connection", "keep-alive");
            req.Res.Set("Keep-Alive", $"timeout={app.KeepAliveTimeout}"); // Keep-Alive is in *seconds*
        }
        else
        {
            // HTTP/2.0 TODO
        }

        req.Socket = tcpClient.Client;
        req.Res.Socket = tcpClient.Client;

        #region Parse Ip and Ips

        // get the IP from the remove endPoint
        if (!app.Get("trust proxy").Equals("false")
        && !string.IsNullOrEmpty(req.Get("X-Forwarded-For")))
        {
            var fips = req.Get("X-Forwarded-For").Split(',', StringSplitOptions.RemoveEmptyEntries);
            var ips = new List<IPAddress>();
            foreach (var fip in fips)
            {
                try
                {
                    ips.Add(IPAddress.Parse(fip));
                }
                catch
                {
                }
            }

            req.Ips = [.. ips];
            req.Ip = req.Ips.FirstOrDefault();
        }
        else
        {
            var ip = tcpClient.Client.RemoteEndPoint as IPEndPoint;
            req.Ip = ip.Address;
            req.Ips = [ip.Address];
        }

        #endregion

        return true;
    }

}
