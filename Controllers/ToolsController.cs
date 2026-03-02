using Microsoft.AspNetCore.Mvc;

namespace My_Endpoint.Controllers
{
    [Route("api/[controller]")]
    public class ToolsController : Controller
    {
        // GET api/tools
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}