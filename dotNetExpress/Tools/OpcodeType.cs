namespace dotNetExpress.Tools;

public partial class WebSocket
{
    public enum OpcodeType
    {
        /* Denotes a continuation code */
        Fragment = 0,

        /* Denotes a text code */
        Text = 1,

        /* Denotes a binary code */
        Binary = 2,

        /* Denotes a closed mediaServer */
        ClosedConnection = 8
    }
}