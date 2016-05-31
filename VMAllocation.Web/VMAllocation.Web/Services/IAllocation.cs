using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMAllocation.Web.Models;

namespace VMAllocation.Web.Services
{
    public interface IAllocation
    {
        void Allocate(List<CloudSpecification> cloudSpecifications, List<UserRequirement> userRequirements, List<Connection> connections, bool naive);
        double AverageDistancePerRequest { get; set; }
        List<AllocationResult> AllocationResults { get; set; }
    }
}
