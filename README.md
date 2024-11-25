![example workflow](https://github.com/pynch-tv/dotNetExpress/actions/workflows/dotnet.yml/badge.svg)
# .net Express

## Getting started

### Hello world example

Below is essentially the .Net express app you can create. 

```cs
static void HelloWorld()
{
    var app = new Express();
    const int port = 8080;

    app.Get("/", (req, res, next) =>
    {
        res.Send("Hello World");
    });

    app.Listen(port, () =>
    {
        Console.WriteLine($"Example app listening on port {port}");
    });
}
```
More examples in the [Examples](https://github.com/pynch-tv/dotNetExpress/tree/main/examples) folder

## Dependencies

Via NuGet:
- Microsoft.IO.RecyclableMemoryStream
- Multipart Parser from [Http-Multipart-Data-Parser](https://github.com/Http-Multipart-Data-Parser/Http-Multipart-Data-Parser)
