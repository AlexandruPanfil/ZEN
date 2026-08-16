using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ZEN.Application.DTOs.FiscalCloud;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;

namespace ZEN.Infrastructure.Services;

public class FiscalCloudService : IFiscalCloudService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public FiscalCloudService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<FiscalCloudSaleResponseDto> SendSaleAsync(
        Printer printer,
        FiscalCloudSaleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ValidatePrinterConfiguration(printer);

        var endpoint = "/api/v1/operations/sale";
        var method = "POST";
        var body = JsonSerializer.Serialize(request);

        try
        {
            var response = await SendRequestAsync<FiscalCloudSaleResponseDto>(
                printer, method, endpoint, body, cancellationToken);

            return response ?? new FiscalCloudSaleResponseDto(
                false, null, null, null, "Empty response from FiscalCloud", null);
        }
        catch (Exception ex)
        {
            return new FiscalCloudSaleResponseDto(
                false, null, null, null, $"Error: {ex.Message}", null);
        }
    }

    public async Task<FiscalCloudSaleResponseDto> SendReturnAsync(
        Printer printer,
        FiscalCloudReturnRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ValidatePrinterConfiguration(printer);

        var endpoint = "/api/v1/operations/return";
        var method = "POST";
        var body = JsonSerializer.Serialize(request);

        try
        {
            var response = await SendRequestAsync<FiscalCloudSaleResponseDto>(
                printer, method, endpoint, body, cancellationToken);

            return response ?? new FiscalCloudSaleResponseDto(
                false, null, null, null, "Empty response from FiscalCloud", null);
        }
        catch (Exception ex)
        {
            return new FiscalCloudSaleResponseDto(
                false, null, null, null, $"Error: {ex.Message}", null);
        }
    }

    public async Task<bool> SendCloseDayAsync(
        Printer printer,
        FiscalCloudCloseDayRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ValidatePrinterConfiguration(printer);

        var endpoint = "/api/v1/operations/closeday";
        var method = "POST";
        var body = JsonSerializer.Serialize(request);

        try
        {
            await SendRequestAsync<object>(printer, method, endpoint, body, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SendIntermediateTotalsAsync(
        Printer printer,
        FiscalCloudIntermediateTotalsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ValidatePrinterConfiguration(printer);

        var endpoint = "/api/v1/operations/intermediatetotals";
        var method = "POST";
        var body = JsonSerializer.Serialize(request);

        try
        {
            await SendRequestAsync<object>(printer, method, endpoint, body, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<FiscalCloudStatusResponseDto> GetStatusAsync(
        Printer printer,
        CancellationToken cancellationToken = default)
    {
        ValidatePrinterConfiguration(printer);

        var endpoint = "/api/v1/status";
        var method = "GET";

        try
        {
            var response = await SendRequestAsync<FiscalCloudStatusResponseDto>(
                printer, method, endpoint, string.Empty, cancellationToken);

            return response ?? new FiscalCloudStatusResponseDto(
                false, false, "Unknown", "Empty response", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            return new FiscalCloudStatusResponseDto(
                false, false, "Error", ex.Message, DateTime.UtcNow);
        }
    }

    private async Task<T?> SendRequestAsync<T>(
        Printer printer,
        string method,
        string endpoint,
        string body,
        CancellationToken cancellationToken)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var signature = GenerateHmacSignature(
            printer.ApiSecret!,
            printer.DeviceId!,
            printer.PointOfSaleId ?? string.Empty,
            timestamp,
            method,
            endpoint,
            body);

        var httpClient = _httpClientFactory.CreateClient();

        // Ensure we have a valid base URL
        var baseUrl = !string.IsNullOrWhiteSpace(printer.FiscalCloudBaseUrl) 
            ? printer.FiscalCloudBaseUrl.TrimEnd('/') 
            : "https://fiscalcloud.md";

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("FiscalCloud Base URL is not configured and default URL is empty.");
        }

        var requestUri = $"{baseUrl}{endpoint}";

        var httpRequest = new HttpRequestMessage(new HttpMethod(method), requestUri);
        httpRequest.Headers.Add("Api-Key", printer.ApiKey);
        httpRequest.Headers.Add("Api-DeviceId", printer.DeviceId);

        if (!string.IsNullOrEmpty(printer.PointOfSaleId))
        {
            httpRequest.Headers.Add("Api-PointOfSaleId", printer.PointOfSaleId);
        }

        httpRequest.Headers.Add("Api-Timestamp", timestamp.ToString());
        httpRequest.Headers.Add("Api-Signature", signature);

        if (!string.IsNullOrEmpty(body) && (method == "POST" || method == "PUT"))
        {
            httpRequest.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        var httpResponse = await httpClient.SendAsync(httpRequest, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var responseContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    private string GenerateHmacSignature(
        string apiSecret,
        string deviceId,
        string pointOfSaleId,
        long timestamp,
        string method,
        string endpoint,
        string body)
    {
        var dataToSign = new StringBuilder();
        dataToSign.Append(deviceId);

        if (!string.IsNullOrEmpty(pointOfSaleId))
        {
            dataToSign.Append(pointOfSaleId);
        }

        dataToSign.Append(timestamp);
        dataToSign.Append(method.ToUpper());
        dataToSign.Append(endpoint);

        if (!string.IsNullOrEmpty(body))
        {
            dataToSign.Append(body);
        }

        var keyBytes = Encoding.UTF8.GetBytes(apiSecret);
        var dataBytes = Encoding.UTF8.GetBytes(dataToSign.ToString());

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(dataBytes);

        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    private void ValidatePrinterConfiguration(Printer printer)
    {
        if (string.IsNullOrEmpty(printer.ApiKey))
            throw new InvalidOperationException("FiscalCloud API Key is not configured.");

        if (string.IsNullOrEmpty(printer.ApiSecret))
            throw new InvalidOperationException("FiscalCloud API Secret is not configured.");

        if (string.IsNullOrEmpty(printer.DeviceId))
            throw new InvalidOperationException("FiscalCloud Device ID is not configured.");
    }
}
