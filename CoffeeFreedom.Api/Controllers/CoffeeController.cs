using System.Net.Mime;
using System.Threading.Tasks;
using CoffeeFreedom.Api.Models;
using CoffeeFreedom.Api.Services;
using CoffeeFreedom.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeFreedom.Api.Controllers
{
    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [Route("/coffeefreedom")]
    public class CoffeeController : ControllerBase
    {
        private readonly ICoffeeService _coffee;

        public CoffeeController(ICoffeeService coffee)
        {
            _coffee = coffee;
        }

        [HttpGet]
        public async Task<ActionResult<Progress>> Get() => ToActionResult(await _coffee.PeekAsync());

        [HttpPost]
        public async Task<ActionResult<Progress>> Post([FromBody] Order order) => ToActionResult(await _coffee.OrderAsync(order));

        private ActionResult<Progress> ToActionResult(CoffeeServiceResult coffeeResult)
        {
            if (coffeeResult.HttpStatusCode == StatusCodes.Status200OK)
            {
                return Ok(coffeeResult.Progress);
            }
            return new ContentResult()
            {
                StatusCode = coffeeResult.HttpStatusCode,
                Content = coffeeResult.Message,
                ContentType = MediaTypeNames.Text.Plain
            };
        }
    }
}
