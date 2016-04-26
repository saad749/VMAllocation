using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMAllocation.Web.Models
{
    public class CloudSpecification
    {
        public int UniversalId { get; set; }
        public string LocationTitle { get; set; }
        public string LocationType { get; set; }
        public double CpuCount { get; set; }
        public double MemorySize { get; set; }
        public double NetworkBandwidth { get; set; }
        //public double ExternalBandwidth { get; set; }
        public double CpuCost { get; set; }
        public double MemoryCost { get; set; }
        public double NetworkCost { get; set; }


        public double AllocatedCpuCount { get; set; }
        public double AllocatedMemorySize { get; set; }
        public double AllocatedNetworkBandwidth { get; set; }

        public double RemainCpuCount => CpuCount - AllocatedCpuCount;
        public double RemainMemorySize => MemorySize - AllocatedMemorySize;
        public double RemainNetworkBandwidth => NetworkBandwidth - AllocatedNetworkBandwidth;


        public List<UserRequirement> AllocatedUserRequirements { get; set; }

    }
}
