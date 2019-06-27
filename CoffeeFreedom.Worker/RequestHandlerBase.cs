using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
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

        static RequestHandlerBase()
        {
            Http = new HttpClient
            {
                BaseAddress = new Uri(ConfigurationManager.AppSettings["CafeItBaseUrl"])
            };
        }

        protected Dictionary<string, string> Cookies;
        protected string Csrf;
        protected HtmlDocument Document;

        public abstract Task<WorkerResponse> HandleAsync(WorkerRequest request);

        protected async Task<WorkerResponse> LogInAsync(WorkerRequest request)
        {
            // Load the login page just to get CSRF tokens.
            using (HttpResponseMessage loginGetResponse = await Http.GetAsync("/account/login"))
            {
                Document = new HtmlDocument();
                Document.Load(await loginGetResponse.Content.ReadAsStreamAsync());
                SaveCookies(loginGetResponse.Headers);
                SaveCsrf(Document);
            }

            // Submit the login form.
            Document = new HtmlDocument();
            using (HttpRequestMessage loginPostRequest = new HttpRequestMessage(HttpMethod.Post, "/account/login"))
            using (MultipartFormDataContent form = new MultipartFormDataContent())
            {
                form.Add(new StringContent(request.Username), "Username");
                form.Add(new StringContent(request.Password), "Password");
                AddCookies(loginPostRequest.Headers);
                AddCsrf(form);
                loginPostRequest.Content = form;

                using (HttpResponseMessage loginPostResponse = await Http.SendAsync(loginPostRequest))
                {
                    if (loginPostResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        return new WorkerResponse(WorkStatus.BadLogin);
                    }
                    Document.Load(await loginPostResponse.Content.ReadAsStreamAsync());
                    SaveCookies(loginPostResponse.Headers);
                    SaveCsrf(Document);
                }
            }

            // Check if the login succeeded.
            if (Document.DocumentNode.SelectSingleNode("//h2[text()='Username or password is incorrect.']") != null)
            {
                return new WorkerResponse(WorkStatus.BadLogin);
            }
            
            // Check if the cafe is closed.
            if (Document.DocumentNode.SelectSingleNode("//h2[text()='Cafe is Closed']") != null)
            {
                return new WorkerResponse(WorkStatus.CafeClosed);
            }

            // Check if we're on the ordering page.
            HtmlNode queueLengthNode = Document.DocumentNode.SelectSingleNode("//div[@class='coffee-order-footer-timer-numbers']");
            if (queueLengthNode != null)
            {
                LastKnownQueueLength = int.Parse(queueLengthNode.InnerText);
                return new WorkerResponse(WorkStatus.Ok)
                {
                    Progress = new Progress
                    {
                        QueueLength = LastKnownQueueLength
                    }
                };
            }

            // We must be on the "wait" page.
            return ExtractPosition();
        }

        protected WorkerResponse ExtractPosition()
        {
            const string queuePositionPrefix = "You are currently number ";
            HtmlNode queuePositionNode = Document.DocumentNode.SelectSingleNode($"//h2[starts-with(text(), '{queuePositionPrefix}')]");
            if (queuePositionNode == null)
            {
                throw new Exception("Cannot understand queue position page: " + Document.DocumentNode.OuterHtml);
            }

            string numeric = queuePositionNode.InnerText.Substring(queuePositionPrefix.Length);
            numeric = numeric.Substring(0, numeric.IndexOf(' '));
            int position = int.Parse(numeric);

            LastKnownQueueLength = Math.Max(LastKnownQueueLength ?? 0, position);

            return new WorkerResponse(WorkStatus.Ok)
            {
                Progress = new Progress
                {
                    QueueLength = LastKnownQueueLength,
                    QueuePosition = position
                }
            };
        }

        protected void SaveCookies(HttpResponseHeaders headers)
        {
            Cookies = Cookies ?? new Dictionary<string, string>();

            if (!headers.TryGetValues("Set-Cookie", out IEnumerable<string> values))
            {
                return;
            }

            IEnumerable<string[]> newCookies = values
                .Where(v => v.StartsWith(".AspNetCore."))
                .Select(v => v.Substring(0, v.IndexOf(';')).Split('='));
            foreach (string[] cookie in newCookies)
            {
                Cookies[cookie[0]] = cookie[1];
            }
        }

        protected void SaveCsrf(HtmlDocument doc)
        {
            Csrf = doc.DocumentNode
                .SelectSingleNode("//input[@name='__RequestVerificationToken']")
                ?.GetAttributeValue("value", "");
        }

        protected void AddCsrf(MultipartFormDataContent form)
        {
            if (Csrf == null)
            {
                return;
            }
            form.Add(new StringContent(Csrf), "__RequestVerificationToken");
        }

        protected void AddCsrf(Dictionary<string, string> formValues)
        {
            if (Csrf == null)
            {
                return;
            }
            formValues.Add("__RequestVerificationToken", Csrf);
        }

        protected void AddCookies(HttpRequestHeaders headers)
        {
            if (Cookies.Count == 0)
            {
                return;
            }
            headers.Add("Cookie", string.Join("; ", Cookies.Select(c => $"{c.Key}={c.Value}")));
        }
    }
}
