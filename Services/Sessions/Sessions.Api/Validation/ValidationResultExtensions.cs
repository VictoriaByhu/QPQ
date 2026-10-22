using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace Sessions.Api.Validation;

public static class ValidationResultExtensions
{
    public static ActionResult ToValidationProblem(this ControllerBase controller, ValidationResult result)
    {
        foreach (var error in result.Errors)
            controller.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

        return controller.ValidationProblem(controller.ModelState);
    }
}
