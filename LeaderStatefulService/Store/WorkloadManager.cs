using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace LeaderStatefulService.Store
{
    [DataContract]
    public class WorkloadManager
    {
        const int _defaultElements = 100;
        private readonly WorkloadStore _store;

        [DataMember]
        public int AggregatedTotal { get; set; }
        [DataMember]
        public int Page { get; set; }
        [DataMember]
        public int ItemsPerPage { get; set; }

        public WorkloadManager()
        {
            _store = new WorkloadStore();
        }

        public WorkloadManager(WorkloadStore store, int itemsPerPage = _defaultElements)
        {
            _store = store;
            ItemsPerPage = itemsPerPage;
        }

        public Task<List<ApplicationLog>> GetNextChunk()
        {
            var result = _store.ApplicationLogs
                .Skip(Page * ItemsPerPage)
                .Take(ItemsPerPage).ToList();

            Page = Page + 1;

            return Task.FromResult(result);
        }
    }
}
