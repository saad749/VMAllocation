using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VMAllocation.Web.Models;

namespace VMAllocation.Web.Services
{
    public class LoadBalancedAllocationService : IAllocation
    {
        public List<string> Allocate(List<CloudSpecification> cloudSpecifications, List<UserRequirement> userRequirements, List<Connection> connections)
        {
            List<string> results = new List<string>();
            List<UserCloudAllocation> allocations = new List<UserCloudAllocation>();


            int i = 0;
            foreach (UserRequirement userRequirement in userRequirements)
            {
                cloudSpecifications = cloudSpecifications.OrderByDescending(c => c.RemainCpuCount).ToList();


                if (userRequirement.CpuCount < cloudSpecifications[i].RemainCpuCount)
                {
                    if (userRequirement.MemorySize < cloudSpecifications[i].RemainMemorySize)
                    {
                        //First load the connection...
                        if (userRequirement.ExternalBandwidth < connections[i].RemainBandwidth)
                        {
                            
                        }
                    }
                }
                else
                {
                    userRequirements.Remove(userRequirement);
                    results.Add($"{userRequirement.UniversalId} Cannot be placed on any VM. CPU requirements not feasible");
                }
            }



            return results;
        }


    }
}