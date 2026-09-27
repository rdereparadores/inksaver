namespace InkSaver;

/// <summary>A printing problem whose message can be shown to the user as is.</summary>
internal sealed class PrintException : Exception
{
    public PrintException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
