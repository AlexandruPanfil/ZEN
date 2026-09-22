using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ZEN.Application.DTOs.Admin;
using ZEN.Application.Services.Interfaces;
using ZEN.Application.DTOs.Auth;
using ZEN.Application.DTOs.CashDeskMenu;
using ZEN.Application.DTOs.Products;
using ZEN.Application.DTOs.Reports;
using ZEN.Application.DTOs.Sales;
using ZEN.Application.DTOs.Shifts;
using ZEN.Application.DTOs.Sync;
using ZEN.Application.Services.Interfaces;
using ServiceTaxDto = ZEN.Application.Services.Interfaces.TaxDto;
using ServiceCreateTaxRequest = ZEN.Application.Services.Interfaces.CreateTaxRequest;
using ServiceUpdateTaxRequest = ZEN.Application.Services.Interfaces.UpdateTaxRequest;

namespace ZEN.Windows.Services.Sync;

public sealed class SyncConflictHttpException : HttpRequestException
{
    public SyncConflictHttpException(
        string entityType,
        string responseBody,
        long? expectedVersion,
        long? currentVersion,
        string? recordJson)
        : base($"Synchronization conflict for {entityType}: {responseBody}", null, System.Net.HttpStatusCode.Conflict)
    {
        ExpectedVersion = expectedVersion;
        CurrentVersion = currentVersion;
        RecordJson = recordJson;
    }

