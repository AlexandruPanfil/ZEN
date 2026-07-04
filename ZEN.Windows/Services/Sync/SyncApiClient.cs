using System.Net.Http.Headers;
using System.Net.Http.Json;
using ZEN.Application.DTOs.Sync;

namespace ZEN.Windows.Services.Sync;

public class SyncApiClient
{
    private readonly HttpClient _httpClient;

    public SyncApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ProductsDeltaResponse?> GetProductsDeltaAsync(DateTime? sinceUtc, CancellationToken cancellationToken)
    {
        var sincePart = sinceUtc.HasValue
            ? $"sinceUtc={Uri.EscapeDataString(sinceUtc.Value.ToString("O"))}&"
            : string.Empty;

        var uri = $"api/sync/products?{sincePart}pageSize=500";
        return await _httpClient.GetFromJsonAsync<ProductsDeltaResponse>(uri, cancellationToken);
    }

    public async Task<UploadPaymentsResponse?> UploadPaymentsAsync(UploadPaymentsRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/sync/payments", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<UploadPaymentsResponse>(cancellationToken: cancellationToken);
    }

    public void SetBearerToken(string? token)
    {
        _httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }
}