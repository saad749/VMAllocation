using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMAllocation.Web.Models
{
    public class CloudSpecification : ISpec
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

        public double CpuFitnessRatio { get; set; }
        public double MemoryFitnessRatio { get; set; }
        public double NetworkFitnessRatio { get; set; }

        public double? TotalFitness { get; set; }

        public double CpuFitnessRatioRelaxed { get; set; }
        public double MemoryFitnessRatioRelaxed { get; set; }
        public double NetworkFitnessRatioRelaxed { get; set; }

        public double? TotalFitnessRelaxed { get; set; }

        public double? TemporaryPathDistance { get; set; }


        public List<UserRequirement> AllocatedUserRequirements { get; set; }


        public bool AllocateUser(UserRequirement userRequirement)
        {
            AllocatedCpuCount += userRequirement.CpuCount;
            AllocatedMemorySize += userRequirement.MemorySize;
            AllocatedNetworkBandwidth += userRequirement.NetworkBandwidth;

            AllocatedUserRequirements.Add(userRequirement);
            userRequirement.Allocated = true;
            userRequirement.AllocatedCloud = this;

            return false;
        }

        public bool DeAllocateUser(UserRequirement userRequirement)
        {
            AllocatedCpuCount -= userRequirement.CpuCount;
            AllocatedMemorySize -= userRequirement.MemorySize;
            AllocatedNetworkBandwidth -= userRequirement.NetworkBandwidth;

            AllocatedUserRequirements.Remove(userRequirement);
            userRequirement.Allocated = false;
            userRequirement.AllocatedCloud = null;

            return false;
        }

        public double CalculateCost(UserRequirement userRequirement)
        {
            return ((userRequirement.CpuCount * CpuCost) + (userRequirement.MemorySize * MemoryCost) + (userRequirement.NetworkBandwidth * NetworkCost));
        }

        public void CalculateFitnessRatio(UserRequirement userRequirement)
        {
            CpuFitnessRatio = userRequirement.CpuCount/RemainCpuCount;
            MemoryFitnessRatio = userRequirement.MemorySize/RemainMemorySize;
            NetworkFitnessRatio = userRequirement.NetworkBandwidth/RemainNetworkBandwidth;

            CpuFitnessRatioRelaxed = userRequirement.CpuCount / CpuCount;
            MemoryFitnessRatioRelaxed = userRequirement.MemorySize / MemorySize;
            NetworkFitnessRatioRelaxed = userRequirement.NetworkBandwidth / NetworkBandwidth;
        }

        public void CalculateTotalFitness(UserRequirement userRequirement)
        {
            CalculateFitnessRatio(userRequirement);
            if (CpuFitnessRatio <= 1 && MemoryFitnessRatio <= 1 && NetworkFitnessRatio <= 1)
            {
                TotalFitness = CpuFitnessRatio * MemoryFitnessRatio * NetworkFitnessRatio;
            }
            else
            {
                TotalFitness = null;
            }

            if (CpuFitnessRatioRelaxed <= 1 && MemoryFitnessRatioRelaxed <= 1 && NetworkFitnessRatioRelaxed <= 1)
            {
                TotalFitnessRelaxed = CpuFitnessRatioRelaxed * MemoryFitnessRatioRelaxed * NetworkFitnessRatioRelaxed;
            }
            else
            {
                TotalFitnessRelaxed = null;
            }
        }
    }
}