    public long? ExpectedVersion { get; }
    public long? CurrentVersion { get; }
    public string? RecordJson { get; }
}

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

    public bool HasBearerToken =>
        string.Equals(_httpClient.DefaultRequestHeaders.Authorization?.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(_httpClient.DefaultRequestHeaders.Authorization?.Parameter);

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
            ? $"api/equipments/printers?storeId={storeId.Value}&includeDeleted=true"
            : "api/equipments/printers?includeDeleted=true";
        return await _httpClient.GetFromJsonAsync<IEnumerable<PrinterDto>>(uri, cancellationToken) ?? [];
    }

    public async Task<IEnumerable<PrinterDto>> GetFiscalPrintersAsync(Guid? storeId = null, CancellationToken cancellationToken = default)
    {
        var uri = storeId.HasValue
            ? $"api/equipments/printers/fiscal?storeId={storeId.Value}"
            : "api/equipments/printers/fiscal";
        return await _httpClient.GetFromJsonAsync<IEnumerable<PrinterDto>>(uri, cancellationToken) ?? [];
    }

    public async Task<PrinterDto?> CreatePrinterAsync(
        CreatePrinterRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/equipments/printers", request, cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<PrinterDto>(cancellationToken)
            : null;
    }

    public async Task<PrinterDto?> UpdatePrinterAsync(
        UpdatePrinterRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/equipments/printers/{request.Id}", request, cancellationToken);
        await ThrowIfConflictAsync(response, "printer", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new HttpRequestException(
                $"Printer with ID {request.Id} was not found on the server.",
                null,
                response.StatusCode);
        }

        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<PrinterDto>(cancellationToken)
            : null;
    }

    public async Task<bool> DeletePrinterAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        long baseVersion = 0)
    {
        using var response = await _httpClient.DeleteAsync(
            $"api/equipments/printers/{id}?baseVersion={baseVersion}",
            cancellationToken);
        await ThrowIfConflictAsync(response, "printer", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<ScannerDto?> CreateScannerAsync(
        CreateScannerRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/equipments/scanners", request, cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ScannerDto>(cancellationToken)
            : null;
    }

    public async Task<ScannerDto?> UpdateScannerAsync(
        UpdateScannerRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/equipments/scanners/{request.Id}", request, cancellationToken);
        await ThrowIfConflictAsync(response, "scanner", cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ScannerDto>(cancellationToken)
            : null;
    }

    public async Task<bool> DeleteScannerAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        long baseVersion = 0)
    {
        using var response = await _httpClient.DeleteAsync(
            $"api/equipments/scanners/{id}?baseVersion={baseVersion}",
            cancellationToken);
        await ThrowIfConflictAsync(response, "scanner", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<CustomerDisplayDto?> CreateCustomerDisplayAsync(
        CreateCustomerDisplayRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/equipments/customer-displays", request, cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<CustomerDisplayDto>(cancellationToken)
            : null;
    }

    public async Task<CustomerDisplayDto?> UpdateCustomerDisplayAsync(
        UpdateCustomerDisplayRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/equipments/customer-displays/{request.Id}", request, cancellationToken);
        await ThrowIfConflictAsync(response, "customer display", cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<CustomerDisplayDto>(cancellationToken)
            : null;
    }

    public async Task<bool> DeleteCustomerDisplayAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        long baseVersion = 0)
    {
        using var response = await _httpClient.DeleteAsync(
            $"api/equipments/customer-displays/{id}?baseVersion={baseVersion}",
            cancellationToken);
        await ThrowIfConflictAsync(response, "customer display", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<IEnumerable<PaymentTerminalDto>> GetPaymentTerminalsAsync(Guid? storeId = null, CancellationToken cancellationToken = default)
    {
        var uri = storeId.HasValue
            ? $"api/equipments/payment-terminals?storeId={storeId.Value}&includeDeleted=true"
            : "api/equipments/payment-terminals?includeDeleted=true";
        return await _httpClient.GetFromJsonAsync<IEnumerable<PaymentTerminalDto>>(uri, cancellationToken) ?? [];
    }

    public async Task<IEnumerable<PaymentTerminalDto>> GetPaymentTerminalsAsync(Guid cashDeskId, CancellationToken cancellationToken = default)
    {
        var uri = $"api/equipments/payment-terminals?cashDeskId={cashDeskId}&includeDeleted=true";
        return await _httpClient.GetFromJsonAsync<IEnumerable<PaymentTerminalDto>>(uri, cancellationToken) ?? [];
    }

    public async Task<PaymentTerminalDto?> GetPaymentTerminalAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/equipments/payment-terminals/{id}", cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<PaymentTerminalDto>(cancellationToken)
            : null;
    }

    public async Task<PaymentTerminalDto?> CreatePaymentTerminalAsync(
        CreatePaymentTerminalRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/equipments/payment-terminals", request, cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<PaymentTerminalDto>(cancellationToken)
            : null;
    }

    public async Task<PaymentTerminalDto?> UpdatePaymentTerminalAsync(
        UpdatePaymentTerminalRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/equipments/payment-terminals/{request.Id}", request, cancellationToken);
        await ThrowIfConflictAsync(response, "payment terminal", cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<PaymentTerminalDto>(cancellationToken)
            : null;
    }

    public async Task<bool> DeletePaymentTerminalAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        long baseVersion = 0)
    {
        using var response = await _httpClient.DeleteAsync(
            $"api/equipments/payment-terminals/{id}?baseVersion={baseVersion}",
            cancellationToken);
        await ThrowIfConflictAsync(response, "payment terminal", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<IEnumerable<ScannerDto>> GetScannersAsync(Guid? storeId = null, CancellationToken cancellationToken = default)
    {
        var uri = storeId.HasValue
            ? $"api/equipments/scanners?storeId={storeId.Value}&includeDeleted=true"
            : "api/equipments/scanners?includeDeleted=true";
        return await _httpClient.GetFromJsonAsync<IEnumerable<ScannerDto>>(uri, cancellationToken) ?? [];
    }

    public async Task<IEnumerable<CustomerDisplayDto>> GetCustomerDisplaysAsync(Guid? storeId = null, CancellationToken cancellationToken = default)
    {
        var uri = storeId.HasValue
            ? $"api/equipments/customer-displays?storeId={storeId.Value}&includeDeleted=true"
            : "api/equipments/customer-displays?includeDeleted=true";
        return await _httpClient.GetFromJsonAsync<IEnumerable<CustomerDisplayDto>>(uri, cancellationToken) ?? [];
    }

    public async Task<IEnumerable<CashDeskDto>> GetCashDesksAsync(Guid? storeId = null, CancellationToken cancellationToken = default)
    {
        var uri = storeId.HasValue
            ? $"api/equipments/cash-desks?storeId={storeId.Value}&includeDeleted=true"
            : "api/equipments/cash-desks?includeDeleted=true";
        return await _httpClient.GetFromJsonAsync<IEnumerable<CashDeskDto>>(uri, cancellationToken) ?? [];
    }

    public async Task<CashDeskDetailDto?> GetCashDeskAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/equipments/cash-desks/{id}", cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<CashDeskDetailDto>(cancellationToken)
            : null;
    }

    public async Task<CashDeskDto?> CreateCashDeskAsync(
        CreateCashDeskRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/equipments/cash-desks", request, cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<CashDeskDto>(cancellationToken)
            : null;
    }

    public async Task<CashDeskDto?> UpdateCashDeskAsync(
        UpdateCashDeskRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/equipments/cash-desks/{request.Id}", request, cancellationToken);
        await ThrowIfConflictAsync(response, "cash desk", cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<CashDeskDto>(cancellationToken)
            : null;
    }

    public async Task<bool> DeleteCashDeskAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        long baseVersion = 0)
    {
        using var response = await _httpClient.DeleteAsync(
            $"api/equipments/cash-desks/{id}?baseVersion={baseVersion}",
            cancellationToken);
        await ThrowIfConflictAsync(response, "cash desk", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<IEnumerable<StoreDto>> GetStoresAsync(
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<StoreDto>>(
            "api/equipments/stores?includeDeleted=true",
            cancellationToken) ?? [];
    }

    public async Task<StoreDto?> CreateStoreAsync(
        CreateStoreRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/equipments/stores", request, cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<StoreDto>(cancellationToken)
            : null;
    }

    public async Task<StoreDto?> UpdateStoreAsync(
        UpdateStoreRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/equipments/stores/{request.Id}", request, cancellationToken);
        await ThrowIfConflictAsync(response, "store", cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<StoreDto>(cancellationToken)
            : null;
    }

    public async Task<bool> DeleteStoreAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        long baseVersion = 0)
    {
        using var response = await _httpClient.DeleteAsync(
            $"api/equipments/stores/{id}?baseVersion={baseVersion}",
            cancellationToken);
        await ThrowIfConflictAsync(response, "store", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<IEnumerable<MenuSummaryDto>> GetMenusAsync(
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<MenuSummaryDto>>(
            "api/equipments/menus?includeDeleted=true",
            cancellationToken) ?? [];
    }

    public async Task<MenuDto?> CreateMenuAsync(
        CreateMenuRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/equipments/menus", request, cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<MenuDto>(cancellationToken)
            : null;
    }

    public async Task<MenuDto?> UpdateMenuAsync(
        UpdateMenuRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/equipments/menus/{request.Id}", request, cancellationToken);
        await ThrowIfConflictAsync(response, "menu", cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<MenuDto>(cancellationToken)
            : null;
    }

    public async Task<bool> DeleteMenuAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        long baseVersion = 0)
    {
        using var response = await _httpClient.DeleteAsync(
            $"api/equipments/menus/{id}?baseVersion={baseVersion}",
            cancellationToken);
        await ThrowIfConflictAsync(response, "menu", cancellationToken);
        return response.IsSuccessStatusCode;
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

    public async Task<MenuDto?> GetMenuStructureAsync(Guid menuId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<MenuDto>($"api/equipments/menus/{menuId}/structure", cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<MenuDto?> AddMenuGroupAsync(
        CreateMenuGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/equipments/menus/{request.MenuId}/menu-groups", request, cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<MenuDto>(cancellationToken)
            : null;
    }

    public async Task<MenuDto?> UpdateMenuGroupAsync(
        UpdateMenuGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/equipments/menu-groups/{request.Id}", request, cancellationToken);
        await ThrowIfConflictAsync(response, "menu group", cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<MenuDto>(cancellationToken)
            : null;
    }

    public async Task<bool> DeleteMenuGroupAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(
            $"api/equipments/menu-groups/{id}",
            cancellationToken);
        await ThrowIfConflictAsync(response, "menu group", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<MenuDto?> AddProductToMenuGroupAsync(
        AddProductToMenuGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/equipments/menu-groups/{request.MenuGroupId}/products", request, cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<MenuDto>(cancellationToken)
            : null;
    }

    public async Task<bool> RemoveProductFromMenuGroupAsync(
        Guid menuGroupId,
        Guid menuGroupProductId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(
            $"api/equipments/menu-groups/{menuGroupId}/products/{menuGroupProductId}",
            cancellationToken);
        await ThrowIfConflictAsync(response, "menu group product", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<MenuDto?> AddProductToMenuAsync(
        AddProductToMenuRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/equipments/menus/{request.MenuId}/products", request, cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<MenuDto>(cancellationToken)
            : null;
    }

    public async Task<bool> RemoveProductFromMenuAsync(
        Guid menuProductId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(
            $"api/equipments/menus/products/{menuProductId}",
            cancellationToken);
        await ThrowIfConflictAsync(response, "menu product", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<MenuDto?> ReorderMenuGroupsAsync(
        ReorderMenuGroupsRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/equipments/menus/{request.MenuId}/reorder-groups", request, cancellationToken);
        await ThrowIfConflictAsync(response, "menu groups reorder", cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<MenuDto>(cancellationToken)
            : null;
    }

    public async Task<MenuDto?> ReorderMenuGroupProductsAsync(
        ReorderMenuGroupProductsRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/equipments/menu-groups/{request.MenuGroupId}/reorder-products", request, cancellationToken);
        await ThrowIfConflictAsync(response, "menu group products reorder", cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<MenuDto>(cancellationToken)
            : null;
    }

    public async Task<MenuDto?> ReorderMenuRootItemsAsync(
        ReorderMenuRootItemsRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/equipments/menus/{request.MenuId}/reorder-root-items", request, cancellationToken);
        await ThrowIfConflictAsync(response, "menu root items reorder", cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<MenuDto>(cancellationToken)
            : null;
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

    public async Task<(SaleDto? Sale, string? Error)> CreateSaleWithResultAsync(
        CreateSaleRequest request,
        CancellationToken cancellationToken = default,
        int? timeoutSeconds = null)
    {
        // Configurable timeout for fiscal operations (can take 30+ seconds).
        // If a printer timeout is provided, add a 10-second buffer for network/DB overhead.
        // Otherwise default to 120 seconds.
        var effectiveTimeout = timeoutSeconds.HasValue ? timeoutSeconds.Value + 10 : 120;

        try
        {
            Console.WriteLine($"[SyncApiClient] Creating sale - BaseAddress: {_httpClient.BaseAddress}");
            Console.WriteLine($"[SyncApiClient] Authorization header: {_httpClient.DefaultRequestHeaders.Authorization?.ToString() ?? "NULL"}");
            Console.WriteLine($"[SyncApiClient] Items count: {request.Items.Count()}, Total: {request.Payments.Sum(p => p.Amount)}");
            Console.WriteLine($"[SyncApiClient] CashDeskId: {request.CashDeskId}");

            Console.WriteLine($"[SyncApiClient] Using effective timeout: {effectiveTimeout}s");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(effectiveTimeout));

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
            var errorMsg = $"Sale request timed out ({effectiveTimeout}s). This may indicate FiscalCloud agent is slow or unreachable.";
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

    private static async Task ThrowIfConflictAsync(
        HttpResponseMessage response,
        string entityType,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode != System.Net.HttpStatusCode.Conflict)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        long? expectedVersion = null;
        long? currentVersion = null;
        string? recordJson = null;

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("expectedVersion", out var expected)
                && expected.TryGetInt64(out var expectedValue))
            {
                expectedVersion = expectedValue;
            }

            if (document.RootElement.TryGetProperty("currentVersion", out var current)
                && current.TryGetInt64(out var currentValue))
            {
                currentVersion = currentValue;
            }

            if (document.RootElement.TryGetProperty("record", out var record))
            {
                recordJson = record.GetRawText();
            }
        }
        catch (JsonException)
        {
            // Preserve the raw response in the exception when the server did not return JSON.
        }

        throw new SyncConflictHttpException(
            entityType,
            body,
            expectedVersion,
            currentVersion,
            recordJson);
    }

    // Cash Desk Status and Heartbeat
    public async Task<IEnumerable<CashDeskDto>> GetCashDesksWithStatusAsync(Guid? storeId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = storeId.HasValue
                ? $"api/equipments/cash-desks/status?storeId={storeId.Value}"
                : "api/equipments/cash-desks/status";
            return await _httpClient.GetFromJsonAsync<IEnumerable<CashDeskDto>>(uri, cancellationToken) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<CashDeskDto?> SendHeartbeatAsync(Guid cashDeskId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/equipments/cash-desks/{cashDeskId}/heartbeat", null, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CashDeskDto>(cancellationToken);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    // Held Sale Completion
    public async Task<bool> CompleteHeldSaleAsync(Guid heldSaleId, string completedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/sales/held/{heldSaleId}/complete", completedBy, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<HeldSaleDto?> HoldSaleAsync(HoldSaleRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/sales/hold", request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<HeldSaleDto>(cancellationToken);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<HeldSaleDto>> GetHeldSalesAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = $"api/sales/held/{storeId}";
            return await _httpClient.GetFromJsonAsync<IEnumerable<HeldSaleDto>>(uri, cancellationToken) ?? [];
        }
        catch
        {
            return [];
        }
    }

    // Product CRUD
    public async Task<IEnumerable<ProductDto>> GetProductsAsync(string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        var uri = string.IsNullOrWhiteSpace(searchTerm)
            ? "api/products"
            : $"api/products/search/{Uri.EscapeDataString(searchTerm)}";
        return await _httpClient.GetFromJsonAsync<IEnumerable<ProductDto>>(uri, cancellationToken) ?? [];
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ProductDto>($"api/products/{id}", cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<ProductDto?> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/products", request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<ProductDto?> UpdateProductAsync(UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/products/{request.Id}", request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/products/{id}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Category CRUD
    public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<CategoryDto>>("api/products/categories", cancellationToken) ?? [];
    }

    public async Task<CategoryDto?> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/products/categories", request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CategoryDto>(cancellationToken);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<CategoryDto?> UpdateCategoryAsync(UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/products/categories/{request.Id}", request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CategoryDto>(cancellationToken);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/products/categories/{id}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Product Group CRUD
    public async Task<IEnumerable<ProductGroupDto>> GetProductGroupsAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<ProductGroupDto>>("api/products/groups", cancellationToken) ?? [];
    }

    public async Task<ProductGroupDto?> CreateProductGroupAsync(CreateProductGroupRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/products/groups", request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProductGroupDto>(cancellationToken);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<ProductGroupDto?> UpdateProductGroupAsync(UpdateProductGroupRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/products/groups/{request.Id}", request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProductGroupDto>(cancellationToken);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteProductGroupAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/products/groups/{id}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Tax CRUD
    public async Task<IEnumerable<ServiceTaxDto>> GetTaxesAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<ServiceTaxDto>>("api/taxes", cancellationToken) ?? [];
    }

    public async Task<ServiceTaxDto?> CreateTaxAsync(ServiceCreateTaxRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/taxes", request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ServiceTaxDto>(cancellationToken);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<ServiceTaxDto?> UpdateTaxAsync(ServiceUpdateTaxRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/taxes/{request.Id}", request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ServiceTaxDto>(cancellationToken);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteTaxAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/taxes/{id}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SetDefaultTaxAsync(Guid taxId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/taxes/{taxId}/set-default", new { }, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}