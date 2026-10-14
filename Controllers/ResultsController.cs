using Microsoft.AspNetCore.Mvc;

namespace RaceDay.API_Part2.Controllers
{
    public class ResultsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
