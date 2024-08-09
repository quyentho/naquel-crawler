
using System.Net.Http.Headers;
using System.Text.Json;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var client = new HttpClient();
        var referenceNumbers = new List<string> {  "288082838",
  "288082843",
  "288082846",
  "288082847",
  "288082853",
  "288082855",
  "288082858",
  "288082873",
  "288082859",
  "288082860",
  "288082863",
  "288082864",
  "288082878",
  "288082880",
  "288082882",
  "288082884",
  "288082885",
  "288082887",
  "288082891",
  "288082893",
  "288082894",
  "288082905",
  "288082903",
  "288082909",
  "288082915",
  "288082924",
  "288082927",
  "288082928",
  "288082929",
  "288082936",
  "288082944",
  "288082970",
  "288082955",
  "288082956",
  "288082954",
  "288082960",
  "288082969",
  "288082962",
  "288082965",
  "288082966" };
        string content1 = JsonSerializer.Serialize(referenceNumbers);
        var content = new StringContent(content1, new MediaTypeHeaderValue("application/json"));

        var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost:7065/NaqelCrawling")
        {
            Content = content
        };

        using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
        {
            response.EnsureSuccessStatusCode();

            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var reader = new StreamReader(stream))
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    Console.WriteLine(line);
                }
            }
        }
    }
}