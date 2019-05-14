using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CoffeeFreedom.Common.Models;
using HtmlAgilityPack;

namespace CoffeeFreedom.Worker
{
    internal abstract class RequestHandlerBase
    {
        protected static readonly HttpClient Http;
        protected static int? LastKnownQueueLength;

        protected List<string> SessionCookies;
        protected string Csrf;
        protected HtmlDocument ParsedDocument;

        static RequestHandlerBase()
        {
            Http = new HttpClient
            {
                BaseAddress = new Uri(ConfigurationManager.AppSettings["CafeItBaseUrl"])
            };
        }

        public abstract Task<WorkerResponse> HandleAsync(WorkerRequest request);

        protected async Task<WorkerResponse> LogInAsync(WorkerRequest request)
        {
            // Load the login page just to get the anti-CSRF token.
            List<string> loginCookies;
            string loginCsrf;
            using (HttpResponseMessage loginGetResponse = await Http.GetAsync("/account/login"))
            {
                HtmlDocument loginDoc = new HtmlDocument();
                loginDoc.Load(await loginGetResponse.Content.ReadAsStreamAsync());
                loginCookies = ExtractCookies(loginGetResponse.Headers).ToList();
                loginCsrf = ExtractCsrf(loginDoc);
            }

            // Submit the login form.
            ParsedDocument = new HtmlDocument();
            using (HttpRequestMessage loginPostRequest = new HttpRequestMessage(HttpMethod.Post, "/account/login"))
            using (MultipartFormDataContent form = new MultipartFormDataContent())
            {
                form.Add(new StringContent(request.Username), "Username");
                form.Add(new StringContent(request.Password), "Password");
                AddCookies(loginCookies, loginPostRequest.Headers);
                AddCsrf(loginCsrf, form);
                loginPostRequest.Content = form;

                using (HttpResponseMessage loginPostResponse = await Http.SendAsync(loginPostRequest))
                {
                    ParsedDocument.Load(await loginPostResponse.Content.ReadAsStreamAsync());
                    SessionCookies = ExtractCookies(loginPostResponse.Headers).ToList();
                    Csrf = ExtractCsrf(ParsedDocument);
                }
            }

            // Check if the login succeeded.
            if (ParsedDocument.DocumentNode.SelectSingleNode("//h2[text()='Username or password is incorrect.']") != null)
            {
                return new WorkerResponse
                {
                    Guid = request.Guid,
                    QueueLength = LastKnownQueueLength,
                    Status = WorkStatus.BadLogin
                };
            }
            
            // Check if the cafe is closed.
            if (ParsedDocument.DocumentNode.SelectSingleNode("//h2[text()='Cafe is Closed']") != null)
            {
                return new WorkerResponse
                {
                    Guid = request.Guid,
                    Status = WorkStatus.CafeClosed
                };
            }

            // TODO: Populate QueuePosition if an order has been placed, or update LastKnownQueueLength if we're on the ordering page.

            return new WorkerResponse
            {
                Guid = request.Guid,
                QueueLength = LastKnownQueueLength,
                Status = WorkStatus.Ok
            };
        }

        protected IEnumerable<string> ExtractCookies(HttpResponseHeaders headers)
        {
            if (!headers.TryGetValues("Set-Cookie", out IEnumerable<string> values))
            {
                return new string[0];
            }

            return values
                .Where(v => v.StartsWith(".AspNetCore."))
                .Select(v => v.Substring(0, v.IndexOf(';')));
        }

        protected string ExtractCsrf(HtmlDocument doc)
        {
            return doc.DocumentNode
                .SelectSingleNode("//input[@name='__RequestVerificationToken']")
                ?.GetAttributeValue("value", "");
        }

        protected void AddCsrf(string csrf, MultipartFormDataContent form)
        {
            form.Add(new StringContent(csrf), "__RequestVerificationToken");
        }

        protected void AddCookies(IEnumerable<string> cookies, HttpRequestHeaders headers)
        {
            headers.Add("Cookie", string.Join("; ", cookies));
        }
    }
}
