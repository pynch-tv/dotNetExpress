using dotNetExpress.Exceptions;
using System.Reflection;

namespace dotNetExpress.Middlewares;

public class TemplateEngine
{
    // This middleware and the dll are coupled
    private const string LibName = "Pynch.Nexa.T4.dll";

    /// <summary>
    /// 
    /// </summary>
    /// <param name="view"></param>
    /// <param name="locals"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="ExpressException"></exception>
    public static string T4(string view, dynamic? locals = null)
    {
        var __dirname = Directory.GetCurrentDirectory();

        var filename = Path.Combine(__dirname, LibName);
        var asm = Assembly.LoadFrom(filename);
        if (null == asm)
            throw new FileNotFoundException($"dll {filename} not found in {__dirname}");
        var type = asm.GetType($"Pynch.Nexa.T4.Views.{view}");
        if (null == type)
            throw new ExpressException(500, "Not found", $"TypeOf(Pynch.Nexa.T4.Views.{view} not found");

        dynamic template = Activator.CreateInstance(type);
        if (null == template)
            throw new ExpressException(500, "template is null",  $"Unable to create instance of {type}");

        return template.TransformText(locals);
    }
}