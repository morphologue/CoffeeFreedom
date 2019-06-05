using System;
using System.Configuration;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using CoffeeFreedom.Common.Models;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace CoffeeFreedom.Worker
{
    public partial class Service : ServiceBase
    {
        private readonly HubConnection _hubConnection;
        
        public Service()
        {
            InitializeComponent();

            _log.Source = ServiceName;

            // Set up SignalR.
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(ConfigurationManager.AppSettings["HubUrl"], options =>
                {
                    options.Transports = HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling;
                    string usernamePassword = $"{GetType().Namespace}:{ConfigurationManager.AppSettings["HubSecret"]}";
                    options.Headers.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes(usernamePassword))}");
                })
                .Build();
            _hubConnection.On<WorkerRequest>("RequestAsync", HandleRequest);

            // Retry on network errors. This was built-in in the old SignalR :(
            Random random = new Random();
            _hubConnection.Closed += async exception =>
            {
                do
                {
                    await Task.Delay(random.Next(0, 4) * 1000);
                    _log.WriteEntry($"Restarting SignalR connection due to exception: {exception}", EventLogEntryType.Warning);
                    try
                    {
                        await _hubConnection.StartAsync();
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                } while (_hubConnection.State != HubConnectionState.Connected);
            };
        }

        protected override void OnStart(string[] args)
        {
            _hubConnection.StartAsync().Wait();
        }

        protected override void OnStop()
        {
            Task.Run(async () => await _hubConnection.StopAsync());
        }

        private void HandleRequest(WorkerRequest request)
        {
            // Select the specific handler type.
            RequestHandlerBase handler = request.Order == null ? (RequestHandlerBase)new PeekRequestHandler() : new OrderRequestHandler();
            WorkerResponse response;

            // Call the handler.
            try
            {
                response = handler.HandleAsync(request).Result;
            }
            catch (Exception ex)
            {
                _log.WriteEntry($"Exception: {ex}", EventLogEntryType.Error);
                response = new WorkerResponse(WorkStatus.Error);
            }

            // Send the response.
            response.Guid = request.Guid;
            _hubConnection.InvokeAsync("RespondAsync", response);
            _log.WriteEntry($"Sending response {response.Guid} for user {request.Username}");
        }
    }
}
