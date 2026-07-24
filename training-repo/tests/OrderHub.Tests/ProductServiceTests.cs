using OrderHub.Core.Domain;

namespace OrderHub.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task GetAll_ReturnsAllProductsIncludingInactive()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001");
        TestSetup.AddProduct(db, sku: "SKU-A002", isActive: false);

        var products = await service.GetAllAsync();

        Assert.Equal(2, products.Count);
    }

    [Fact]
    public async Task GetActive_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001");
        TestSetup.AddProduct(db, sku: "SKU-A002", isActive: false);

        var products = await service.GetActiveAsync();

        Assert.All(products, p => Assert.True(p.IsActive));
        Assert.Single(products);
    }

    [Fact]
    public async Task GetLowStock_FiltersByThresholdAndSortsAscending()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, stock: 8, sku: "SKU-B008");
        TestSetup.AddProduct(db, stock: 3, sku: "SKU-B003");
        TestSetup.AddProduct(db, stock: 12, sku: "SKU-B012");

        var result = await service.GetLowStockAsync(10);

        Assert.Equal(new[] { 3, 8 }, result.Select(r => r.StockQuantity).ToArray());
    }

    [Fact]
    public async Task GetLowStock_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, stock: 2, sku: "SKU-C001", isActive: true);
        TestSetup.AddProduct(db, stock: 2, sku: "SKU-C002", isActive: false);

        var result = await service.GetLowStockAsync(10);

        Assert.Single(result);
        Assert.Equal("SKU-C001", result[0].Sku);
    }

    [Fact]
    public async Task GetLowStock_UnitsSold_ExcludesCancelledAndOldOrders()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var product = TestSetup.AddProduct(db, stock: 4, sku: "SKU-D001");
        var now = DateTime.UtcNow;

        // 近 30 天內、非 Cancelled → 計入
        TestSetup.AddOrder(db, product, quantity: 5, status: OrderStatus.Shipped, createdAt: now.AddDays(-10));
        // 近 30 天內、Cancelled → 排除
        TestSetup.AddOrder(db, product, quantity: 7, status: OrderStatus.Cancelled, createdAt: now.AddDays(-5));
        // 40 天前、非 Cancelled → 排除
        TestSetup.AddOrder(db, product, quantity: 9, status: OrderStatus.Shipped, createdAt: now.AddDays(-40));

        var result = await service.GetLowStockAsync(10);

        var row = Assert.Single(result);
        Assert.Equal(5, row.UnitsSoldLast30Days);
    }
}
