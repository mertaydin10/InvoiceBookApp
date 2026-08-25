using FaturaDefteri.Api.Data;
using FaturaDefteri.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaturaDefteri.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController(FaturaDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<HealthResponse>> Get(CancellationToken ct)
    {
        try
        {
            await db.Database.CanConnectAsync(ct);
            return Ok(new HealthResponse("ok", "up"));
        }
        catch
        {
            return StatusCode(503, new HealthResponse("degraded", "down"));
        }
    }
}
