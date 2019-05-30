using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CoffeeFreedom.Common.Extensions;
using CoffeeFreedom.Common.Models;
using HtmlAgilityPack;

namespace CoffeeFreedom.Worker
{
    internal class OrderRequestHandler : RequestHandlerBase
    {
        private static readonly Dictionary<Variant, string> VariantLabels = new Dictionary<Variant, string>
        {
            [Variant.Espresso] = "Espresso",
            [Variant.DoubleEspresso] = "Double Espresso",
            [Variant.DirtyChai] = "Dirty Chai",
            [Variant.ShortMacchiato] = "Short Macchiato",
            [Variant.HotChocolate] = "Hot Chocolate",
            [Variant.LongMacchiato] = "Long Macchiato",
            [Variant.LongBlack] = "Long Black",
            [Variant.Latte] = "Latte",
            [Variant.Cappuccino] = "Cappuccino",
            [Variant.FlatWhite] = "Flat White",
            [Variant.Piccolo] = "Piccolo",
            [Variant.Mocha] = "Mocha",
            [Variant.IcedCoffee] = "Iced Coffee",
            [Variant.Chai] = "Chai"
        };

        private static readonly Dictionary<Size, string> SizeLabels = new Dictionary<Size, string>
        {
            [Size.MiniCup] = "Mini Cup",
            [Size.Small] = "Small",
            [Size.Large] = "Large",
            [Size.KeepCup] = "Keep Cup"
        };

        private static readonly Dictionary<Milk, string> MilkLabels = new Dictionary<Milk, string>
        {
            [Milk.FullCream] = "Full Cream",
            [Milk.Skim] = "Skim Milk",
            [Milk.Soy] = "Soy Milk",
            [Milk.Almond] = "Almond Milk"
        };

        private static readonly Dictionary<Dash, string> DashLabels = new Dictionary<Dash, string>
        {
            [Dash.ColdMilk] = "D/o/cm",
            [Dash.HotMilk] = "D/o/hm",
            [Dash.ColdWater] = "D/o/cw",
            [Dash.HotWater] = "D/o/hw"
        };

        private static readonly Dictionary<decimal, string> SugarLabels = new Dictionary<decimal, string>
        {
            [0.5M] = "1/2 Sugar",
            [1M] = "1 Sugar",
            [2M] = "2 Sugar"  // sic!
        };

        private static readonly Dictionary<decimal, string> ProportionLabels = new Dictionary<decimal, string>
        {
            [0.5M] = "1/2 Full",
            [0.75M] = "3/4 Full"
        };

        private static readonly Dictionary<Customisation, string> CustomisationLabels = new Dictionary<Customisation, string>
        {
            [Customisation.Caramel] = "Caramel",
            [Customisation.Hazelnut] = "Hazelnut",
            [Customisation.ExtraChocolate] = "Extra Choc",
            [Customisation.Weak] = "Weak",
            [Customisation.Strong] = "Strong",
            [Customisation.ExtraHot] = "Extra Hot",
            [Customisation.Warm] = "Warm"
        };

        public override async Task<WorkerResponse> HandleAsync(WorkerRequest request)
        {
            // Validate request.
            string validationError = request.Order.Validate();
            if (validationError != null)
            {
                throw new Exception("Validation error not caught by front end: " + validationError);
            }

            // Log in.
            WorkerResponse loginResult = await LogInAsync(request);
            if (loginResult.Status != WorkStatus.Ok)
            {
                return loginResult;
            }

            // Make sure an order is not already in progress.
            if (loginResult.Progress.QueuePosition.HasValue)
            {
                return new WorkerResponse(WorkStatus.Conflict);
            }

            // Prepare the order.
            Dictionary<string, string> formValues = new Dictionary<string, string>();
            PopulateFormValues(request.Order, formValues);
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

            // Make sure the order succeeded.
            if (Document.DocumentNode.SelectSingleNode("//h2[text()='Thankyou']") == null)
            {
                throw new Exception("Unexpected post-order page: " + Document.DocumentNode.OuterHtml);
            }
            LastKnownQueueLength = (LastKnownQueueLength ?? 0) + 1;

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

        private void PopulateFormValues(Order order, Dictionary<string, string> formValues)
        {
            foreach ((string label, string isSelectedName, string optionIdName, string optionId) in
                GetCodes("//div[@id='order-bases']//*[@class='coffee-step1-type']"))
            {
                formValues.Add(optionIdName, optionId);
                formValues.Add(isSelectedName, VariantLabels[order.Variant] == label ? "true" : "false");
            }

            formValues.Add("SizeChoice", SizeLabels[order.Size]);
            formValues.Add("MilkChoice", order.Milk.HasValue ? MilkLabels[order.Milk.Value] : "No Milk");

            HashSet<string> rawCustomisations = new HashSet<string>(GetRawCustomisations(order));
            foreach ((string label, string isSelectedName, string optionIdName, string optionId) in
                GetCodes("//*[@class='coffee-step2-option-ckeckbox-span']"))
            {
                formValues.Add(optionIdName, optionId);
                formValues.Add(isSelectedName, rawCustomisations.Contains(label) ? "true" : "false");
            }

            formValues.Add("PersonalNote", "CoffeeFreedom");
        }

        private IEnumerable<(string label, string isSelectedName, string optionIdName, string optionId)> GetCodes(string xpath)
        {
            foreach (HtmlNode headingNode in Document.DocumentNode.SelectNodes(xpath))
            {
                HtmlNode isSelectedNode = headingNode.ParentNode.ParentNode.SelectSingleNode(".//input[@type='checkbox']");
                string isSelectedName = isSelectedNode.GetAttributeValue("name", "");
                string optionIdName = isSelectedName.Substring(0, isSelectedName.IndexOf('.')) + ".OptionId";
                HtmlNode optionIdNode = Document.DocumentNode.SelectSingleNode($".//input[@name='{optionIdName}']");
                string optionId = optionIdNode.GetAttributeValue("value", "");
                yield return (headingNode.InnerText, isSelectedName, optionIdName, optionId);
            }
        }

        private IEnumerable<string> GetRawCustomisations(Order order)
        {
            if (order.Dash.HasValue)
            {
                yield return DashLabels[order.Dash.Value];
            }

            switch (order.Sweetener)
            {
                case Sweetener.Sugar:
                    yield return SugarLabels[order.SweetenerQuantity];
                    break;
                case Sweetener.Equal:
                    yield return "2 Equals";
                    break;
                case Sweetener.Honey:
                    yield return "Honey";
                    break;
            }

            if (ProportionLabels.TryGetValue(order.ProportionFull, out string proportion))
            {
                yield return proportion;
            }

            foreach (Customisation customisation in order.Customisations)
            {
                yield return CustomisationLabels[customisation];
            }
        }
    }
}
