using dotNetExpress.Delegates;
using System;
using System.Collections.Specialized;
using HttpMultipartParser;

namespace dotNetExpress.Middlewares.Multer;

public delegate void DiskStorageCallback(Request request, File file);

public class DiskStorage
{
    DiskStorageCallback Destination;
}

public class Multer
{
    /// <summary>
    /// Where to store the files
    /// </summary>
    public DiskStorage DiskStorage;

    /// <summary>
    /// Where to store the files
    /// </summary>
    public string Dest = "";

    /// <summary>
    /// Function to control which files are accepted
    /// </summary>
    public string FileFilter = "";

    /// <summary>
    /// Limits of the uploaded data
    /// </summary>
    public string Limits;

    /// <summary>
    /// Keep the full path of files instead of just the base name
    /// </summary>
    public string PreservePath;

    public Multer()
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public MiddlewareCallback Single(string fieldname)
    {
        return ParseMultiPart;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="names"></param>
    /// <param name="maxCount"></param>
    /// <returns></returns>
    public MiddlewareCallback Array(string fieldname, uint maxCount = uint.MaxValue)
    {
        return ParseMultiPart;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public MiddlewareCallback Fields()
    {
        return ParseMultiPart;
    }

    //public void DiskStorage()
    //{

    //}

    /// <summary>
    /// 
    /// </summary>
    /// <param name="req"></param>
    /// <param name="res"></param>
    /// <param name="next"></param>
    public void ParseMultiPart(Request req, Response res, NextCallback next = null)
    {
        if (null != req.Body)
        {   //  already parsed
            next?.Invoke(null);
            return;
        }

        if (null == req.Get("Content-Type"))
        {
            next?.Invoke(null);
            return;
        }

        if (req.Get("Content-Type")!.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
        {
            var contentType = req.Get("Content-Type"); // will be in lowercase
            var boundary = contentType.Split('=')[1];

            req.Body = new NameValueCollection();

            var contentLength = int.Parse(req.Get("Content-Length") ?? "0");
            if (contentLength > 0)
            {
                var parser = new StreamingMultipartFormDataParser(req.StreamReader);
                parser.ParameterHandler += kvp =>
                {
                    req.Body[kvp.Name] = kvp.Data;
                };
                parser.FileHandler += (name, fileName, type, disposition, buffer, bytes, partNumber, additionalProperties) =>
                {
                    if (partNumber == 0)
                    {
                        Console.WriteLine(Dest);
                    }
                };

                parser.Run();

                Console.WriteLine();
            }
        }

        next?.Invoke(null);
    }

}
