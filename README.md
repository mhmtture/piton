# 🖥️ QuoteManager Pro — Donanım Teklif Yönetim Sistemi

Endüstriyel donanım ürünleri (HMI, LED Panel, LCD) için tam kapsamlı, modern bir teklif yönetim platformu.

**GitHub Repository:** [https://github.com/mhmtture/Piton](https://github.com/mhmtture/Piton)

[![CI/CD Pipeline](https://github.com/mhmtture/Piton/actions/workflows/ci.yml/badge.svg)](https://github.com/mhmtture/Piton/actions/workflows/ci.yml)

---

## 📸 Ekran Görüntüleri

| Ana Sayfa | Ürün Listesi | Admin Paneli |
|---|---|---|
| Popüler ürünler, kategori kartları, istatistikler | Sol bar filtreleri, ürün grid | Teklif listesi, fiyat girişi, Excel import |

---

## 🏗️ Mimari

```
piton-2/
├── backend/                    # .NET 9 Web API (Clean Architecture)
│   ├── src/
│   │   ├── QuoteManagement.Domain/        # Entities, Enums
│   │   ├── QuoteManagement.Application/   # Interfaces, DTOs
│   │   ├── QuoteManagement.Infrastructure/# EF Core, Repositories, Services
│   │   └── QuoteManagement.Api/           # Controllers, Program.cs
│   └── Dockerfile
├── frontend/                   # Next.js 14 (App Router) + Tailwind CSS
│   ├── src/
│   │   ├── app/                # Sayfalar (/, /products, /cart, /admin)
│   │   ├── components/         # Navbar, Sidebar, ProductCard
│   │   └── lib/                # API client, Types, Helpers
│   └── Dockerfile
├── database/
│   └── init.sql                # PostgreSQL şema + seed data
├── .github/workflows/
│   └── ci-cd.yml               # GitHub Actions CI/CD
├── docker-compose.yml
└── README.md
```

---

## 🗃️ Veritabanı Yapısı (Code First & Seed Data)

Proje, **Entity Framework Core Code First** yaklaşımıyla geliştirilmiştir. Veritabanı tabloları ve test senaryolarına uygun, anlamlı başlangıç (seed) verileri uygulamanın ilk çalıştırılmasında otomatik olarak oluşturulur. Ayrıca Entity Framework Core `dotnet ef database update` ile manuel olarak da migrate edilebilir.

```sql
-- Müşteri/cari bilgileri
customers (id, name, email, phone, company, address)

-- Ürün kataloğu — son fiyat/tarih burada tutulur
products (id, name, description, category, model_number, specifications,
          base_price, currency, stock_quantity, is_active,
          last_request_price, last_request_date,  ← KRİTİK ALANLAR
          image_url)

-- Fiyat hareketi geçmişi
product_price_histories (id, product_id→products, price, currency, request_date, notes)

-- Teklif üst bilgileri
requests (id, request_no, customer_id→customers, request_date,
          total_amount, currency, status, notes, excel_file_path, sent_at)

-- Teklif satır kalemleri
request_items (id, request_id→requests, product_id→products,
               quantity, unit_price, discount_rate, line_total)
```

**Kategoriler:** `HMI` | `LED_PANEL` | `LCD`  
**Teklif Durumları:** `PENDING` | `PRICED` | `SENT` | `APPROVED` | `REJECTED`

---

## 🚀 Hızlı Başlangıç (Docker Compose)

### Ön Koşullar
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) kurulu olmalı

### Tüm Servisleri Başlat

```bash
git clone https://github.com/mhmtture/Piton.git
cd Piton

docker-compose up -d
```

Servisler otomatik olarak başlar:
| Servis | URL |
|---|---|
| 🌐 Frontend | http://localhost:3000 |
| ⚙️ Backend API | http://localhost:8080 |
| 📖 Swagger UI | http://localhost:8080/swagger |
| 🗄️ PostgreSQL | localhost:5432 |

> **Not:** `init.sql` dosyası ilk çalıştırmada otomatik olarak yüklenir (schema + seed data).

---

## 💻 Yerel Geliştirme

### Backend (.NET 9)

```bash
# PostgreSQL'i Docker ile başlat
docker run -d --name pg \
  -e POSTGRES_DB=quotemanagement \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  postgres:16-alpine

# Seed data Code First yaklaşımı ile dotnet run esnasında otomatik oluşturulacaktır.
# Eğer veritabanını manuel update etmek isterseniz:
# dotnet ef database update --project src/QuoteManagement.Api

# Backend'i çalıştır
cd backend
dotnet run --project src/QuoteManagement.Api
# → http://localhost:8080
```

### Frontend (Next.js)

```bash
cd frontend
npm install
# Backend URL'ini ayarla (opsiyonel, varsayılan: http://localhost:8080)
echo "NEXT_PUBLIC_API_URL=http://localhost:8080" > .env.local
npm run dev
# → http://localhost:3000
```

---

## 📋 API Referansı

| Method | Endpoint | Açıklama |
|---|---|---|
| `GET` | `/api/products` | Ürün listesi (query: `?category=HMI&search=...`) |
| `GET` | `/api/products/popular` | Popüler ürünler (query: `?count=6`) |
| `GET` | `/api/products/{id}` | Ürün detayı |
| `GET` | `/api/products/{id}/price-history` | Ürün fiyat geçmişi |
| `GET` | `/api/customers` | Müşteri listesi |
| `POST` | `/api/requests` | Yeni teklif + Excel döner |
| `GET` | `/api/requests` | Tüm teklifler |
| `GET` | `/api/requests/{id}` | Teklif detayı |
| `POST` | `/api/requests/import-excel` | Excel yükle (form-data: `file`) |
| `PUT` | `/api/requests/{id}/submit` | Fiyat ver ve teklif ilet |
| `GET` | `/health` | Health check |

Tam dokümantasyon: **http://localhost:8080/swagger**

---

## 🔄 Kullanıcı Akışı

```
Kullanıcı → Ürün seç → Sepete ekle → E-posta gir → "Teklif Al"
                                                         ↓
                                              Excel (teklif-formu.xlsx) indir
                                                         ↓
Admin → Excel yükle → Son fiyatları gör → Fiyatları düzenle → "Teklif İlet"
                                                         ↓
                              E-posta gönderilir (loglama simülasyonu)
                              products.last_request_price güncellenir
                              product_price_histories kayıt eklenir
```

---

## 🛠️ Teknoloji Yığını

| Katman | Teknoloji |
|---|---|
| **Frontend** | Next.js 14 (App Router), TypeScript, Tailwind CSS |
| **Backend** | .NET 9, ASP.NET Core Web API, Entity Framework Core 9 |
| **Veritabanı** | PostgreSQL 16 |
| **Excel** | ClosedXML (export), ExcelDataReader (import) |
| **E-posta** | Loglama simülasyonu (ILogger) |
| **Konteyner** | Docker, Docker Compose |
| **CI/CD** | GitHub Actions |

---

## 📦 Seed Data

Başlangıçta yüklenen veriler:
- **5 Müşteri** (farklı şirketler)
- **12 Ürün** — 4 HMI, 4 LED Panel, 4 LCD (gerçek model adları ve teknik özellikler)
- **4 Teklif** — PENDING, PRICED, SENT, APPROVED statüleri
- **7 Teklif kalemi**
- **10 Fiyat geçmişi kaydı**
