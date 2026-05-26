namespace PoOcr.Application.Common;

public sealed class ApplicationResult
{
    private ApplicationResult(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public string Error { get ; }

    public static ApplicationResult Success()
    {
        return new ApplicationResult(true, "");
    }

    public static ApplicationResult Failure(string error)
    {
        return new ApplicationResult(false, error);
    }
}