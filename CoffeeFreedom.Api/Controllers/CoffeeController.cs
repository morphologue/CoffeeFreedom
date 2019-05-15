using System.Threading.Tasks;
using CoffeeFreedom.Api.Models;
using CoffeeFreedom.Api.Services;
using CoffeeFreedom.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeFreedom.Controllers
{
    [ApiController]
    public class CoffeeController : ControllerBase
    {
        private readonly ICoffeeService _coffee;

        public CoffeeController(ICoffeeService coffee)
        {
            _coffee = coffee;
        }

        [HttpGet]
        public async Task<ActionResult<Progress>> Get()
        {
            CoffeeServiceResult result = await _coffee.PeekAsync();
            if (result.HttpStatusCode == StatusCodes.Status200OK)
            {
                return Ok(result.Progress);
            }
            return StatusCode(result.HttpStatusCode, result.Message);
        }

        [HttpPost]
        public async Task<ActionResult<Progress>> Post([FromBody] Order order)
        {
            CoffeeServiceResult result = await _coffee.OrderAsync(order);
            if (result.HttpStatusCode == StatusCodes.Status200OK)
            {
                return Ok(result.Progress);
            }
            return StatusCode(result.HttpStatusCode, result.Message);
        }
    }
}
