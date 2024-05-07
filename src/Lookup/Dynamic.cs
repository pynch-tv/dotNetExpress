using System.Collections.Generic;
using System.ComponentModel;

namespace dotNetExpress.Lookup;

internal static class Dynamic
{
    public static Dictionary<string, dynamic> ToDictionary(dynamic dynObj)
    {
        var dictionary = new Dictionary<string, object>();
        foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties(dynObj))
        {
            object obj = propertyDescriptor.GetValue(dynObj);
            dictionary.Add(propertyDescriptor.Name, obj);
        }
        return dictionary;
    }
}
