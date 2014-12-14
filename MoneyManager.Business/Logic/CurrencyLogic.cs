﻿#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.UI.Popups;
using BugSense;
using MoneyManager.Foundation;
using MoneyManager.Foundation.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#endregion

namespace MoneyManager.Business.Logic
{
    public class CurrencyLogic
    {
        private const string CURRENCY_SERVICE_URL =
            "http://www.freecurrencyconverterapi.com/api/convert?q={0}&compact=y";

        private const string COUNTRIES_SERVICE_URL = "http://www.freecurrencyconverterapi.com/api/v2/countries";

        private static HttpClient httpClient = new HttpClient();

        public static async Task<List<Country>> GetSupportedCountries()
        {
            try
            {
                var jsonString = await GetJsonFromService(COUNTRIES_SERVICE_URL);

                var json = JsonConvert.DeserializeObject(jsonString) as JContainer;

                return (from JProperty token in json.Children().Children().Children()
                    select new Country
                    {
                        Abbreviation = token.Name,
                        CurrencyID = token.Value["currencyId"].ToString(),
                        CurrencyName = token.Value["currencyName"].ToString(),
                        Name = token.Value["name"].ToString(),
                        Alpha3 = token.Value["alpha3"].ToString(),
                        ID = token.Value["id"].ToString(),
                    })
                    .OrderBy(x => x.ID)
                    .ToList();
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog(Translation.GetTranslation("CheckInternetConnectionMessage"),
                Translation.GetTranslation("CheckInternetConnectionTitle"));
                dialog.Commands.Add(new UICommand(Translation.GetTranslation("YesLabel")));

                dialog.ShowAsync();
            }
            return new List<Country>();
        }

        public static async Task<double> GetCurrencyRatio(string currencyFrom, string currencyTo)
        {
            var currencyFromTo = string.Format("{0}-{1}", currencyFrom.ToUpper(), currencyTo.ToUpper());
            var url = string.Format(CURRENCY_SERVICE_URL, currencyFromTo);

            var jsonString = await GetJsonFromService(url);
            jsonString = jsonString.Replace(currencyFromTo, "Conversion");

            return ParseToExchangeRate(jsonString);
        }

        private static double ParseToExchangeRate(string jsonString)
        {
            try
            {
                var typeExample =
                    new
                    {
                        Conversion = new
                        {
                            val = ""
                        }
                    };

                var currency = JsonConvert.DeserializeAnonymousType(jsonString, typeExample);
                //use US culture info for parsing, since service uses us format
                return Double.Parse(currency.Conversion.val, new CultureInfo("en-us"));
            }
            catch (Exception ex)
            {
                BugSenseHandler.Instance.LogException(ex);
            }
            return 1;
        }

        private static async Task<string> GetJsonFromService(string url)
        {
            try
            {
                PrepareHttpClient();
                var req = new HttpRequestMessage(HttpMethod.Get, url);
                var response = await httpClient.SendAsync(req);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                BugSenseHandler.Instance.LogException(ex);
            }
            return "1";
        }

        private static void PrepareHttpClient()
        {
            httpClient = new HttpClient {BaseAddress = new Uri("https://api.SmallInvoice.com/")};
            httpClient.DefaultRequestHeaders.Add("user-agent",
                "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
        }
    }
}