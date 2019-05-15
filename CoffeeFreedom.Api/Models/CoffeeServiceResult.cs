using CoffeeFreedom.Common.Models;

namespace CoffeeFreedom.Api.Models
{
    public class CoffeeServiceResult
    {
        public CoffeeServiceResult(int httpStatusCode)
        {
            HttpStatusCode = httpStatusCode;
        }

        public int HttpStatusCode { get; set; }
        public string Message { get; set; }
        public Progress Progress { get; set; }
    }
}
