using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FundingPlatform.Web.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("Admin/Reports")]
public class AdminReportsController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}
