namespace Stateless;

internal static class TaskResult
{
    internal static readonly Task Done = FromResult(1);

    static Task<T> FromResult<T>(T value)
    {
        TaskCompletionSource<T> tcs = new();
        tcs.SetResult(value);
        return tcs.Task;
    }
}