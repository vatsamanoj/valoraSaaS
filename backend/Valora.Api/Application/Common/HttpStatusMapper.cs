using Lab360.Application.Common.Results;
namespace Lab360.Application.Common
{
    public static class HttpStatusMapper
    {
        public static int Map(ApiResult result)
        {
            if (result.Success)
                return StatusCodes.Status200OK;

            if (result.Errors.Any(e => e.Code == "ValidationError"))
                return StatusCodes.Status400BadRequest;

            if (result.Errors.Any(e => e.Code == "UniqueViolation"))
                return StatusCodes.Status409Conflict;

            if (result.Errors.Any(e => e.Code == "Forbidden"))
                return StatusCodes.Status403Forbidden;

            if (result.Errors.Any(e => e.Code == "NotFound"))
                return StatusCodes.Status404NotFound;

            return StatusCodes.Status500InternalServerError;
        }
    }
}