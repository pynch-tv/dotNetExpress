namespace Pynch.Nexa.Tools.Express.Extensions;

using System;
using System.Linq;

public static class Extensions
{
    public static Uri Append(this Uri uri, params string[] paths)
    {
        return new Uri(paths.Aggregate(uri.AbsoluteUri, (current, path) => string.Format("{0}/{1}", current.TrimEnd('/'), path.TrimStart('/'))));
    }


    public static void AddRange<T>(this ICollection<T> target, IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);

        foreach (var element in source)
            target.Add(element);
    }

    public static void AddRange<T, S>(this Dictionary<T, S> source, Dictionary<T, S> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        foreach (var item in collection)
        {
            if (!source.ContainsKey(item.Key))
            {
                source.Add(item.Key, item.Value);
            }
            else
            {
                // handle duplicate key issue here
            }
        }
    }

}
