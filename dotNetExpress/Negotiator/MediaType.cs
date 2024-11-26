using System.Collections.Specialized;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace dotNetExpress.Lookup;

internal class MediaType
{
    private const string SimpleMediaTypeRegExp = @"^\s*([^\s\/;]+)\/([^;\s]+)\s*(?:;(.*))?$";

    private static bool IsQuality(SpecificMediaType spec)
    {
        return spec.q > 0;
    }

    //private bool CompareSpecs(AcceptsMediaType a, AcceptsMediaType b)
    //{
    //    return (b.q - a.q) ;//|| (b.subtype - a.subtype) || (a.o - b.o) || (a.i - b.i) || 0;
    //}

    /// <summary>
    /// Get the preferred media types from an Accept header.
    /// </summary>
    /// <param name="accept"></param>
    /// <param name="provided"></param>
    /// <returns></returns>
    internal string[] PreferredMediaTypes(string accept, string[] provided)
    {
        // RFC 2616 sec 14.2: no header = */*
        var accepts = ParseAccept(accept ?? "*/*");

        if (null == provided)
        {
            //accepts = Array.FindAll(accepts, IsQuality);
           // Array.Sort(accepts, CompareSpecs);

            // sorted list of all types
            //return accepts
            //    .filter(isQuality)
            //    .sort(compareSpecs)
            //    .map(getFullType);
            return null;
        }

        
        var priorities = provided.Select((type, index) 
            => GetMediaTypePriority(type, accepts, index));

        var e1 = priorities.Where(IsQuality);
        var e2 = e1.OrderBy(x => x, Comparer<SpecificMediaType>.Create(CompareSpecs));
        var e3 = e2.Select(priority => IndexOf(priorities, priority)).Select(provided.ElementAt);

        return e3.ToArray();


        // sorted list of accepted types
        return priorities.Where(IsQuality).OrderBy(x => x, Comparer<SpecificMediaType>.Create(CompareSpecs)).Select(priority => IndexOf(priorities, priority)).Select(provided.ElementAt).ToArray();
    }

    public static int IndexOf<T>(IEnumerable<T> source, T value)
    {
        var index = 0;
        var comparer = EqualityComparer<T>.Default; // or pass in as a parameter
        foreach (var item in source)
        {
            if (comparer.Equals(item, value)) return index;
            index++;
        }
        return -1;
    }

