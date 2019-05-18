using System.Threading.Tasks;
using CoffeeFreedom.Common.Models;

namespace CoffeeFreedom.Worker
{
    internal class PeekRequestHandler : RequestHandlerBase
    {
        public override Task<WorkerResponse> HandleAsync(WorkerRequest request) => LogInAsync(request);
    }
}
