using dotNetExpress.Overrides;
using System.Net.Sockets;
using System.Net;

namespace dotNetExpress;

internal class Client
{
    /// <summary>
    /// KeepALive allows HTTP clients to re-use connections for multiple requests, and relies on timeout configurations
    /// on both the client and target server to decide when to close open TCP sockets.
    ///
    /// There is overhead in establishing a new TCP connection (DNS lookups, TCP handshake, SSL/TLS handshake, etc).
    /// Without a keep-alive, every HTTP request has to establish a new TCP connection, and then close the connection
    /// once the response has been sent/received. A keep-alive allows an existing TCP connection to be re-used for
    /// multiple requests/responses, thus avoiding all of that overhead. That is what makes the connection "persistent".
    /// </summary>
    public bool KeepAlive = true;

    /// <summary>
    /// An integer that is the time in seconds that the host will allow an idle connection to remain open before it is closed.
    /// A connection is idle if no data is sent or received by a host. A host may keep an idle connection open for longer than
    /// timeout seconds, but the host should attempt to retain a connection for at least timeout seconds.
    /// </summary>
    public int KeepAliveTimeout = 2; // timing in seconds

    public async Task Connection(Express express, TcpClient tcpClient)
    {
        try
        {
            var stream = tcpClient.GetStream();

            stream.ReadTimeout = KeepAliveTimeout * 1000; // KeepAliveTimeout is in seconds, ReadTimeout in milliseconds
            if (stream.ReadTimeout == 0) KeepAlive = false;

            while (true)
            {
                Request req = null;

                try
                {
                    if (!GetRequest(express, tcpClient, out req))
                    {
                        throw new HttpProtocolException(500, "Unable to construct Request",
                            new ProtocolViolationException("Unable to construct Request"));
                    }

                    // If the request header has an explicit request to close the connection, set
                    // the KeepAlive of this request to false
                    var connection = req.Get("Connection");
                    if (connection != null)
                    {
                        KeepAlive = connection switch
                        {
                            "close" => false,
                            "keep-alive" => true,
                            _ => KeepAlive
                        };
                    }

                    if (!string.IsNullOrEmpty(req.Get("Content-Length")))
                    {
                        var contentLength = int.Parse(req.Get("Content-Length"));

                        // When a content-length is available, a stream is provided in Request
                        req.StreamReader = new MessageBodyStreamReader(stream);
                        req.StreamReader.SetLength(contentLength);
                    }
                }
                catch (IOException) // includes IOException when timeout
                {
                    tcpClient.Close();

                    return;
                }

                await express.Dispatch(req, req.Res);

                req.StreamReader?.Close();

                if (req.Res.Get("Upgrade") != null && req.Res.Get("Upgrade").Equals("WebSocket", StringComparison.OrdinalIgnoreCase))
                {
                    break; // websocket
                }
                else if (req.Res.Get("Content-Type") != null && req.Res.Get("Content-Type").Equals("text/event-stream", StringComparison.OrdinalIgnoreCase))
                {
                    break; // Server-Sent Events
                }
                else if (KeepAlive)
                {
                    // https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.tcplistener.accepttcpclient?view=net-8.0
                    // Remark: When you are through with the TcpClient, be sure to call its Close method. If you want greater
                    // flexibility than a TcpClient offers, consider using AcceptSocket.
                    continue;
                }
                else
                {
                    // Do not keep the connection alive
                    tcpClient.Close();
                    break;
                }
            }
        }
        catch (HttpProtocolException e)
        {
            // Typical exception when the client disconnects prematurely
            // Assumes socket closes automatically
        }
        catch (IOException e)
        {
            //tcpClient.Close();
        }
        finally
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="app"></param>
    /// <param name="stream"></param>
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
                    req.OriginalUrl = new Uri(requestLineParts[1], UriKind.Relative);
                    var idx = requestLineParts[1].LastIndexOf('?');
                    if (idx > -1)
                    {
                        var queries = requestLineParts[1][(idx + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var query in queries)
                        {
                            var queryParts = query.Split('=', StringSplitOptions.RemoveEmptyEntries);
                            if (queryParts.Length != 2) throw new UriFormatException($"Query part is malformed: {query}");

                            req.Query.Add(queryParts[0], Uri.UnescapeDataString(queryParts[1]));
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

        req.Host = req.Headers["host"];
        req.Hostname = req.Headers["host"]?.Split(':')[0];

        if (null != req.Headers["X-Requested-With"])
            req.Xhr = req.Headers["X-Requested-With"]!.Equals("XMLHttpRequest");

        // Construct Response whilst we are at it
        req.Res = new Response(app, tcpClient.GetStream());

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
