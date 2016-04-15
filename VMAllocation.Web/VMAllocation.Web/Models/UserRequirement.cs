using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMAllocation.Web.Models
{
    public class UserRequirement
    {
        public int UserId { get; set; }
        public string LocationTitle { get; set; }
        public string LocationType { get; set; }
        public double CpuCount { get; set; }
        public double MemorySize { get; set; }
        public double NetworkBandwidth { get; set; }
        public double ExternalBandwidth { get; set; }
        public double DistanceThreshold { get; set; }
        public double CostThreshold { get; set; }
    }
}
