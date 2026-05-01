namespace ShiftLauncher.Core.Utilities;

public sealed class Result
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    public static Result Ok(string message = "") => new() { Success = true, Message = message };
    public static Result Fail(string message) => new() { Success = false, Message = message };
}
