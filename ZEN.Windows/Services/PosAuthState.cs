namespace ZEN.Windows.Services;

public static class PosRoles
{
    public const string Administrator = "Administrator";
    public const string Cashier = "Cashier";
    public const string Manager = "Manager"; // For future use
}

public sealed class PosAuthState
{
    public bool IsAuthenticated { get; private set; }
    public string? UserName { get; private set; }
    public string? Role { get; private set; }
    public string? Token { get; private set; }
    public Guid? StoreId { get; private set; }
    public Guid? CashDeskId { get; private set; }
    public Guid? ShiftId { get; private set; }

    public bool IsAdmin => string.Equals(Role, PosRoles.Administrator, StringComparison.OrdinalIgnoreCase);
    public bool IsCashier => string.Equals(Role, PosRoles.Cashier, StringComparison.OrdinalIgnoreCase);
    public bool IsManager => string.Equals(Role, PosRoles.Manager, StringComparison.OrdinalIgnoreCase);

    public event Action? Changed;

    public void SignIn(string? userName, string role, string? token = null, Guid? storeId = null, Guid? cashDeskId = null)
    {
        IsAuthenticated = true;
        UserName = userName;
        Role = role;
        Token = token ?? Token;
        StoreId = storeId ?? StoreId;
        CashDeskId = cashDeskId ?? CashDeskId;
        ShiftId = null; // Reset shift on new login
        Changed?.Invoke();
    }

    public void SetShift(Guid shiftId)
    {
        ShiftId = shiftId;
        Changed?.Invoke();
    }

    public void ClearShift()
    {
        ShiftId = null;
        Changed?.Invoke();
    }

    public void SignOut()
    {
        IsAuthenticated = false;
        UserName = null;
        Role = null;
        Token = null;
        StoreId = null;
        CashDeskId = null;
        ShiftId = null;
        Changed?.Invoke();
    }
}