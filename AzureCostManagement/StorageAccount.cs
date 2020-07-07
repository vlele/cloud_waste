using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureCostManagement
{
    public class StorageAccount
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Boolean isUsed { get; set; }
        public Boolean isNotUsed { get; set; }
        public Boolean onlyListLogEntries { get; set; }
        public Boolean isDiagnosticsEnabled { get; set; }
        public Boolean isLoggingEnabled { get; set; }
        public Boolean isUnusedVhdPresent { get; set; }
    }
}
