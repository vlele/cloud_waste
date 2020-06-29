using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureCostManagement
{
    public class Resource
    {
        public string Id { get; set; }
        public string Location { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string ResourceGroupName { get; set; }
    }
}
