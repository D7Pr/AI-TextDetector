using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class RequestPayload
{
    public int logprobs { get; set; }
    public int max_tokens { get; set; }
    public string model { get; set; }
    public int n { get; set; }
    public string prompt { get; set; }
    public string stop { get; set; }
    public bool stream { get; set; }
    public double temperature { get; set; }
    public double top_p { get; set; }
}

public class ApiClient
{
    public async Task<string> MakeApiRequestAsync(string url, RequestPayload payload, string authorization)
    {
        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9,hi;q=0.8");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authorization);
            httpClient.DefaultRequestHeaders.Connection.Add("keep-alive");
            httpClient.DefaultRequestHeaders.Add("Origin", "https://platform.openai.com");
            httpClient.DefaultRequestHeaders.Add("Referer", "https://platform.openai.com/");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-site");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36");
            httpClient.DefaultRequestHeaders.Add("sec-ch-ua", "\"Not_A Brand\";v=\"99\", \"Google Chrome\";v=\"109\", \"Chromium\";v=\"109\"");
            httpClient.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
            httpClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"macOS\"");

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}

public class Program
{
    private static readonly List<string> PossibleClasses = new List<string> { "very unlikely", "unlikely", "unclear if it is", "possibly", "likely" };
    private static readonly List<int> ClassMax = new List<int> { 10, 45, 90, 98, 99 };
    public static async Task Main(string[] args)
    {
        Console.Write("Please enter the file name (paragraphs must be in quotations like this: \"text here\" ): ");
        string fileName = Console.ReadLine();
        if (File.Exists(fileName))
        {
            Console.WriteLine($"File '{fileName}' exists.");
        }
        else
        {
            Console.WriteLine($"File '{fileName}' does not exist.");
            Environment.Exit(0);
        }
        var apiClient = new ApiClient();
        var requestPayload = new RequestPayload
        {
            logprobs = 5,
            max_tokens = 1,
            model = "model-detect-v2",
            n = 1,
            prompt = File.ReadAllText(fileName),
            stop = "\n",
            stream = false,
            temperature = 1,
            top_p = 1
        };
        Console.Write("Please enter barrer token: ");
        string auth = Console.ReadLine();
        string authorization = auth.Trim();
        if (authorization.StartsWith("Bearer "))
        {
            authorization = authorization.Substring(7);
        }
        string url = "https://api.openai.com/v1/completions";

        string response = await apiClient.MakeApiRequestAsync(url, requestPayload, authorization);
        JObject jsonResponse = JObject.Parse(response);
        Dictionary<string, double> probs;
        KeyValuePair<string, double> topProb;

        if (jsonResponse.ContainsKey("choices"))
        {
            JObject choices = (JObject)jsonResponse["choices"][0];
            JObject logprobs = (JObject)choices["logprobs"]["top_logprobs"][0];
            probs = CalculateProbabilities(logprobs);
            topProb = GetTopProb(probs);
        }
        else
        {
            throw new Exception("Invalid response received");
        }
        int decimalPlaces = 2;
        double roundedNumber = Math.Round(topProb.Value, decimalPlaces);
        Console.WriteLine($"Type: {topProb.Key}\nAI-Generated Probability: {roundedNumber}%");
    }

    private static Dictionary<string, double> CalculateProbabilities(JObject logprobs)
    {
        Dictionary<string, double> probs = new Dictionary<string, double>();

        foreach (var logprob in logprobs)
        {
            string key = logprob.Key == "\"" ? "\\\"" : logprob.Key;
            double value = 100 * Math.Exp(logprob.Value.Value<double>());
            probs.Add(key, value);
        }

        return probs;
    }


    private static KeyValuePair<string, double> GetTopProb(Dictionary<string, double> probs)
    {
        double keyProb = probs["\\\""];
        string classLabel;

        if (ClassMax[0] < keyProb && keyProb < ClassMax[ClassMax.Count - 1])
        {
            int val = ClassMax.Last(i => i < keyProb);
            classLabel = PossibleClasses[ClassMax.IndexOf(val)];
        }
        else if (ClassMax[0] > keyProb)
        {
            classLabel = PossibleClasses[0];
        }
        else
        {
            classLabel = PossibleClasses[PossibleClasses.Count - 1];
        }

        return new KeyValuePair<string, double>(classLabel, keyProb);
    }
}