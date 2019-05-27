using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CoffeeFreedom.Common.Models;
using HtmlAgilityPack;

namespace CoffeeFreedom.Worker
{
    internal class OrderRequestHandler : RequestHandlerBase
    {
        public async override Task<WorkerResponse> HandleAsync(WorkerRequest request)
        {
            WorkerResponse loginResult = await LogInAsync(request);
            if (loginResult.Status != WorkStatus.Ok)
            {
                return loginResult;
            }

            if (loginResult.Progress.QueuePosition.HasValue)
            {
                return new WorkerResponse(WorkStatus.Conflict);
            }

            Dictionary<string, string> formValues = new Dictionary<string, string>();
            // TODO: Populate the form values here.
            AddCsrf(formValues);

            // Submit the order.
            Document = new HtmlDocument();
            using (HttpRequestMessage orderPostRequest = new HttpRequestMessage(HttpMethod.Post, "/"))
            using (FormUrlEncodedContent form = new FormUrlEncodedContent(formValues))
            {
                AddCookies(orderPostRequest.Headers);
                orderPostRequest.Content = form;

                using (HttpResponseMessage orderPostResponse = await Http.SendAsync(orderPostRequest))
                {
                    Document.Load(await orderPostResponse.Content.ReadAsStreamAsync());
                }
            }

            // Make sure it succeeded.
            if (Document.DocumentNode.SelectSingleNode("//h2[text()='Thankyou']") == null)
            {
                throw new Exception("Unexpected post-order page: " + Document.DocumentNode.OuterHtml);
            }

            // Using the same session, check the queue position.
            Document = new HtmlDocument();
            using (HttpRequestMessage queueGetRequest = new HttpRequestMessage(HttpMethod.Get, "/"))
            {
                AddCookies(queueGetRequest.Headers);

                using (HttpResponseMessage queueGetResponse = await Http.SendAsync(queueGetRequest))
                {
                    Document.Load(await queueGetResponse.Content.ReadAsStreamAsync());
                }
            }

            return ExtractPosition();
        }
    }
}
