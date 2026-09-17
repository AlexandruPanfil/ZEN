using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using ZEN.Application.DTOs.FiscalCloud;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;

namespace ZEN.Infrastructure.Services;

public class FiscalCloudService : IFiscalCloudService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FiscalCloudService> _logger;

    // FiscalCloud agent is single-threaded — only one operation at a time.
    // Static => shared across ALL scoped service instances within the process,
    // so concurrent API requests are serialized rather than racing the agent.
    private static readonly SemaphoreSlim _fiscalLock = new(1, 1);

    public FiscalCloudService(IHttpClientFactory httpClientFactory, ILogger<FiscalCloudService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<FiscalCloudSaleResponseDto> SendSaleAsync(
        Printer printer,
        FiscalCloudSaleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ValidatePrinterConfiguration(printer);

        var endpoint = "/api/v1/operations/sale";
        var method = "POST";
        var body = JsonSerializer.Serialize(request, GetJsonOptions());

        try
        {
            // Log the request details for debugging
            _logger.LogDebug("Sending FiscalCloud sale request for printer {PrinterId}. Items: {ItemCount}, Payments: {PaymentCount}", 
                printer.Id, request.Items.Count(), request.Payments.Count());

            var response = await SendRequestAsync<FiscalCloudApiResponse>(
                printer, method, endpoint, body, cancellationToken);

            if (response == null)
            {
                _logger.LogError("FiscalCloud returned null response for sale request");
                return new FiscalCloudSaleResponseDto(false, null, null, null, "Empty response from FiscalCloud", null);
            }

            return MapResponse(response, "sale");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("400"))
        {
            // Log detailed error for 400 Bad Request
            _logger.LogError(ex, "FiscalCloud 400 Bad Request for printer {PrinterId}. Request: {RequestBody}", 
                printer.Id, body.Length > 5000 ? body[..5000] + "..." : body);
            return new FiscalCloudSaleResponseDto(false, null, null, null, $"FiscalCloud validation error: {ex.Message}", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending sale to FiscalCloud for printer {PrinterId}", printer.Id);
            return new FiscalCloudSaleResponseDto(false, null, null, null, $"Error: {ex.Message}", null);
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
        var body = JsonSerializer.Serialize(request, GetJsonOptions());

        try
        {
            var response = await SendRequestAsync<FiscalCloudApiResponse>(
                printer, method, endpoint, body, cancellationToken);

            if (response == null)
                return new FiscalCloudSaleResponseDto(false, null, null, null, "Empty response from FiscalCloud", null);

            return MapResponse(response, "return");
        }
        catch (Exception ex)
        {
            return new FiscalCloudSaleResponseDto(false, null, null, null, $"Error: {ex.Message}", null);
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
        var body = JsonSerializer.Serialize(request, GetJsonOptions());

        try
        {
            await SendRequestAsync<FiscalCloudApiResponse>(printer, method, endpoint, body, cancellationToken);
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
        var body = JsonSerializer.Serialize(request, GetJsonOptions());

        try
        {
            await SendRequestAsync<FiscalCloudApiResponse>(printer, method, endpoint, body, cancellationToken);
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

        // The local FiscalCloud agent has no dedicated /status endpoint.
        // Probe a lightweight real endpoint (/api/v1/devices). A JSON response
        // (even an auth-failure JSON) means the agent is reachable ("Online");
        // an HTML/connection error means it is not.
        var endpoint = "/api/v1/devices";
        var method = "GET";

        try
        {
            var response = await SendRequestAsync<FiscalCloudApiResponse>(
                printer, method, endpoint, string.Empty, cancellationToken);

            // Auth/credential problems are still reported as reachable but not "connected".
            if (response == null)
            {
                return new FiscalCloudStatusResponseDto(
                    false, false, "Error", "Empty response from FiscalCloud agent.", DateTime.UtcNow);
            }

            var isReachable = true;
            var isConnected = response.Success;
            var statusText = response.Success ? "Online" : "Reachable (check credentials)";
            var error = response.Success ? null : (response.Message ?? response.FullExceptionMessage);

            return new FiscalCloudStatusResponseDto(isReachable, isConnected, statusText, error, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            return new FiscalCloudStatusResponseDto(false, false, "Offline", ex.Message, DateTime.UtcNow);
        }
    }

    private static JsonSerializerOptions GetJsonOptions()
    {
        // FiscalCloud's operation endpoints expect:
        //  - camelCase property names (e.g. printOnServer, receiptMode, goodId)
        //  - enum values as STRINGS in their original case (e.g. "FiscalReceiptOnly",
        //    "ByAmount"), NOT integers and NOT lower-camelCased.
        // Plain System.Text.Json serializes enums as integers by default, which the agent
        // rejects - that is why GET /devices (no body) authenticated but POST /operations/sale
        // (body with receiptMode/modifierMode enums) failed. JsonStringEnumConverter makes
        // the enums serialize as their string names while PropertyNamingPolicy.CamelCase
        // keeps the property names camelCase as the contract requires.
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    private static FiscalCloudSaleResponseDto MapResponse(FiscalCloudApiResponse response, string operation)
    {
        var saleData = response.Data;
        var isAlternative = string.Equals(saleData?.ReceiptType, "AlternativeReceipt", StringComparison.OrdinalIgnoreCase);
        var fiscalReceipt = !isAlternative ? saleData?.FiscalReceipt : null;
        var alternativeReceipt = isAlternative ? saleData?.AlternativeReceipt : null;

        var errorMessage = response.Success
            ? null
            : (response.Message ?? response.FullExceptionMessage ?? $"FiscalCloud {operation} failed");

        return new FiscalCloudSaleResponseDto(
            response.Success,
            saleData?.ReceiptType,
            fiscalReceipt,
            alternativeReceipt,
            errorMessage,
            null);
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
        var fiscalTimeoutSeconds = printer.FiscalCloudTimeoutSeconds ?? 30;
        httpClient.Timeout = TimeSpan.FromSeconds(fiscalTimeoutSeconds);

        // Ensure we have a valid base URL.
        // FiscalCloud runs as a local agent by default (swagger server default: http://localhost:50700).
        var baseUrl = !string.IsNullOrWhiteSpace(printer.FiscalCloudBaseUrl)
            ? printer.FiscalCloudBaseUrl.TrimEnd('/')
            : "http://localhost:50700";

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

        _logger.LogInformation("FiscalCloud -> {Method} {Uri} (Printer: {PrinterId}, DeviceId: {DeviceId})", 
            method, requestUri, printer.Id, printer.DeviceId);

        string responseContent = string.Empty;
        var responseStatus = 0;

        // Serialize concurrent FiscalCloud HTTP calls — the local agent is
        // single-threaded and returns "busy" errors under concurrent load.
        var lockAcquired = await _fiscalLock.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
        if (cancellationToken.IsCancellationRequested || !lockAcquired)
        {
            if (!lockAcquired)
            {
                _logger.LogWarning(
                    "Timed out waiting for FiscalCloud lock after 30 seconds. The agent at {BaseUrl} may be stuck.",
                    baseUrl);
            }
            return default;
        }

        try
        {
            using var httpResponse = await httpClient.SendAsync(httpRequest, cancellationToken);
            responseStatus = (int)httpResponse.StatusCode;
            responseContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("FiscalCloud <- {StatusCode} {Body}", responseStatus,
                responseContent.Length > 2000 ? responseContent[..2000] : responseContent);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            var errorMsg = $"FiscalCloud agent not found at {baseUrl}. Please ensure FiscalCloud agent is running on port 50700 (or configured FiscalCloudBaseUrl). Error: {ex.Message}";
            _logger.LogError(ex, errorMsg);
            throw new InvalidOperationException(errorMsg, ex);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var errorMsg = $"FiscalCloud authentication failed. Check ApiKey, ApiSecret, and DeviceId for printer {printer.Id}. Error: {ex.Message}";
            _logger.LogError(ex, errorMsg);
            throw new InvalidOperationException(errorMsg, ex);
        }
        catch (HttpRequestException ex)
        {
            var errorMsg = $"FiscalCloud connection error to {baseUrl}: {ex.Message}";
            _logger.LogError(ex, errorMsg);
            throw new InvalidOperationException(errorMsg, ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || ex.Message.Contains("timeout"))
        {
            var errorMsg = $"FiscalCloud request timed out after {fiscalTimeoutSeconds} seconds. Check if agent at {baseUrl} is responsive.";
            _logger.LogError(ex, errorMsg);
            throw new InvalidOperationException(errorMsg, ex);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Unexpected error communicating with FiscalCloud at {baseUrl}: {ex.Message}";
            _logger.LogError(ex, errorMsg);
            throw new InvalidOperationException(errorMsg, ex);
        }
        finally
        {
            TraceExchange(printer, method, requestUri, body, timestamp, signature, responseStatus, responseContent);
            _fiscalLock.Release();
        }

        // The FiscalCloud agent returns JSON error envelopes (including for 4xx
        // auth failures). Only treat non-JSON (HTML) bodies as fatal - that means
        // we hit a web page instead of the API (wrong host/path/port).
        // Empty responses on error status codes (4xx, 5xx) are treated as errors with no detail.
        var looksLikeJson = !string.IsNullOrWhiteSpace(responseContent) &&
            (responseContent.TrimStart().StartsWith("{") || responseContent.TrimStart().StartsWith("["));

        // If response is empty and status code indicates error, create a meaningful error
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            if (responseStatus >= 400)
            {
                var truncatedBody = body.Length > 500 ? body.Substring(0, 500) + "..." : body;
                var errorMsg = $"FiscalCloud agent returned error {responseStatus} with empty response. " +
                    $"This usually means the request data is invalid. Check: " +
                    $"1) Item prices > 0, 2) Payment amounts > 0, 3) goodId exists in FiscalCloud catalog, 4) taxGroupCode is valid. " +
                    $"Request sent: {truncatedBody}";
                
                _logger.LogError("FiscalCloud error {StatusCode}: {ErrorMessage}", responseStatus, errorMsg);
                throw new InvalidOperationException(errorMsg);
            }
            return default;
        }

        if (!looksLikeJson)
        {
            var truncatedResponse = responseContent.Length > 500 ? responseContent.Substring(0, 500) + "..." : responseContent;
            throw new InvalidOperationException(
                $"FiscalCloud agent at {baseUrl} did not return JSON. " +
                $"Status: {responseStatus}. Response started with: " +
                $"{(truncatedResponse.Length > 80 ? truncatedResponse[..80] : truncatedResponse)}. " +
                $"This usually means the agent is not running or the URL is incorrect.");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize FiscalCloud response: {Response}", responseContent);
            throw new InvalidOperationException($"Failed to parse FiscalCloud response: {ex.Message}", ex);
        }
    }

    private static void TraceExchange(
        Printer printer,
        string method,
        string requestUri,
        string body,
        long timestamp,
        string signature,
        int responseStatus,
        string responseContent)
    {
        try
        {
            var dir = Path.Combine(Path.GetTempPath(), "ZEN");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, "fiscalcloud-trace.log");

            var sb = new StringBuilder();
            sb.AppendLine("============================================================");
            sb.AppendLine($"UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}");
            sb.AppendLine($"METHOD: {method}");
            sb.AppendLine($"URI: {requestUri}");
            sb.AppendLine("--- REQUEST HEADERS (used for signature) ---");
            sb.AppendLine($"Api-Key: {printer.ApiKey}");
            sb.AppendLine($"Api-DeviceId: {printer.DeviceId}");
            sb.AppendLine($"Api-PointOfSaleId: {printer.PointOfSaleId ?? "(not sent)"}");
            sb.AppendLine($"Api-Timestamp: {timestamp}");
            sb.AppendLine($"Api-Signature: {signature}");
            sb.AppendLine("--- REQUEST BODY (exact bytes signed) ---");
            sb.AppendLine(body);
            sb.AppendLine("--- RESPONSE ---");
            sb.AppendLine($"StatusCode: {responseStatus}");
            sb.AppendLine(responseContent);
            sb.AppendLine();

            File.AppendAllText(file, sb.ToString());
        }
        catch
        {
            // Tracing must never break the sale flow.
        }
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

        // FiscalCloud expects ASCII encoding for the secret key (per swagger example)
        var keyBytes = Encoding.ASCII.GetBytes(apiSecret);
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

        var baseUrl = !string.IsNullOrWhiteSpace(printer.FiscalCloudBaseUrl)
            ? printer.FiscalCloudBaseUrl.TrimEnd('/')
            : "http://localhost:50700";

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
            throw new InvalidOperationException("FiscalCloud Base URL is not a valid absolute URL.");
    }
}
