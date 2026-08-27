using System.Net.Http.Headers;
using System.Net.Http.Json;
using ZEN.Application.DTOs.Admin;
using ZEN.Application.DTOs.Auth;
using ZEN.Application.DTOs.CashDeskMenu;
using ZEN.Application.DTOs.Reports;
using ZEN.Application.DTOs.Sales;
using ZEN.Application.DTOs.Shifts;
using ZEN.Application.DTOs.Sync;

namespace ZEN.Windows.Services.Sync;

public class SyncApiClient
{
    private readonly HttpClient _httpClient;

    public SyncApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public void UpdateBaseUrl(string baseUrl)
    {
        if (!baseUrl.EndsWith("/")) baseUrl += "/";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public Uri? BaseAddress => _httpClient.BaseAddress;

    public async Task<LoginResponse?> LoginWithPinAsync(string pin, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/pin-login", new PinLoginRequest(pin), cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);
                if (result != null)
                {
                    IsApiAvailable = true;
                    return result;
                }
            }
            return null;
        }
        catch (HttpRequestException)
        {
            return null; // API unreachable
        }
        catch (TaskCanceledException)
        {
            return null; // Timeout
        }
    }

    public bool IsApiAvailable { get; private set; } = false;

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

    public async Task<IEnumerable<PrinterDto>> GetPrintersAsync(Guid? storeId = null, CancellationToken cancellationToken = default)
    {
        var uri = storeId.HasValue
            ? $"api/equipments/printers?storeId={storeId.Value}"
            : "api/equipments/printers";
        return await _httpClient.GetFromJsonAsync<IEnumerable<PrinterDto>>(uri, cancellationToken) ?? [];
    }

    public async Task<IEnumerable<PrinterDto>> GetFiscalPrintersAsync(Guid? storeId = null, CancellationToken cancellationToken = default)
    {
        var uri = storeId.HasValue
            ? $"api/equipments/printers/fiscal?storeId={storeId.Value}"
            : "api/equipments/printers/fiscal";
        return await _httpClient.GetFromJsonAsync<IEnumerable<PrinterDto>>(uri, cancellationToken) ?? [];
    }

    public async Task<IEnumerable<PaymentTerminalDto>> GetPaymentTerminalsAsync(Guid? storeId = null, CancellationToken cancellationToken = default)
    {
        var uri = storeId.HasValue
            ? $"api/equipments/payment-terminals?storeId={storeId.Value}"
            : "api/equipments/payment-terminals";
        return await _httpClient.GetFromJsonAsync<IEnumerable<PaymentTerminalDto>>(uri, cancellationToken) ?? [];
    }

    public async Task<IEnumerable<PaymentTerminalDto>> GetPaymentTerminalsAsync(Guid cashDeskId, CancellationToken cancellationToken = default)
    {
        var uri = $"api/equipments/payment-terminals?cashDeskId={cashDeskId}";
        return await _httpClient.GetFromJsonAsync<IEnumerable<PaymentTerminalDto>>(uri, cancellationToken) ?? [];
    }

    public async Task<IEnumerable<ScannerDto>> GetScannersAsync(Guid? storeId = null, CancellationToken cancellationToken = default)
    {
        var uri = storeId.HasValue
            ? $"api/equipments/scanners?storeId={storeId.Value}"
            : "api/equipments/scanners";
        return await _httpClient.GetFromJsonAsync<IEnumerable<ScannerDto>>(uri, cancellationToken) ?? [];
    }

    public async Task<IEnumerable<CustomerDisplayDto>> GetCustomerDisplaysAsync(Guid? storeId = null, CancellationToken cancellationToken = default)
    {
        var uri = storeId.HasValue
            ? $"api/equipments/customer-displays?storeId={storeId.Value}"
            : "api/equipments/customer-displays";
        return await _httpClient.GetFromJsonAsync<IEnumerable<CustomerDisplayDto>>(uri, cancellationToken) ?? [];
    }

    public async Task<IEnumerable<CashDeskDto>> GetCashDesksAsync(Guid? storeId = null, CancellationToken cancellationToken = default)
    {
        var uri = storeId.HasValue
            ? $"api/equipments/cash-desks?storeId={storeId.Value}"
            : "api/equipments/cash-desks";
        return await _httpClient.GetFromJsonAsync<IEnumerable<CashDeskDto>>(uri, cancellationToken) ?? [];
    }

    public async Task<IEnumerable<FlatMenuItemDto>> GetCashDeskMenuItemsAsync(Guid cashDeskId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<IEnumerable<FlatMenuItemDto>>($"api/equipments/cash-desks/{cashDeskId}/menu-items", cancellationToken) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<IEnumerable<GroupDto>> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<IEnumerable<GroupDto>>("api/equipments/groups", cancellationToken) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void SetBearerToken(string? token)
    {
        _httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<SaleDto?> CreateSaleAsync(CreateSaleRequest request, CancellationToken cancellationToken = default)
    {
        var result = await CreateSaleWithResultAsync(request, cancellationToken);
        return result.Sale;
    }

    public async Task<(SaleDto? Sale, string? Error)> CreateSaleWithResultAsync(CreateSaleRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"[SyncApiClient] Creating sale - BaseAddress: {_httpClient.BaseAddress}");
            Console.WriteLine($"[SyncApiClient] Authorization header: {_httpClient.DefaultRequestHeaders.Authorization?.ToString() ?? "NULL"}");
            Console.WriteLine($"[SyncApiClient] Items count: {request.Items.Count()}, Total: {request.Payments.Sum(p => p.Amount)}");
            Console.WriteLine($"[SyncApiClient] CashDeskId: {request.CashDeskId}");

            // Increase timeout for fiscal operations (can take 30+ seconds)
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(60));

            var response = await _httpClient.PostAsJsonAsync("api/sales", request, cts.Token);

            Console.WriteLine($"[SyncApiClient] Response status: {(int)response.StatusCode} {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SaleDto>(cts.Token);
                Console.WriteLine($"[SyncApiClient] Sale created successfully: {result?.Id}");
                return (result, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
            Console.WriteLine($"[SyncApiClient] CreateSale FAILED");
            Console.WriteLine($"[SyncApiClient] Status: {response.StatusCode}");
            Console.WriteLine($"[SyncApiClient] Error body: {errorContent}");

            return (null, ExtractProblemDetail(errorContent) ?? $"HTTP {(int)response.StatusCode}");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || ex.Message.Contains("timeout") || ex.CancellationToken.IsCancellationRequested)
        {
            var errorMsg = "Sale request timed out (60s). This may indicate FiscalCloud agent is slow or unreachable.";
            Console.WriteLine($"[SyncApiClient] CreateSale TIMEOUT: {errorMsg}");
            return (null, errorMsg);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SyncApiClient] CreateSale EXCEPTION: {ex.GetType().Name}");
            Console.WriteLine($"[SyncApiClient] Message: {ex.Message}");
            Console.WriteLine($"[SyncApiClient] Stack trace: {ex.StackTrace}");
            return (null, ex.Message);
        }
    }

    public async Task<ShiftDto?> GetCurrentShiftAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = $"api/shifts/current/{storeId}";
            return await _httpClient.GetFromJsonAsync<ShiftDto>(uri, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<ShiftDto?> OpenShiftAsync(Guid cashDeskId, decimal openingCash, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new OpenShiftForCashDeskRequest(cashDeskId, openingCash);
            var response = await _httpClient.PostAsJsonAsync("api/shifts/open-for-cashdesk", request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ShiftDto>(cancellationToken);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<XReportDto?> GetXReportAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = $"api/reports/x/{storeId}";
            return await _httpClient.GetFromJsonAsync<XReportDto>(uri, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<XReportDto?> GetXReportDataAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = $"api/reports/x-data/{storeId}";
            return await _httpClient.GetFromJsonAsync<XReportDto>(uri, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    // Result class for Z Report API calls
    public record ZReportApiResult(ZReportDto? ZReport, string? Error, string? Details);

    public async Task<ZReportApiResult> GetZReportAsync(Guid shiftId, decimal closingCash, string? notes = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new CloseShiftWithReportRequest(shiftId, closingCash, notes);
            var response = await _httpClient.PostAsJsonAsync("api/reports/z", request, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var zReport = await response.Content.ReadFromJsonAsync<ZReportDto>(cancellationToken);
                return new ZReportApiResult(zReport, null, null);
            }
            
            // Try to read error details from response
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            string? error = null;
            string? details = null;
            
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(errorContent);
                if (doc.RootElement.TryGetProperty("error", out var errorProp))
                    error = errorProp.GetString();
                if (doc.RootElement.TryGetProperty("details", out var detailsProp))
                    details = detailsProp.GetString();
            }
            catch
            {
                error = $"HTTP {(int)response.StatusCode}";
                details = errorContent.Length > 500 ? errorContent[..500] : errorContent;
            }
            
            return new ZReportApiResult(null, error, details);
        }
        catch (Exception ex)
        {
            return new ZReportApiResult(null, ex.Message, ex.GetType().Name);
        }
    }

    private static string? ExtractProblemDetail(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("detail", out var detail))
                return detail.GetString();
            if (doc.RootElement.TryGetProperty("title", out var title))
                return title.GetString();
        }
        catch
        {
            // Not JSON - fall back to raw content
        }

        return content.Length > 500 ? content[..500] : content;
    }
}