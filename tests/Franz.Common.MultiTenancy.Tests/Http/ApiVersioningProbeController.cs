using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Franz.Common.MultiTenancy.Tests.Http;

[ApiController]
[ApiVersion("1.0")]
[Route("versioning-probe")]
public sealed class ApiVersioningProbeController : ControllerBase
{
  [HttpGet]
  public IActionResult Get()
  {
    return Ok(new { Value = "ok" });
  }
}
