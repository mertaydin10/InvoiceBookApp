using FaturaDefteri.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FaturaDefteri.Api.Controllers;

[ApiController]
[Route("api/currencies")]
[Authorize]
public class CurrenciesController : ControllerBase
{
    [HttpGet]
    public IActionResult List() => Ok(Currencies.All);
}
