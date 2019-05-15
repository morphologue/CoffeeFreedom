using System;
using System.Configuration;
using System.ServiceProcess;
using System.Text;
using CoffeeFreedom.Common.Models;
using Microsoft.AspNet.SignalR.Client;

namespace CoffeeFreedom.Worker
{
    public partial class Service : ServiceBase
    {
        private readonly HubConnection _hubConnection;
        private readonly IHubProxy _hubProxy;

        public Service()
        {
            InitializeComponent();

            // SignalR settings
            string url = ConfigurationManager.AppSettings["HubUrl"];
            string secret = ConfigurationManager.AppSettings["HubSecret"];
            string name = ConfigurationManager.AppSettings["HubName"];

            // Set up SignalR.
            _hubConnection = new HubConnection(url);
            string usernamePassword = $"{GetType().Namespace}:{secret}";
            _hubConnection.Headers.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes(usernamePassword))}");
            _hubProxy = _hubConnection.CreateHubProxy(name);
            _hubProxy.On<WorkerRequest>("Request", HandleRequest);
        }

        protected override void OnStart(string[] args)
        {
            _hubConnection.Start().Wait();
        }

        protected override void OnStop()
        {
            _hubConnection.Stop();
        }

        private void HandleRequest(WorkerRequest request)
        {
            _log.WriteEntry($"Received request {request.Guid}");

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
                _log.WriteEntry($"Exception: {ex}");
                response = new WorkerResponse(WorkStatus.Error);
            }

            // Send the response.
            response.Guid = request.Guid;
            _hubProxy.Invoke("Response", response).Wait();

            _log.WriteEntry($"Sent response {response.Guid}");
        }

        // Allow this project to be run as a console app.
        private static void Main()
        {
            Run(new ServiceBase[] {new Service()});
        }
    }
}
