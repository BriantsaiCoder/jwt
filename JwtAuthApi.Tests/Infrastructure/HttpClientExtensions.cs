using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace JwtAuthApi.Tests.Infrastructure;

public static class HttpClientExtensions
{
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNameCaseInsensitive = true
  };

  public static HttpClient AddBearerToken(this HttpClient client, string token)
  {
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    return client;
  }

  public static void SetAuthorizationHeader(this HttpClient client, string token)
  {
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
  }

  public static async Task<HttpResponseMessage> GetWithTokenAsync(
      this HttpClient client,
      string requestUri,
      string token)
  {
    var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    return await client.SendAsync(request);
  }

  public static async Task<HttpResponseMessage> PostWithTokenAsync<T>(
      this HttpClient client,
      string requestUri,
      T content,
      string token)
  {
    var json = JsonSerializer.Serialize(content);
    var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
    var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
    {
      Content = httpContent
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    return await client.SendAsync(request);
  }

  public static async Task<HttpResponseMessage> PostAsJsonAsync<T>(
      this HttpClient client,
      string requestUri,
      T content)
  {
    var json = JsonSerializer.Serialize(content);
    var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
    return await client.PostAsync(requestUri, httpContent);
  }

  public static async Task<HttpResponseMessage> PutWithTokenAsync<T>(
      this HttpClient client,
      string requestUri,
      T content,
      string token)
  {
    var json = JsonSerializer.Serialize(content);
    var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
    var request = new HttpRequestMessage(HttpMethod.Put, requestUri)
    {
      Content = httpContent
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    return await client.SendAsync(request);
  }

  public static async Task<T?> ReadAsJsonAsync<T>(this HttpContent content)
  {
    var json = await content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<T>(json, JsonOptions);
  }
}