    private static int CompareSpecs(SpecificMediaType a, SpecificMediaType b)
    {
        return Equals(a.q, default(float)) ? (int)(b.q - a.q) :
               Equals(a.s, default(int)) ? (b.s - a.s) :
               Equals(a.o, default(int)) ? (a.o - b.o) :
               Equals(a.i, default(int)) ? (a.i - b.i) : 
               0;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <param name="accepted"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    internal SpecificMediaType GetMediaTypePriority(string type, AcceptsMediaType[] accepted, int index)
    {
        var priority = new SpecificMediaType { i = -1 };

        foreach (var t in accepted)
        {
            var spec = Specify(type, t, index);
            if (null == spec) continue;

            //if ((Equals(priority.s, default(int)) ? (priority.s - spec.s) :
            //     Equals(priority.q, default(float)) ? (priority.q - spec.q) :
            //     Equals(priority.o, default(int)) ? (priority.o - spec.s) : -1) < 0)
            //{

            //}

            if (getNonZero([priority.s - spec.s, priority.q - spec.q, priority.o - spec.o]) < 0)
                priority = (SpecificMediaType)spec;
        }

        return priority;
    }

    private static float getNonZero(IEnumerable<float> array)
    {
        return array.FirstOrDefault(i => i != 0);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <param name="spec"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    internal SpecificMediaType Specify(string type, AcceptsMediaType spec, int index)
    {
        var p = ParseMediaType(type);
        var s = 0;

        if (null == p) return null;

        if (string.Equals(spec.type, p.type, StringComparison.CurrentCultureIgnoreCase))
            s |= 4;
        else if (spec.type != "*")
            return null;

        if (string.Equals(spec.subtype, p.subtype, StringComparison.CurrentCultureIgnoreCase))
            s |= 2;
        else if (spec.subtype != "*")
            return null;

        var keys = spec.prms.AllKeys;
        if (keys.Length > 0)
        {
            //if (keys.All(k => spec.params[k] == "*" || (spec.params[k] ?? "").ToLower() == (p.params[k] ?? "").ToLower()))
            //{
            //    s |= 1;
            //}
            //else
            //{
            //    return null;
            //}


            //    if (keys.every(function(k) {
            //        return spec.params[k] == '*' || (spec.params[k] || '').ToLower() == (p.params[k] || '').ToLower();
            //    })) {
            //        s |= 1
            //    } else
            //    {
            //        return null
            //    }
        }

        return new SpecificMediaType
        {
            i = index,
            o = spec.i,
            q = spec.q,
            s = s
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="accept"></param>
    /// <returns></returns>
    internal AcceptsMediaType[] ParseAccept(string accept)
    {
        var mediaTypes = SplitMediaTypes(accept);

        return mediaTypes.Select((t, i) => ParseMediaType(t.Trim(), i)).Where(mediaType => null != mediaType).ToArray();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="str"></param>
    /// <param name="i"></param>
    /// <returns></returns>
    internal AcceptsMediaType ParseMediaType(string str, int i = -1)
    {
        var rg = new Regex(SimpleMediaTypeRegExp);
        var match = rg.Match(str);
        if (!match.Success) return null;

        var prms = new NameValueCollection();
        var q = 1.0f;
        var subtype = match.Groups[2].Value;
        var type = match.Groups[1].Value;

        if (string.IsNullOrEmpty(match.Groups[3].Value))
            return new AcceptsMediaType(type, subtype, prms, q, i);

        var kvps = SplitParameters(match.Groups[3].Value).Select(splitKeyValuePair);
        foreach (var (s, val) in kvps)
        {
            var key = s.ToLower();

            // get the value, unwrapping quotes
            var value = null != val && val[0] == '"' && val[^1] == '"'
                ? val.Substring(1, val.Length - 2)
                : val;

            if (key == "q")
            {
                q = float.Parse(value);
                break;
            }

            // store parameter
            prms[key] = value;
        }

        return new AcceptsMediaType(type, subtype, prms, q, i);
    }

    [DebuggerDisplay("type = {type}, subtype={subtype}, q={q}, i={i}")]
    internal class AcceptsMediaType(string type, string subtype, NameValueCollection prms, float q, int i)
    {
        public string type = type;
        public string subtype = subtype;
        public NameValueCollection prms = prms;
        public float q = q;
        public int i = i;
    }

    [DebuggerDisplay("i = {i}, o={o}, q={q}, s={s}")]
    internal class SpecificMediaType : IEquatable<SpecificMediaType>
    {
        public int i;
        public int o;
        public float q;
        public int s;

        public bool Equals(SpecificMediaType other)
        {
            if (null == other) return false;
            return (i == other.i && o == other.o && Math.Abs(q - other.q) < 0.0001 &&  s == other.s);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="accept"></param>
    /// <returns></returns>
    internal string[] SplitMediaTypes(string accept)
    {
        var accepts = accept.Split(",", StringSplitOptions.RemoveEmptyEntries);

        var j = 0;
        for (var i = 1; i < accepts.Length; i++)
        {
            if (QuoteCount(accepts[j]) % 2 == 0)
                accepts[++j] = accepts[i];
            else
                accepts[j] += "," + accepts[i];
        }

        // trim accepts
    //    accepts.Length = j + 1;

        return accepts;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="accept"></param>
    /// <returns></returns>
    internal static int QuoteCount(string accept)
    {
        var count = 0;
        var index = 0;

        while ((index = accept.IndexOf('"', index)) != -1)
        {
            count++;
            index++;
        }

        return count;

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    internal string[] SplitParameters(string str)
    {
        var parameters = str.Split(';', StringSplitOptions.RemoveEmptyEntries);

        var j = 0;
        for (var i = 1; i < parameters.Length; i++)
        {
            if (QuoteCount(parameters[j]) % 2 == 0)
            {
                parameters[++j] = parameters[i];
            }
            else
            {
                parameters[j] += ';' + parameters[i];
            }
        }

        // trim parameters
        //parameters.length = j + 1;

        for (var i = 0; i < parameters.Length; i++)
        {
            parameters[i] = parameters[i].Trim();
        }

        return parameters;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    internal KeyValuePair<string, string> splitKeyValuePair(string str)
    {
        var index = str.IndexOf('=');
        var key = "";
        var val = "";

        if (index == -1)
        {
            key = str;
        }
        else
        {
            key = str[..index];
            val = str[(index + 1)..];
        }

        return new KeyValuePair<string,string>(key, val);
    }

}