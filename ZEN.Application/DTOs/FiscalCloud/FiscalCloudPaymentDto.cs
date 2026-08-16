namespace ZEN.Application.DTOs.FiscalCloud;

public record FiscalCloudPaymentDto(
    int? TypeCode,
    decimal Amount,
    bool? UseBankTerminal,
    Guid? BankTerminal,
    string? BankTerminalCurrency,
    string? BankTerminalRRN,
    string? BankTerminalOperationNumber
);
