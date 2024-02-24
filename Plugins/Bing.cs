
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using CsvHelper.Configuration.Attributes;
using Microsoft.Azure.CognitiveServices.Search.WebSearch;
using Microsoft.SemanticKernel;
using Options;

namespace Plugins;

public class Bing
{
    private WebSearchClient client;
    private string apiKey;

    public bool IsOn { get; set; } = false;

    public Bing(BingSearchOptions bingSearchOptions)
    {
        apiKey = bingSearchOptions.ApiKey;
    }

    [KernelFunction("Search")]
    [Description("Searches Bing for the specified query and returns the results")]
    public async Task<BingResults> SearchAsync(string userQuery)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
        var queryString = HttpUtility.ParseQueryString(string.Empty);
        queryString["q"] = userQuery;
        var query = "https://api.bing.microsoft.com/v7.0/search?" + queryString;

        // Run the query
        HttpResponseMessage httpResponseMessage = await client.GetAsync(query).ConfigureAwait(false);

        // Deserialize the response content
        var responseContentString = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
        Newtonsoft.Json.Linq.JObject responseObjects = Newtonsoft.Json.Linq.JObject.Parse(responseContentString);

        // Convert the response to a BingResults object
        var results = new BingResults
        {
            Results = new List<BingResult>()
        };

        foreach (var result in responseObjects["webPages"]["value"])
        {
            results.Results.Add(new BingResult
            {
                Title = result["name"].ToString(),
                Description = result["snippet"].ToString(),
                Url = result["url"].ToString()
            });
        }

        return results;
    }
}

public class BingResults
{
    public List<BingResult> Results { get; set; }
}

public class BingResult
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }
}
