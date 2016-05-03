using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            IAllocation allocationModel = new LoadBalancedAllocationService();
            List<string> results =  allocationModel.Allocate(specificationModel.CloudSpecifications, specificationModel.UserRequirements,
                specificationModel.Connections);
            stopwatch.Stop();
            viewModel.ProcessTime = stopwatch.ElapsedTicks;
            Console.WriteLine("Success");
            int reqFullfilled = 0;
            viewModel.UserRequirementDetails = new List<List<string>>();
            foreach (UserRequirement userRequirement in specificationModel.UserRequirements)
            {
                viewModel.UserRequirementDetails.Add(PrintDetails(userRequirement));
                if (userRequirement.Allocated == true)
                {
                    reqFullfilled++;
                }
                
            }
            viewModel.ReqFullfilledPercentage = ((double)reqFullfilled/ (double)specificationModel.UserRequirements.Count)* (double)100;

            viewModel.CloudSpecificationDetails = new List<List<string>>();
            foreach (CloudSpecification cloudSpecification in specificationModel.CloudSpecifications)
            {
                viewModel.CloudSpecificationDetails.Add(PrintDetails(cloudSpecification));
            }

            viewModel.Results = results;

            viewModel.UtilisationPercentage = new List<string>();
            foreach (CloudSpecification cloudSpecification in specificationModel.CloudSpecifications)
            {
                if (cloudSpecification.CpuCount > cloudSpecification.RemainCpuCount)
                {
                    viewModel.CloudsInUse++;
                    viewModel.UtilisationPercentage.Add($"Cloud Id: {cloudSpecification.UniversalId} | {cloudSpecification.LocationTitle} : " +
                                                        $"{(cloudSpecification.AllocatedCpuCount / cloudSpecification.CpuCount) * 100}%");
                }
            }


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