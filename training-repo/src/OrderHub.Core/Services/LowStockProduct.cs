namespace OrderHub.Core.Services;

public record LowStockProduct(string Sku, string Name, int StockQuantity, int UnitsSoldLast30Days);
