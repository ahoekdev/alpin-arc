using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

internal static class ControllerResultExtensions
{
    public static ActionResult<T> ToActionResult<T>(this ControllerBase controller, ServiceResult<T> result)
        where T : notnull
    {
        if (result.Succeeded)
        {
            return result.Value;
        }

        return controller.ToActionResult(result.Error);
    }

    public static IActionResult ToNoContentActionResult(this ControllerBase controller, ServiceResult result)
    {
        if (result.Succeeded)
        {
            return controller.NoContent();
        }

        return controller.ToActionResult(result.Error);
    }

    private static ActionResult ToActionResult(this ControllerBase controller, ServiceError? error)
    {
        return error?.Type switch
        {
            ServiceErrorType.NotFound => controller.NotFound(),
            ServiceErrorType.BadRequest => controller.BadRequest(error.Message),
            ServiceErrorType.Conflict => controller.Conflict(error.Message),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
