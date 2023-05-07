using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace MdsCloud.Identity.Presentation.DTOs;

[DefaultStatusCode(DefaultStatusCode)]
public class InternalServerErrorResponse : ObjectResult
{
    private const int DefaultStatusCode = StatusCodes.Status500InternalServerError;

    /// <summary>
    /// Initializes a new instance of the <see cref="OkObjectResult"/> class.
    /// </summary>
    /// <param name="value">The content to format into the entity body.</param>
    public InternalServerErrorResponse(object? value)
        : base(value)
    {
        StatusCode = DefaultStatusCode;
    }
}
