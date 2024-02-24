namespace dotNetExpress.Options;
public class RangeOptions
{
    /// <summary>
    /// Specify if overlapping & adjacent ranges should be combined, defaults to false.
    /// When true, ranges will be combined and returned as if they were specified that way in the header.
    /// </summary>
    public bool Combine;
}
