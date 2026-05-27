using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuoteManagement.Application.Interfaces;
using QuoteManagement.Domain.Entities;
using QuoteManagement.Domain.Enums;
using QuoteManagement.Infrastructure.Data;
using QuoteManagement.Infrastructure.Repositories;
using QuoteManagement.Infrastructure.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─── Database ────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── Auth ─────────────────────────────────────────────────────
var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Secret"] ?? "super-secret-key-for-jwt-token-auth");
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

// ─── Repositories ─────────────────────────────────────────────
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductPriceHistoryRepository, ProductPriceHistoryRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IRequestRepository, RequestRepository>();

// ─── Services ─────────────────────────────────────────────────
builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddScoped<INotificationService, LogNotificationService>();

// ─── API ──────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Quote Management API", Version = "v1" });
});

// ─── CORS (Next.js frontend) ──────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:3001",
                "http://frontend:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ─── Seeder & schema updates ────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();

    await context.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS users (
            id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            name VARCHAR(200) NOT NULL,
            email VARCHAR(200) NOT NULL UNIQUE,
            password_hash TEXT NOT NULL,
            role VARCHAR(50) NOT NULL DEFAULT 'USER',
            created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        ALTER TABLE products ADD COLUMN IF NOT EXISTS rating NUMERIC(3,2) NOT NULL DEFAULT 4.0;
        ALTER TABLE products ADD COLUMN IF NOT EXISTS sales_count INT NOT NULL DEFAULT 0;
        """);

    var now = DateTime.UtcNow;
    if (!await context.Users.AnyAsync())
    {
        context.Users.Add(new User
        {
            Id = Guid.Parse("f1000000-0000-0000-0000-000000000001"),
            Name = "Admin",
            Email = "admin@piton.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Role = Role.Admin,
            CreatedAt = now,
            UpdatedAt = now
        });
        await context.SaveChangesAsync();
    }

    var catalogIdsList = new List<Guid>
    {
        Guid.Parse("b1000000-0000-0000-0000-000000000001"),
        Guid.Parse("b1000000-0000-0000-0000-000000000002"),
        Guid.Parse("b1000000-0000-0000-0000-000000000003"),
        Guid.Parse("b1000000-0000-0000-0000-000000000004"),
        Guid.Parse("b1000000-0000-0000-0000-000000000005"),
        Guid.Parse("b1000000-0000-0000-0000-000000000006"),
    };

    for (int i = 1; i <= 12; i++)
    {
        catalogIdsList.Add(Guid.Parse($"b1000000-0000-0000-0000-0000000000{(6 + i).ToString("D2")}"));
    }
    var catalogIds = catalogIdsList.ToArray();

    {
        await context.Products
            .Where(p => !catalogIds.Contains(p.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false));

        var catalog = new List<Product>
        {
            new() {
                Id = catalogIds[0], Name = "Weintek MT8103iE HMI Panel",
                Description = "10.1 inç kapasitif dokunmatik ekran, Ethernet ve RS-485 destekli endüstriyel HMI.",
                Category = "HMI", ModelNumber = "MT8103iE",
                Specifications = """{"screen_size":"10.1 inç","resolution":"1024x600","touch":"Kapasitif"}""",
                BasePrice = 12500, Currency = "TRY", StockQuantity = 15,
                ImageUrl = "/images/product.png", Rating = 4.7m, SalesCount = 128, IsActive = true,
                CreatedAt = now, UpdatedAt = now
            },
            new() {
                Id = catalogIds[1], Name = "Siemens SIMATIC HMI TP700",
                Description = "7 inç TFT dokunmatik panel, TIA Portal uyumlu, çok dilli destek.",
                Category = "HMI", ModelNumber = "TP700 COMFORT",
                Specifications = """{"screen_size":"7 inç","resolution":"800x480","touch":"Dirençli"}""",
                BasePrice = 18900, Currency = "TRY", StockQuantity = 8,
                ImageUrl = "/images/image.png", Rating = 4.9m, SalesCount = 95, IsActive = true,
                CreatedAt = now, UpdatedAt = now
            },
            new() {
                Id = catalogIds[2], Name = "Samsung Indoor LED Panel IF015H",
                Description = "1.5mm piksel aralıklı, yüksek parlaklıklı iç mekan LED ekran paneli.",
                Category = "LED_PANEL", ModelNumber = "IF015H",
                Specifications = """{"pixel_pitch":"1.5mm","brightness":"1200 nit"}""",
                BasePrice = 45000, Currency = "TRY", StockQuantity = 10,
                ImageUrl = "/images/product.png", Rating = 4.6m, SalesCount = 72, IsActive = true,
                CreatedAt = now, UpdatedAt = now
            },
            new() {
                Id = catalogIds[3], Name = "Absen A2731 Outdoor LED",
                Description = "IP65 koruma sınıflı, dış mekan kullanıma uygun yüksek parlaklık LED panel.",
                Category = "LED_PANEL", ModelNumber = "A2731",
                Specifications = """{"pixel_pitch":"3.1mm","brightness":"5500 nit","protection":"IP65"}""",
                BasePrice = 62000, Currency = "TRY", StockQuantity = 6,
                ImageUrl = "/images/image.png", Rating = 4.8m, SalesCount = 54, IsActive = true,
                CreatedAt = now, UpdatedAt = now
            },
            new() {
                Id = catalogIds[4], Name = "NEC MultiSync ME552 55\" LCD",
                Description = "55 inç Full HD profesyonel LCD ekran, 24/7 kullanıma uygun.",
                Category = "LCD", ModelNumber = "ME552",
                Specifications = """{"size":"55 inç","resolution":"1920x1080","brightness":"500 nit"}""",
                BasePrice = 28000, Currency = "TRY", StockQuantity = 10,
                ImageUrl = "/images/product.png", Rating = 4.5m, SalesCount = 61, IsActive = true,
                CreatedAt = now, UpdatedAt = now
            },
            new() {
                Id = catalogIds[5], Name = "Philips BDL4330QL 43\" LCD",
                Description = "43 inç Full HD Android tabanlı, dahili medya oynatıcılı profesyonel ekran.",
                Category = "LCD", ModelNumber = "BDL4330QL",
                Specifications = """{"size":"43 inç","resolution":"1920x1080","os":"Android"}""",
                BasePrice = 18500, Currency = "TRY", StockQuantity = 18,
                ImageUrl = "/images/image.png", Rating = 4.4m, SalesCount = 88, IsActive = true,
                CreatedAt = now, UpdatedAt = now
            }
        };

        for (int i = 1; i <= 12; i++)
        {
            var category = i % 3 == 0 ? "HMI" : i % 3 == 1 ? "LED_PANEL" : "LCD";
            catalog.Add(new Product
            {
                Id = catalogIds[6 + i - 1],
                Name = $"Yeni Nesil {category} Serisi V{i}",
                Description = $"Bu ürün {category} kategorisinde öne çıkan yeni modeldir. Yüksek performans sunar.",
                Category = category,
                ModelNumber = $"NP-{category}-{i}",
                Specifications = "{\"baglanti\":\"Ethernet/Wi-Fi\",\"garanti\":\"3 Yıl\"}",
                BasePrice = 12000 + (i * 350),
                Currency = "TRY",
                StockQuantity = 10 + i,
                ImageUrl = i % 2 == 0 ? "/images/image.png" : "/images/product.png",
                Rating = 4.8m,
                SalesCount = i * 3,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        foreach (var p in catalog)
        {
            var existing = await context.Products.FindAsync(p.Id);
            if (existing is null)
                context.Products.Add(p);
            else
            {
                existing.Name = p.Name;
                existing.Description = p.Description;
                existing.Category = p.Category;
                existing.ModelNumber = p.ModelNumber;
                existing.Specifications = p.Specifications;
                existing.BasePrice = p.BasePrice;
                existing.StockQuantity = p.StockQuantity;
                existing.ImageUrl = p.ImageUrl;
                existing.Rating = p.Rating;
                existing.SalesCount = p.SalesCount;
                existing.IsActive = true;
                existing.UpdatedAt = now;
            }
        }
        await context.SaveChangesAsync();
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Quote Management API v1");
    c.RoutePrefix = "swagger";
});

// ─── Static Files (image uploads) ──────────────────────────
var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
Directory.CreateDirectory(Path.Combine(wwwrootPath, "uploads"));
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(wwwrootPath),
    RequestPath = ""
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
