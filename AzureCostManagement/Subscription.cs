using System.Collections.Generic;

namespace AzureCostManagement
{
    public class Subscription
    {
        public string DisplayName { get; set; }
        public string SubscriptionId { get; set; }
        public string TenantId { get; set; }
        public string State { get; set; }
        public List<ResourceGroup> ResourceGroups { get; set; } = new List<ResourceGroup>();
        
    }
}
