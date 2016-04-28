using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using VMAllocation.Web.ViewModels.Home;
using Newtonsoft.Json;
using VMAllocation.Web.Models;
using VMAllocation.Web.Services;

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
            AllocationResultViewModel viewModel = new AllocationResultViewModel();  

            SpecificationViewModel specificationModel = JsonConvert.DeserializeObject<SpecificationViewModel>(model);

            IAllocation allocationModel = new LoadBalancedAllocationService();
            List<string> results =  allocationModel.Allocate(specificationModel.CloudSpecifications, specificationModel.UserRequirements,
                specificationModel.Connections);

            Console.WriteLine("Success");

            viewModel.UserRequirementDetails = new List<List<string>>();
            foreach (UserRequirement userRequirement in specificationModel.UserRequirements)
            {
                viewModel.UserRequirementDetails.Add(PrintDetails(userRequirement));
            }
            viewModel.CloudSpecificationDetails = new List<List<string>>();
            foreach (CloudSpecification cloudSpecification in specificationModel.CloudSpecifications)
            {
                viewModel.CloudSpecificationDetails.Add(PrintDetails(cloudSpecification));
            }

            viewModel.Results = results;
            return View("Result", viewModel);
        }


        private List<string> PrintDetails(Object o)
        {
            List<string> details = new List<string>();
            PropertyInfo[] infos = o.GetType().GetProperties();

            foreach (PropertyInfo propertyInfo in infos)
            {
                details.Add(propertyInfo.Name + " : " + propertyInfo.GetValue(o));
            }

            return details;
        }

    }
}