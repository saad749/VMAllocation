using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMAllocation.Web.Models
{
    public interface ISpec
    {
        double CpuCount { get; set; }
        double MemorySize { get; set; }
        double NetworkBandwidth { get; set; }
    }
}
