using System.Collections.Generic;

namespace dotNetExpress.Middlewares;

public class TemplateEngine
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="locals"></param>
    /// <returns></returns>
    public static string T4(string filename, Dictionary<string, dynamic> locals)
    {
        //var asm = Assembly.LoadFrom("Pynch.Nexa.dll");
        //var type = asm.GetType($"Pynch.Nexa.Views.LandingPage");

        //dynamic template = Activator.CreateInstance(type);
        //return template.TransformText(uri, title, obj);

        return string.Empty;
    }
}