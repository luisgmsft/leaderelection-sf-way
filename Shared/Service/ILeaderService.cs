using Microsoft.ServiceFabric.Services.Remoting;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Service
{
    public interface ILeaderService : IService
    {
        Task<List<ApplicationLog>> GetWorkloadChunk();

        Task ReportResult(int total);
    }
}
