namespace ZEN.Application.DTOs.FiscalCloud;

public record FiscalCloudReturnPaymentDto(
    int? TypeCode,
    decimal Amount,
    bool? UseBankTerminal,
    string? BankTerminal,
    string? BankTerminalCurrency,
    string? BankTerminalRRN,
    string? BankTerminalOperationNumber,
    IEnumerable<FiscalCloudPrintFormRowDto>? BankTerminalPrintData
);