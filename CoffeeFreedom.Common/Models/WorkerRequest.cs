using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CoffeeFreedom.Common.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public class WorkerRequest : WorkerModelBase
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public Order Order { get; set; }
    }
}
