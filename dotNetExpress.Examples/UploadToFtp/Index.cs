using System.Net;

namespace dotNetExpress.examples;

internal partial class Examples
{
    internal static async Task UploadToFtp()
    {
        var app = new Express();
        const int port = 8080;

        app.Post("/v1/servers/XT2/clips", Express.Json(), (req, res, next) =>
        {
            var address = "192.168.0.197";
            var slot = "1";
            var fileName = "test222.mp4";

            var ftpRequest = WebRequest.Create($"ftp://{address}/{slot}/{fileName}");
            ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;

            var requestStream = ftpRequest.GetRequestStream();
            req.StreamReader.CopyTo(requestStream);
            requestStream.Close();

            res.Send("Hello World");
        });

        await app.Listen(port, () =>
        {
            Console.WriteLine($"Example app listening on port {port}");
        });
    }
}
