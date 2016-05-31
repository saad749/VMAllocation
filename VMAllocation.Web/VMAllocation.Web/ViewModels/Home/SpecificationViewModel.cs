using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMAllocation.Web.Models;

namespace VMAllocation.Web.ViewModels.Home
{
    public class SpecificationViewModel
    {
        public List<CloudSpecification> CloudSpecifications { get; set; }
        public List<UserRequirement> UserRequirements { get; set; }
        public List<Connection> Connections { get; set; }
        public bool Naive { get; set; }
    }
}
