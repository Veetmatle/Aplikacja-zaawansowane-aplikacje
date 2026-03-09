using Microsoft.AspNetCore.Mvc;
using ShopApp.Application.Common;

namespace ShopApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected IActionResult FromResult<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(result.Value);
        return result.StatusCode switch
        {
            404 => NotFound(new { error = result.Error }),
            401 => Unauthorized(new { error = result.Error }),
            403 => Forbid(),
            _ => BadRequest(new { error = result.Error })
        };
    }

    protected IActionResult FromResult(Result result)
    {
        if (result.IsSuccess) return NoContent();
        return result.StatusCode switch
        {
            404 => NotFound(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error })
        };
    }
}
