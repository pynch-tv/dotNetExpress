namespace dotNetExpress.Middlewares.Multer;
public class File
{
    /// <summary>
    /// Field name specified in the form
    /// </summary>
    public string FieldName;

    /// <summary>
    /// Name of the file on the user’s computer
    /// </summary>
    public string Originalname;

    /// <summary>
    /// Encoding type of the file
    /// </summary>
    public string Encoding;

    /// <summary>
    /// Mime type of the file
    /// </summary>
    public string Mimetype;

    /// <summary>
    /// Size of the file in bytes
    /// </summary>
    public uint Size;

    /// <summary>
    /// The folder to which the file has been saved
    /// </summary>
    public string Destination;

    /// <summary>
    /// The name of the file within the destination
    /// </summary>
    public string Filename;

    /// <summary>
    /// The full path to the uploaded file
    /// </summary>
    public string Path;

    /// <summary>
    /// A Buffer of the entire file
    /// </summary>
    public string Buffer;
}
