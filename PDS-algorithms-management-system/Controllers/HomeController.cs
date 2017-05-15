using System.Web.Mvc;
using Enterprise.Models;

namespace Enterprise.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Сторінка опису програмного продукта.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Сторінка контактної інформації.";

            return View();
        }

	    public ActionResult Error()
	    {
		    return View();
	    }

	    public ActionResult NotFound()
	    {
		    return View();
	    }
    }
}