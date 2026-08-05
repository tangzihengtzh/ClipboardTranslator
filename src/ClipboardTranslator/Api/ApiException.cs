namespace ClipboardTranslator.Api;

public sealed class ApiException : Exception
{
    public int StatusCode { get; }
    public bool Retryable { get; }

    public ApiException(string message, int statusCode, bool retryable = false)
        : base(message)
    {
        StatusCode = statusCode;
        Retryable = retryable;
    }
}
