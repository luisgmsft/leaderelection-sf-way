using System;
using System.Collections.Generic;
using System.Fabric;
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

        public async Task<List<ApplicationLog>> GetWorkloadChunk()
        {
            using (var tx = this.StateManager.CreateTransaction())
            {
                await workloads.AddOrUpdateAsync(tx, _applicationLogWorkloadName, manager, (key, instance) =>
                {
                    instance.Page = instance.Page + 1;
                    return instance;
                });

                await tx.CommitAsync();
            }

            var result = await manager.GetNextChunk();

            if (result == null || result.Count == 0)
            {
                using (var tx = this.StateManager.CreateTransaction())
                {
                    await workloads.AddOrUpdateAsync(tx, _applicationLogWorkloadName, manager, (key, instance) =>
                    {
                        instance.AggregatedTotal = 0;
                        instance.Page = 0;
                        return instance;
                    });

                    await tx.CommitAsync();
                }

                result = await manager.GetNextChunk();
            }

            return result;
        }

        public async Task ReportResult(int total)
        {
            using (var tx = this.StateManager.CreateTransaction())
            {
                await workloads.AddOrUpdateAsync(tx, _applicationLogWorkloadName, manager, (key, instance) =>
                {
                    instance.AggregatedTotal = instance.AggregatedTotal + total;
                    return instance;
                });

                await tx.CommitAsync();
            }

            ServiceEventSource.Current.ServiceMessage(
                        this.Context,
                        "Manager fresh data. Leader at {0}. Aggregated: {1}, Page: {2}",
                        Context.NodeContext.NodeName,
                        manager.AggregatedTotal,
                        manager.Page);
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
            workloads = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, WorkloadManager>>("workloads");
            
            using (var tx = this.StateManager.CreateTransaction())
            {
                try
                {
                    manager = await workloads.GetOrAddAsync(tx, _applicationLogWorkloadName, new WorkloadManager());

                    await tx.CommitAsync();

                    ServiceEventSource.Current.ServiceMessage(
                        this.Context,
                        "Manager as the Fenix, under newly Elected Leader at {0}. Aggregated: {1}, Page: {2}",
                        Context.NodeContext.NodeName,
                        manager.AggregatedTotal,
                        manager.Page);
                }
                catch (Exception ex)
                {
                    ServiceEventSource.Current.ServiceMessage(
                        this.Context,
                        "Failure at RunAsync on Leader {0}. {1}",
                        Context.NodeContext.NodeName,
                        ex.Message);
                    throw;
                }
            }

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await Task.Delay(20000, cancellationToken);

                if (manager.Page > 0 && IsDivisble(manager.Page, 3))
                {
                    ServiceEventSource.Current.ServiceMessage(this.Context, "Leader {0} forced takedown...", Context.NodeContext.NodeName);
                    throw new ApplicationException("Force failure");
                }
            }
        }

        private bool IsDivisble(int x, int n)
        {
            return (x % n) == 0;
        }
    }
}
