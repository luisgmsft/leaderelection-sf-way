using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Shared;
using Shared.Service;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;

namespace WorkerStatelessService
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class WorkerStatelessService : StatelessService
    {
        public WorkerStatelessService(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            // NOTE: Waits to "ensure" Leader starting first. Not realy needed.
            //       Not sure if there's a better way (i.e. service dependency enforcement into SF).
            await Task.Delay(20000);

            long iterations = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    // NOTE: move this to Service Config
                    var serviceUri = new Uri("fabric:/LeaderElectionSFWay/LeaderStatefulService");

                    ILeaderService service = ServiceProxy.Create<ILeaderService>(serviceUri, new ServicePartitionKey(1));

                    var load = await service.GetWorkloadChunk();

                    var total = 0;
                    load.ForEach(e => total = total + e.Total);

                    await service.ReportResult(total);
                }
                catch (Exception ex)
                {

                    throw;
                }

                ServiceEventSource.Current.ServiceMessage(this.Context, "Working-{0}", ++iterations);

                await Task.Delay(TimeSpan.FromSeconds(20), cancellationToken);
            }
        }
    }
}
