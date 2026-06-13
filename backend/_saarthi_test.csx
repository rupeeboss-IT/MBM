using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var handler = new HttpClientHandler();
using var client = new HttpClient(handler);
client.Timeout = TimeSpan.FromSeconds(120);
client.DefaultRequestHeaders.Accept.Clear();
client.DefaultRequestHeaders.Accept.ParseAdd("application/pdf");
client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

var authJson = JsonSerializer.Serialize(new Dictionary<string, string>
{
    ["MARKETPLACE_API_KEY"] = "1ac53887-3967-46d9-a97d-3f524a385b14",
    ["MARKETPLACE_API_SECRET"] = "03976cd6-0c74-4385-b146-10cbddf717e4",
});

using var req = new HttpRequestMessage(HttpMethod.Post, "https://marketplace.emsme.com/uat/saarthi/v1/get-nic-codes");
req.Headers.TryAddWithoutValidation("marketplace-auth", authJson);
req.Headers.TryAddWithoutValidation("X-Request-ID", "csharp-httpclient-test");
req.Content = new StringContent(JsonSerializer.Serialize(new { udyamNumber = "UDYAM-UP-28-0018412" }), Encoding.UTF8, "application/json");

var resp = await client.SendAsync(req);
var body = await resp.Content.ReadAsStringAsync();
Console.WriteLine($"STATUS={(int)resp.StatusCode}");
Console.WriteLine(body[..Math.Min(300, body.Length)]);
