using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VMAllocation.Web.Models
{
    public class UserCloudAllocation
    {
        public CloudSpecification CloudSpecification { get; set; }
        public UserRequirement UserRequirement { get; set; }
        public Connection Connection { get; set; }

    }
}