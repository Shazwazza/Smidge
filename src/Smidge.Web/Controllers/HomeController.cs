using Microsoft.AspNet.Mvc;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace Smidge.Web.Controllers
{
    public class HomeController : Controller
    {
        // GET: /<controller>/
        public IActionResult Index()
        {			
            return View();
        }

        public IActionResult SubFolder()
        {           
            return View();
        }

        public IActionResult AllDebug()
        {
            return View();
        }
    }
}
