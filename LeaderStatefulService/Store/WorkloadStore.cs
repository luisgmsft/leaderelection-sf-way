using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeaderStatefulService.Store
{
    public class WorkloadStore
    {
        public List<ApplicationLog> ApplicationLogs { get; private set; }

        public WorkloadStore()
        {
            InitializeStore();
        }

        private void InitializeStore()
        {
            ApplicationLogs = Enumerable.Range(0, 1000)
                .Select(i =>
                {
                    var appLog = new ApplicationLog { Total = i };
                    return appLog;
                }).ToList();
        }
    }
}
