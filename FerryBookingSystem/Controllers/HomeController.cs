using FerryBookingSystem.Services;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace FerryBookingSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApiService _apiService = new ApiService();

        public async Task<ActionResult> Index()
        {
            try
            {
                var routes = await _apiService.GetRoutesAsync();
                return View(routes);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error loading routes: " + ex.Message;
                return View(new System.Collections.Generic.List<Models.RouteInfo>());
            }
        }

        public ActionResult About()
        {
            ViewBag.Message = "Ferry Booking System";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Contact Us";
            return View();
        }
    }
}