using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VMAllocation.Web.ViewModels.Home
{
    public class AllocationResultViewModel
    {
        public List<string> Results { get; set; }
        public List<List<string>> UserRequirementDetails { get; set; }
            
        public List<List<string>> CloudSpecificationDetails { get; set; }

        public List<string> UtilisationPercentage { get; set; }

        public int CloudsInUse { get; set; }
        public double DistPerReq { get; set; }
        public double ReqFullfilledPercentage { get; set; }
        public long ProcessTime { get; set; }
        public double AverageDistancePerRequest { get; set; }   
    }
}