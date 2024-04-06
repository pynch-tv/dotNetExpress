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

None
