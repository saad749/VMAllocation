using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMAllocation.Web.Models
{
    class SimilarityRatio
    {
        public double CpuCountRatio { get; set; }
        public double MemorySizeRaio { get; set; }
        public double NetworkBandwidthRatio { get; set; }

        public SimilarityRatio GetSimilarityRatio(ISpec numerator, ISpec denominator)
        {
            CpuCountRatio = numerator.CpuCount / denominator.CpuCount;
            MemorySizeRaio = numerator.MemorySize / denominator.MemorySize;
            NetworkBandwidthRatio = numerator.NetworkBandwidth / denominator.NetworkBandwidth;
            return this;
        }
    }
}
