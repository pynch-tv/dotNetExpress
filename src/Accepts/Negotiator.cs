using System.Collections.Specialized;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace dotNetExpress.Lookup;

internal class Negotiator
{
    private Request _req;

    private const string SimpleMediaTypeRegExp = @"^\s*([^\s\/;]+)\/([^;\s]+)\s*(?:;(.*))?$";

    /// <summary>
    /// 
    /// </summary>
    internal Negotiator(Request req)
    {
        _req = req;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    internal string[] MediaTypes(IEnumerable<string> available = null)
    {
        return PreferredMediaTypes(_req.Headers["accept"], available);
    }

    /// <summary>
    /// Get the preferred media types from an Accept header.
    /// </summary>
    /// <param name="accept"></param>
    /// <param name="provided"></param>
    /// <returns></returns>
    internal string[] PreferredMediaTypes(string accept, IEnumerable<string> provided)
    {
        // RFC 2616 sec 14.2: no header = */*
        var accepts = ParseAccept(accept ?? "*/*");

        if (null == provided)
        {
            // sorted list of all types
            //return accepts
            //    .filter(isQuality)
            //    .sort(compareSpecs)
            //    .map(getFullType);
        }

        var priorities = provided.Select(
            (type, index) => GetMediaTypePriority(type, accepts, index));

        var qualityPriorities = priorities.Where(spec => spec.q > 0).ToList();
        qualityPriorities.Sort(Comparison);

        // take only type
        return qualityPriorities.Select((type, index) => type.type).ToArray();
    }
    
    private int Comparison(SpecificMediaType a, SpecificMediaType b)
    {
        return (int)getNonZero(new[] { b.q - a.q, b.s - a.s, a.o - b.o, a.i - b.i, 0 });
        //return yy switch
        //{
        //    < 0 => -1,
        //    > 1 => 1,
        //    _ => 0
        //};
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

        for (var i = 0; i < accepted.Length; i++) {
            var spec = Specify(type, accepted[i], index);
            if (null == spec) continue;

            if (getNonZero(new[] { priority.s - spec.s, priority.q - spec.q, priority.o - spec.o }) < 0)
                priority = (SpecificMediaType)spec;
        }

        return priority;
    }

    private float getNonZero(IEnumerable<float> array)
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
    internal SpecificMediaType? Specify(string type, AcceptsMediaType spec, int index)
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
            foreach (var key in keys)
            {
                
            }


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
            type = type,
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
        var accepts = new List<AcceptsMediaType>();

        for (var i = 0; i < mediaTypes.Length; i++)
        {
            var mediaType = ParseMediaType(mediaTypes[i].Trim(), i);
            if (null != mediaType)
                accepts.Add((AcceptsMediaType)mediaType);
        }
        
        return accepts.ToArray();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="str"></param>
    /// <param name="i"></param>
    /// <returns></returns>
    internal AcceptsMediaType? ParseMediaType(string str, int i = -1)
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
        foreach (var pair in kvps)
        {
            var key = pair.Key.ToLower();
            var val = pair.Value;

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
    internal class AcceptsMediaType
    {
        public string type;
        public string subtype;
        public NameValueCollection prms;
        public float q;
        public int i;

        public AcceptsMediaType(string type, string subtype, NameValueCollection prms, float q, int i)
        {
            this.type = type;
            this.subtype = subtype;
            this.prms = prms;
            this.q = q;
            this.i = i;
        }
    }

    [DebuggerDisplay("type = {type}, i = {i}, o={o}, q={q}, s={s}")]
    internal class SpecificMediaType : IEquatable<SpecificMediaType>
    {
        public string type;

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