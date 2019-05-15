using CoffeeFreedom.Api.Models;
using CoffeeFreedom.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeFreedom.Api.Services
{
    public interface ICoffeeService
    {
        Task<CoffeeServiceResult> PeekAsync();
        Task<CoffeeServiceResult> OrderAsync(Order order);
    }

    public class CoffeeService : ICoffeeService
    {
        private const string BasicPrefix = "Basic";

        private readonly IHttpContextAccessor _accessor;

        public CoffeeService(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        public async Task<CoffeeServiceResult> PeekAsync()
        {
            WorkerRequest wr = new WorkerRequest();
            if (!DecodeBasicAuth(wr))
            {
                return new CoffeeServiceResult(StatusCodes.Status401Unauthorized)
                {
                    Message = "HTTP basic authentication is required"
                };
            }

            throw new NotImplementedException();
        }

        public async Task<CoffeeServiceResult> OrderAsync(Order order)
        {
            WorkerRequest wr = new WorkerRequest();
            if (!DecodeBasicAuth(wr))
            {
                return new CoffeeServiceResult(StatusCodes.Status401Unauthorized)
                {
                    Message = "HTTP basic authentication is required"
                };
            }

            throw new NotImplementedException();
        }

        private bool DecodeBasicAuth(WorkerRequest wr)
        {
            if (!_accessor.HttpContext.Request.Headers.TryGetValue("Authorization", out StringValues values))
            {
                return false;
            }

            string value = values.ToString();
            if (value == null || !value.StartsWith(BasicPrefix, StringComparison.OrdinalIgnoreCase) || value.Length == BasicPrefix.Length)
            {
                return false;
            }

            string base64 = value.Substring(BasicPrefix.Length).Trim();
            string strung = Encoding.ASCII.GetString(Convert.FromBase64String(base64));
            string[] splat = strung.Split(':');
            if (splat.Length != 2 || splat.Any(s => s == ""))
            {
                return false;
            }

            wr.Username = splat[0];
            wr.Password = splat[1];
            return true;
        }
    }
}
