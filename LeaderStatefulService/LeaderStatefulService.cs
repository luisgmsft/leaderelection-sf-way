using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using LeaderStatefulService.Store;
using Shared;
using Shared.Service;

namespace LeaderStatefulService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class LeaderStatefulService : StatefulService, ILeaderService
    {
        const string _applicationLogWorkloadName = "applicationlog-workload";

        WorkloadManager manager = null;
        IReliableDictionary<string, WorkloadManager> workloads;

        public LeaderStatefulService(StatefulServiceContext context)
            : base(context)
        { }

        public Task<List<ApplicationLog>> GetWorkloadChunk()
        {
            return manager.GetNextChunk();
        }

        public async Task ReportResult(int total)
        {
            using (var tx = this.StateManager.CreateTransaction())
            {
                await workloads.AddOrUpdateAsync(tx, _applicationLogWorkloadName, manager, (key, instance) =>
                {
                    instance.AggregatedTotal = manager.AggregatedTotal + total;
                    return instance;
                });

                await tx.CommitAsync();
            }
        }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[] { new ServiceReplicaListener(context =>
                this.CreateServiceRemotingListener(context)) };
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            workloads = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, WorkloadManager>>("workloads");
            
            using (var tx = this.StateManager.CreateTransaction())
            {
                try
                {
                    manager = await workloads.GetOrAddAsync(tx, _applicationLogWorkloadName, new WorkloadManager(new WorkloadStore()));

                    await tx.CommitAsync();
                }
                catch (Exception ex)
                {

                    throw;
                }
                
            }

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                //using (var tx = this.StateManager.CreateTransaction())
                //{
                //    var result = await workloads.TryGetValueAsync(tx, "Counter");

                //    ServiceEventSource.Current.ServiceMessage(this.Context, "Current Counter Value: {0}",
                //        result.HasValue ? result.Value.ToString() : "Value does not exist.");

                //    await workloads.AddOrUpdateAsync(tx, "Counter", 0, (key, value) => { value.Total = value.Total + 1; return value; });

                //    // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                //    // discarded, and nothing is saved to the secondary replicas.
                //    await tx.CommitAsync();
                //}

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}
