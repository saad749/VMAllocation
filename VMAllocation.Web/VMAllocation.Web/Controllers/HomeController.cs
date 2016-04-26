using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VMAllocation.Web.ViewModels.Home;
using Newtonsoft.Json;

namespace VMAllocation.Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        [HttpPost]
        public ActionResult ProcessTopology(string model)
        {
            SpecificationViewModel specificationModel = JsonConvert.DeserializeObject<SpecificationViewModel>(model);

            Console.WriteLine("Success");

            return View("Result");
        }

    }
}