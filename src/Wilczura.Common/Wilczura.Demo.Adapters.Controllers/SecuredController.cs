using Microsoft.AspNetCore.Mvc;
using Wilczura.Common;

namespace Wilczura.Demo.Adapters.Controllers;

[ApiController]
[Route("[controller]")]
public class SecuredController : ControllerBase
{
    [HttpGet]
    public ActionResult Get()
    {
        return Ok(SystemInfo.GetInfo());
    }
}