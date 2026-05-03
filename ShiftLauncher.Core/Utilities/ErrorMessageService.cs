using System.Net.Http;

namespace ShiftLauncher.Core.Utilities;

public static class ErrorMessageService
{
    public static string ToUserMessage(Exception exception)
    {
        var ex = Unwrap(exception);

        return ex switch
        {
            UnauthorizedAccessException => "The launcher does not have permission to access one of the required files.",
            FileNotFoundException => "A required file was not found.",
            DirectoryNotFoundException => "A required folder was not found.",
            HttpRequestException => "Could not connect to the required online service. Check your internet connection.",
            TaskCanceledException => "The operation took too long and was cancelled.",
            IOException => "A file operation failed. The file may be in use or unavailable.",
            InvalidOperationException => ex.Message,
            _ => $"Unexpected error: {ex.Message}"
        };
    }

    public static Exception Unwrap(Exception exception)
    {
        if (exception is AggregateException aggregateException)
            return aggregateException.Flatten().InnerExceptions.FirstOrDefault() ?? exception;

        return exception.InnerException is null
            ? exception
            : Unwrap(exception.InnerException);
    }
}
