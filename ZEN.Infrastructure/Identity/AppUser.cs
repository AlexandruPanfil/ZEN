using Microsoft.AspNetCore.Identity;
using ZEN.Domain.Entities;

namespace ZEN.Infrastructure.Identity;

public class AppUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PIN { get; set; }
    public string? PinHash { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? StoreId { get; set; }
    public Guid? CashDeskId { get; set; }
    public string? ProfileImagePath { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    public CashDesk? CashDesk { get; set; }
}