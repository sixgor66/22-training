using Microsoft.AspNetCore.Mvc;
using OrderHub.Core.Services;
using OrderHub.Web.ViewModels;

namespace OrderHub.Web.Controllers;

public class ProductsController : Controller
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _productService.GetAllAsync();

        var vm = new ProductListViewModel
        {
            Products = products.Select(p => new ProductRowViewModel
            {
                Sku = p.Sku,
                Name = p.Name,
                UnitPrice = p.UnitPrice,
                StockQuantity = p.StockQuantity,
                IsActive = p.IsActive
            }).ToList()
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> LowStock(LowStockViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var threshold = vm.Threshold ?? 10;
        var products = await _productService.GetLowStockAsync(threshold);

        vm.Threshold = threshold;
        vm.Products = products.Select(p => new LowStockRowViewModel
        {
            Sku = p.Sku,
            Name = p.Name,
            StockQuantity = p.StockQuantity,
            UnitsSoldLast30Days = p.UnitsSoldLast30Days
        }).ToList();

        return View(vm);
    }
}

