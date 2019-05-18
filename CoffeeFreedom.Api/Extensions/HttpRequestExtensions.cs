using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace CoffeeFreedom.Api.Extensions
{
    public static class HttpRequestExtensions
    {
        private const string BasicPrefix = "Basic";

        public static bool TryExtractBasicCredentials(this HttpRequest request, out string username, out string password)
        {
            username = password = null;

            if (!request.Headers.TryGetValue("Authorization", out StringValues values))
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

            username = splat[0];
            password = splat[1];
            return true;
        }
    }
}
