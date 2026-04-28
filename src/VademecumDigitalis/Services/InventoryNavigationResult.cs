namespace VademecumDigitalis.Services;

public sealed record MoneyTransferResult(
    int Dukaten,
    int Silbertaler,
    int Heller,
    int Kreuzer);
