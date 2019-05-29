using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using CoffeeFreedom.Api.Extensions;
using CoffeeFreedom.Api.Models;
using CoffeeFreedom.Common.Extensions;
using CoffeeFreedom.Common.Models;
using Microsoft.Extensions.Logging;

namespace CoffeeFreedom.Api.Services
{
    public interface ICoffeeService
    {
        Task<CoffeeServiceResult> PeekAsync();
        Task<CoffeeServiceResult> OrderAsync(Order order);
    }

    public class CoffeeService : ICoffeeService
    {
        private readonly ILogger<CoffeeService> _log;
        private readonly IHttpContextAccessor _accessor;
        private readonly IWorkPerformer _performer;

        public CoffeeService(ILogger<CoffeeService> log, IHttpContextAccessor accessor, IWorkPerformer performer)
        {
            _log = log;
            _accessor = accessor;
            _performer = performer;
        }

        public async Task<CoffeeServiceResult> PeekAsync()
        {
            WorkerRequest request = new WorkerRequest();
            if (!ExtractCredentials(request, out var errorResult))
            {
                return errorResult;
            }

            return ToCoffeeServiceResult(await _performer.WorkAsync(request));
        }

        public async Task<CoffeeServiceResult> OrderAsync(Order order)
        {
            WorkerRequest request = new WorkerRequest
            {
                Order = order
            };
            if (!ExtractCredentials(request, out var errorResult))
            {
                return errorResult;
            }

            var validationError = order.Validate();
            if (validationError != null)
            {
                return new CoffeeServiceResult(StatusCodes.Status400BadRequest)
                {
                    Message = validationError
                };
            }

            return ToCoffeeServiceResult(await _performer.WorkAsync(request));
        }

        private bool ExtractCredentials(WorkerRequest request, out CoffeeServiceResult errorResult)
        {
            var result = _accessor.HttpContext.Request.TryExtractBasicCredentials(out string username, out string password);
            request.Username = username;
            request.Password = password;
            errorResult = result ? null : new CoffeeServiceResult(StatusCodes.Status401Unauthorized)
            {
                Message = "HTTP basic authentication is required"
            };
            return result;
        }

        private CoffeeServiceResult ToCoffeeServiceResult(WorkerResponse response)
        {
            switch (response?.Status)
            {
                case WorkStatus.Ok:
                    return new CoffeeServiceResult(StatusCodes.Status200OK)
                    {
                        Progress = response.Progress
                    };
                case WorkStatus.BadLogin:
                    return new CoffeeServiceResult(StatusCodes.Status401Unauthorized)
                    {
                        Message = "Invalid user name or password"
                    };
                case WorkStatus.CafeClosed:
                    return new CoffeeServiceResult(StatusCodes.Status503ServiceUnavailable)
                    {
                        Message = "The café is closed"
                    };
                case WorkStatus.Conflict:
                    return new CoffeeServiceResult(StatusCodes.Status409Conflict)
                    {
                        Message = "An order is already in progress"
                    };
                case WorkStatus.Error:
                    return new CoffeeServiceResult(StatusCodes.Status500InternalServerError)
                    {
                        Message = "Internal server error"
                    };
                case null:
                    return new CoffeeServiceResult(StatusCodes.Status504GatewayTimeout)
                    {
                        Message = "Request timed out"
                    };
                default:
                    _log.LogError("Unknown response status {Status}", response.Status);
                    return new CoffeeServiceResult(StatusCodes.Status500InternalServerError)
                    {
                        Message = "Internal server error"
                    };
            }
        }
    }
}
