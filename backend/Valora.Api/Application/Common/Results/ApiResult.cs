namespace Lab360.Application.Common.Results
{
    public class ApiResult
    {
        public bool Success { get; init; }
        public string? Tenant { get; init; }
        public string? Module { get; init; }
        public string? Action { get; init; }
        public string? Message { get; init; }
        public object? Data { get; init; }
        public object? Meta { get; init; }
        public IReadOnlyList<ApiError> Errors { get; init; }
            = Array.Empty<ApiError>();

        public static ApiResult Ok(
            string tenant,
            string module,
            string action,
            object? data = null,
            object? meta = null)
            => new()
            {
                Success = true,
                Tenant = tenant,
                Module = module,
                Action = action,
                Data = data,
                Meta = meta
            };

        public static ApiResult Fail(
            string tenant,
            string module,
            string action,
            params ApiError[] errors)
            => new()
            {
                Success = false,
                Tenant = tenant,
                Module = module,
                Action = action,
                Errors = errors
            };
    }
}