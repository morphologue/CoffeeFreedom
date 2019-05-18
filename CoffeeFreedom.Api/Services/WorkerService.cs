using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoffeeFreedom.Api.Hubs;
using CoffeeFreedom.Common.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CoffeeFreedom.Api.Services
{
    public interface IWorkPerformer
    {
        Task<WorkerResponse> WorkAsync(WorkerRequest request);
    }

    public interface IWorkerResponseHandler
    {
        void HandleResponse(WorkerResponse response);
    }

    public class WorkerService : IWorkPerformer, IWorkerResponseHandler
    {
        private class WorkItem
        {
            internal CancellationTokenSource Cancellation;
            internal WorkerResponse Response;
        }

        private const int WorkerTimeoutMillis = 20_000;

        private readonly ILogger<WorkerService> _log;
        private readonly IHubContext<CoffeeHub, ICoffeeClient> _hub;
        private readonly Dictionary<Guid, WorkItem> _pending;
        private readonly object _pendingLock;

        public WorkerService(ILogger<WorkerService> log, IHubContext<CoffeeHub, ICoffeeClient> hub)
        {
            _log = log;
            _hub = hub;
            _pending = new Dictionary<Guid, WorkItem>();
            _pendingLock = new object();
        }

        public async Task<WorkerResponse> WorkAsync(WorkerRequest request)
        {
            request.Guid = Guid.NewGuid();

            WorkItem item;
            using (CancellationTokenSource cancellation = new CancellationTokenSource())
            {
                item = new WorkItem
                {
                    Cancellation = cancellation
                };

                lock (_pendingLock)
                {
                    _pending.Add(request.Guid, item);
                }

                await _hub.Clients.All.RequestAsync(request);
                try
                {
                    await Task.Delay(WorkerTimeoutMillis, cancellation.Token);
                }
                catch (TaskCanceledException)
                {
                    // We *expect* the task to be cancelled: that's the point.
                }

                // The below lock and removal must be within the 'using' to ensure that
                // HandleResponse cannot get a CancellationTokenSource which has been disposed.
                lock (_pendingLock)
                {
                    _pending.Remove(request.Guid);
                }
            }

            var response = item.Response;
            if (response == null)
            {
                _log.LogWarning("Request {RequestGuid} timed out after {TimeoutMillis} milliseconds", request.Guid, WorkerTimeoutMillis);
            }

            return item.Response;
        }

        public void HandleResponse(WorkerResponse response)
        {
            lock (_pendingLock)
            {
                if (_pending.ContainsKey(response.Guid))
                {
                    WorkItem item = _pending[response.Guid];
                    item.Response = response;
                    // The below cancellation must be inside the lock to ensure that the
                    // CancellationTokenSource can not have been disposed in WorkAsync().
                    item.Cancellation.Cancel();
                    return;
                }
            }

            _log.LogWarning("Response {ResponseGuid} was discarded as no-one was waiting for it", response.Guid);
        }
    }
}
