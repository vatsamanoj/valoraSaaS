namespace Lab360.Application.Common.Results
{    
    public sealed record ApiError(
        string Code,
        string Message,
        string? Field = null
    );
}
