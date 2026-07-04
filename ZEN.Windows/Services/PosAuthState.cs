namespace ZEN.Windows.Services;

public static class PosRoles
{
    public const string Administrator = "Administrator";
    public const string Cashier = "Cashier";
}

public sealed class PosAuthState
{
    public bool IsAuthenticated { get; private set; }
    public string? UserName { get; private set; }
    public string? Role { get; private set; }

    public bool IsAdmin => string.Equals(Role, PosRoles.Administrator, StringComparison.OrdinalIgnoreCase);
    public bool IsCashier => string.Equals(Role, PosRoles.Cashier, StringComparison.OrdinalIgnoreCase);

    public event Action? Changed;

    public void SignIn(string? userName, string role)
    {
        IsAuthenticated = true;
        UserName = userName;
        Role = role;
        Changed?.Invoke();
    }

    public void SignOut()
    {
        IsAuthenticated = false;
        UserName = null;
        Role = null;
        Changed?.Invoke();
    }
}
