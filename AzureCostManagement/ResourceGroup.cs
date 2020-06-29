using System.Collections.Generic;

namespace AzureCostManagement
{
    public class ResourceGroup
    {
        public string Id { get; set; }
        public string Location { get; set; }
        public string Name { get; set; }
        public List<Resource> Resources { get; set; } = new List<Resource>();
    }
}
