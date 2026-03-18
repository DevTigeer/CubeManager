namespace CubeManager.Core.Models;

public class SaleItem
{
    public int Id { get; set; }
    public int DailySalesId { get; set; }
    public int? ReservationId { get; set; }
    public string? Description { get; set; }
    public int Amount { get; set; }
    public string PaymentType { get; set; } = "card"; // card / cash / transfer
    public string Category { get; set; } = "revenue"; // revenue / expense
    public DateTime CreatedAt { get; set; }
}

public class DailySales
{
    public int Id { get; set; }
    public string SaleDate { get; set; } = string.Empty;
    public int CardAmount { get; set; }
    public int CashAmount { get; set; }
    public int TransferAmount { get; set; }
    public int TotalRevenue { get; set; }
    public string? Note { get; set; }
}

public class CashBalance
{
    public int Id { get; set; }
    public string BalanceDate { get; set; } = string.Empty;
    public int OpeningBalance { get; set; }
    public int CashIn { get; set; }
    public int CashOut { get; set; }
    public int ClosingBalance { get; set; }
    public string? Note { get; set; }
}
