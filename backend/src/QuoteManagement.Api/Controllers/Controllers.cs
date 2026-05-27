using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuoteManagement.Application.DTOs;
using QuoteManagement.Application.Interfaces;
using QuoteManagement.Domain.Entities;
using QuoteManagement.Domain.Enums;
using QuoteManagement.Infrastructure.Data;

namespace QuoteManagement.Api.Controllers;

// ─── Products Controller ─────────────────────────────────────
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _products;
    private readonly IProductPriceHistoryRepository _histories;
    private readonly IWebHostEnvironment _env;

    public ProductsController(IProductRepository products, IProductPriceHistoryRepository histories, IWebHostEnvironment env)
    {
        _products = products;
        _histories = histories;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        CancellationToken ct)
    {
        var items = await _products.GetAllAsync(category, search, sort, ct);
        return Ok(items.Select(MapProduct));
    }

    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken ct)
    {
        var items = await _products.GetAllForAdminAsync(ct);
        return Ok(items.Select(MapProduct));
    }

    [HttpGet("popular")]
    public async Task<IActionResult> GetPopular([FromQuery] int count = 6, CancellationToken ct = default)
    {
        var items = await _products.GetPopularAsync(count, ct);
        return Ok(items.Select(MapProduct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var product = await _products.GetByIdAsync(id, ct);
        return product is null ? NotFound() : Ok(MapProduct(product));
    }

    [HttpGet("{id:guid}/price-history")]
    public async Task<IActionResult> GetPriceHistory(Guid id, CancellationToken ct)
    {
        var histories = await _histories.GetByProductIdAsync(id, ct);
        return Ok(histories.Select(h => new ProductPriceHistoryDto(
            h.Id, h.ProductId, h.Price, h.Currency, h.RequestDate, h.Notes)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto, CancellationToken ct)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Category = dto.Category.ToUpperInvariant(),
            Description = dto.Description,
            ModelNumber = dto.ModelNumber,
            Specifications = dto.Specifications,
            BasePrice = dto.BasePrice,
            Currency = "TRY",
            StockQuantity = dto.StockQuantity,
            ImageUrl = dto.ImageUrl ?? "/images/product.png",
            IsActive = true,
            Rating = 4.0m,
            SalesCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _products.AddAsync(product, ct);
        return Ok(MapProduct(created));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductDto dto, CancellationToken ct)
    {
        var product = await _products.GetByIdAsync(id, ct);
        if (product is null) return NotFound();

        product.Name = dto.Name;
        product.Category = dto.Category.ToUpperInvariant();
        product.Description = dto.Description;
        product.ModelNumber = dto.ModelNumber;
        product.Specifications = dto.Specifications;
        product.BasePrice = dto.BasePrice;
        product.StockQuantity = dto.StockQuantity;
        product.ImageUrl = dto.ImageUrl;
        product.IsActive = dto.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await _products.UpdateAsync(product, ct);
        return Ok(MapProduct(product));
    }

    [HttpPost("upload-image")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UploadImage([FromForm] IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Dosya gerekli.");

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest("Geçersiz dosya türü. Sadece jpg, png, gif, webp kabul edilir.");

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest("Dosya boyutu 5MB'den fazla olamaz.");

        var uploadsPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");
        Directory.CreateDirectory(uploadsPath);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsPath, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream, ct);

        var url = $"/uploads/{fileName}";
        return Ok(new { url });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken ct)
    {
        try
        {
            var deleted = await _products.DeleteAsync(id, ct);
            if (!deleted) return NotFound(new { message = "Ürün bulunamadı." });
            return Ok(new { message = "Ürün silindi." });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            when (ex.InnerException?.Message.Contains("foreign key") == true ||
                  ex.InnerException?.Message.Contains("FK_") == true ||
                  ex.InnerException?.Message.Contains("violates") == true)
        {
            return Conflict(new { message = "Bu ürüne ait teklif kayıtları var. Önce ilgili teklifleri silin veya ürünü pasif yapın." });
        }
    }

    private static ProductDto MapProduct(Product p) => new(
        p.Id, p.Name, p.Description, p.Category, p.ModelNumber,
        p.Specifications, p.BasePrice, p.Currency, p.StockQuantity,
        p.IsActive, p.LastRequestPrice, p.LastRequestDate, p.ImageUrl,
        p.Rating, p.SalesCount);
}

// ─── Customers Controller ────────────────────────────────────
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerRepository _customers;
    public CustomersController(ICustomerRepository customers) => _customers = customers;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var items = await _customers.GetAllAsync(ct);
        return Ok(items.Select(c => new CustomerDto(c.Id, c.Name, c.Email, c.Phone, c.Company, c.Address)));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var c = await _customers.GetByIdAsync(id, ct);
        return c is null ? NotFound() : Ok(new CustomerDto(c.Id, c.Name, c.Email, c.Phone, c.Company, c.Address));
    }
}

// ─── Requests Controller ─────────────────────────────────────
[ApiController]
[Route("api/[controller]")]
public class RequestsController : ControllerBase
{
    private readonly IRequestRepository _requests;
    private readonly ICustomerRepository _customers;
    private readonly IProductRepository _products;
    private readonly IProductPriceHistoryRepository _histories;
    private readonly IExcelService _excel;
    private readonly INotificationService _notification;

    public RequestsController(
        IRequestRepository requests,
        ICustomerRepository customers,
        IProductRepository products,
        IProductPriceHistoryRepository histories,
        IExcelService excel,
        INotificationService notification)
    {
        _requests = requests;
        _customers = customers;
        _products = products;
        _histories = histories;
        _excel = excel;
        _notification = notification;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var list = await _requests.GetAllAsync(ct);
        return Ok(list.Select(MapRequest));
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyRequests([FromQuery] string email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email)) return BadRequest("E-posta gerekli.");
        var list = await _requests.GetAllAsync(ct);
        var myRequests = list.Where(r =>
            r.Customer != null &&
            r.Customer.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        return Ok(myRequests.Select(MapRequest));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var r = await _requests.GetByIdAsync(id, ct);
        return r is null ? NotFound() : Ok(MapRequest(r));
    }

    // POST /api/requests — kullanıcı sepetten teklif oluşturur, Excel döner
    [HttpPost]
    public async Task<IActionResult> CreateRequest([FromBody] CreateRequestDto dto, CancellationToken ct)
    {
        // Find or create customer
        var customer = await _customers.GetByEmailAsync(dto.CustomerEmail, ct);
        if (customer is null)
        {
            customer = await _customers.AddAsync(new Customer
            {
                Name = dto.CustomerName,
                Email = dto.CustomerEmail,
                Phone = dto.CustomerPhone,
                Company = dto.CustomerCompany
            }, ct);
        }

        var requestNo = await _requests.GenerateRequestNoAsync(ct);
        var request = new Request
        {
            Id = Guid.NewGuid(),
            RequestNo = requestNo,
            CustomerId = customer.Id,
            RequestDate = DateTime.UtcNow,
            Currency = "TRY",
            Status = RequestStatus.Pending,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var excelItems = new List<ExcelRequestItemDto>();
        foreach (var item in dto.Items)
        {
            var product = await _products.GetByIdAsync(item.ProductId, ct);
            if (product is null) continue;
            request.Items.Add(new RequestItem
            {
                Id = Guid.NewGuid(),
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            excelItems.Add(new ExcelRequestItemDto(
                item.ProductId, product.Name, product.Category,
                item.Quantity, product.LastRequestPrice, product.LastRequestDate));
        }

        await _requests.AddAsync(request, ct);

        // Generate Excel
        var excelBytes = _excel.GenerateRequestExcel(new ExcelRequestDto(
            requestNo, customer.Name, customer.Email, request.RequestDate, excelItems));

        return File(excelBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{requestNo}.xlsx");
    }

    // POST /api/requests/import-excel — admin Excel yükler, ürün + geçmiş fiyat bilgisi döner
    [HttpPost("import-excel")]
    public async Task<IActionResult> ImportExcel([FromForm] IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Excel dosyası gerekli.");

        using var stream = file.OpenReadStream();
        var importResult = _excel.ParseImportExcelFull(stream);

        var results = new List<object>();
        foreach (var row in importResult.Products)
        {
            var product = await _products.GetByIdAsync(row.ProductId, ct);
            var histories = product is not null
                ? (await _histories.GetByProductIdAsync(product.Id, ct)).Take(5).ToList()
                : new List<ProductPriceHistory>();

            results.Add(new
            {
                ProductId = row.ProductId,
                ProductName = product?.Name ?? row.ProductName,
                Category = product?.Category ?? row.Category,
                Quantity = row.Quantity,
                LastPrice = product?.LastRequestPrice,
                LastPriceDate = product?.LastRequestDate,
                HasHistory = product?.LastRequestPrice.HasValue ?? false,
                Message = product?.LastRequestPrice.HasValue == true
                    ? $"Son fiyat: {product.LastRequestPrice:N2} TRY ({product.LastRequestDate:dd.MM.yyyy})"
                    : "Henüz teklif değeri girilmemiş",
                PriceHistory = histories.Select(h => new { h.Price, h.RequestDate, h.Notes }).ToList()
            });
        }

        return Ok(new
        {
            CustomerEmail = importResult.CustomerEmail,
            CustomerName = importResult.CustomerName,
            Products = results
        });
    }

    // POST /api/requests/submit-excel-quote — admin fiyat teklifi gönderir
    [HttpPost("submit-excel-quote")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SubmitExcelQuote([FromBody] SubmitExcelQuoteDto dto, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var excelItems = new List<ExcelRequestItemDto>();

        foreach (var item in dto.Items)
        {
            var product = await _products.GetByIdAsync(item.ProductId, ct);
            if (product is null) continue;

            // Son teklif fiyatını güncelle
            await _products.UpdateLastPriceAsync(item.ProductId, item.UnitPrice, now, ct);

            // Fiyat geçmişine ekle
            await _histories.AddAsync(new ProductPriceHistory
            {
                Id = Guid.NewGuid(),
                ProductId = item.ProductId,
                Price = item.UnitPrice,
                Currency = "TRY",
                RequestDate = now,
                Notes = $"{dto.CustomerEmail} müşterisi için teklif",
                CreatedAt = now
            }, ct);

            excelItems.Add(new ExcelRequestItemDto(
                item.ProductId, product.Name, product.Category,
                item.Quantity, item.UnitPrice, now));
        }

        // Mail simülasyonu
        var excelBytes = _excel.GenerateRequestExcel(new ExcelRequestDto(
            $"TEK-{now:yyyyMMdd-HHmmss}", dto.CustomerName ?? dto.CustomerEmail,
            dto.CustomerEmail, now, excelItems));

        await _notification.SendQuoteEmailAsync(
            dto.CustomerEmail,
            dto.CustomerName ?? dto.CustomerEmail,
            $"TEK-{now:yyyyMMdd}",
            excelBytes, ct);

        return Ok(new { message = "Teklif gönderildi. Müşterinin e-postasına bildirim gönderildi.", sentAt = now });
    }

    // PUT /api/requests/{id}/submit — admin fiyat girer ve gönderir
    [HttpPut("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, [FromBody] SubmitRequestDto dto, CancellationToken ct)
    {
        var request = await _requests.GetByIdAsync(id, ct);
        if (request is null) return NotFound();

        var now = DateTime.UtcNow;
        decimal total = 0;
        var excelItems = new List<ExcelRequestItemDto>();

        foreach (var priceUpdate in dto.Items)
        {
            var item = request.Items.FirstOrDefault(i => i.Id == priceUpdate.RequestItemId);
            if (item is null) continue;

            item.UnitPrice = priceUpdate.UnitPrice;
            item.DiscountRate = priceUpdate.DiscountRate;
            item.LineTotal = priceUpdate.UnitPrice * item.Quantity * (1 - priceUpdate.DiscountRate / 100);
            item.UpdatedAt = now;
            total += item.LineTotal.Value;

            // Update product last price
            await _products.UpdateLastPriceAsync(item.ProductId, priceUpdate.UnitPrice, now, ct);

            // Add price history
            await _histories.AddAsync(new ProductPriceHistory
            {
                Id = Guid.NewGuid(),
                ProductId = item.ProductId,
                Price = priceUpdate.UnitPrice,
                Currency = "TRY",
                RequestDate = now,
                Notes = $"{request.RequestNo} teklifi",
                CreatedAt = now
            }, ct);

            excelItems.Add(new ExcelRequestItemDto(
                item.ProductId, item.Product.Name, item.Product.Category,
                item.Quantity, priceUpdate.UnitPrice, now));
        }

        request.TotalAmount = total;
        request.Status = RequestStatus.Sent;
        request.SentAt = now;
        await _requests.UpdateAsync(request, ct);

        // Generate and send Excel
        var excelBytes = _excel.GenerateRequestExcel(new ExcelRequestDto(
            request.RequestNo, request.Customer.Name,
            request.Customer.Email, now, excelItems));

        await _notification.SendQuoteEmailAsync(
            request.Customer.Email,
            request.Customer.Name,
            request.RequestNo,
            excelBytes, ct);

        return Ok(MapRequest(request));
    }

    private static RequestDto MapRequest(Request r) => new(
        r.Id, r.RequestNo, r.CustomerId,
        r.Customer?.Name ?? "", r.Customer?.Email ?? "", r.Customer?.Company,
        r.RequestDate, r.TotalAmount, r.Currency, r.Status.ToString(),
        r.Notes, r.SentAt,
        r.Items.Select(i => new RequestItemDto(
            i.Id, i.ProductId,
            i.Product?.Name ?? "", i.Product?.Category ?? "",
            i.Quantity, i.UnitPrice, i.DiscountRate, i.LineTotal,
            i.Product?.LastRequestPrice, i.Product?.LastRequestDate)).ToList());
}
