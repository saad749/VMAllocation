using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMAllocation.Web.Models
{
    public class Connection
    {
        public int StartPointId { get; set; }
        public int EndPointId { get; set; }
        public double Distance { get; set; }
        public double LinkBandwidth { get; set; }

        public double AllocatedBandwidth { get; set; }
        public double RemainBandwidth => LinkBandwidth - AllocatedBandwidth;

        public List<UserCloudAllocation> UtilizingAllocations { get; set; }

        public bool Visited { get; set; }   

    }
}
