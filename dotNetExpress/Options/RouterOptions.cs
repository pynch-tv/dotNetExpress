namespace dotNetExpress.Options;

public class RouterOptions
{
    /// <summary>
    /// Enable case sensitivity.
    ///
    /// Disabled by default, treating “/Foo” and “/foo” as the same.	
    /// </summary>
    public bool CaseSensitive = false;

    /// <summary>
    /// Preserve the req.params values from the parent router. If the
    /// parent and the child have conflicting param names, the child’s
    /// value take precedence.	
    /// </summary>
    public bool MergeParams = false;

    /// <summary>
    /// Enable strict routing.
    ///
    /// Disabled by default, “/foo” and “/foo/” are treated the same by the router.		
    /// </summary>
    public bool Strict = false;
}
