using dotNetExpress.examples;

internal class Program
{
    static async Task Main(string[] args)
    {
        await Examples.Download();

        Console.ReadLine();
    }
}