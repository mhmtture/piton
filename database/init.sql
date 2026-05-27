-- ============================================================
-- Teklif Yönetim Sistemi - PostgreSQL Schema + Seed Data
-- ============================================================

-- Extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ============================================================
-- USERS
-- ============================================================
CREATE TABLE IF NOT EXISTS users (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name            VARCHAR(200) NOT NULL,
    email           VARCHAR(200) NOT NULL UNIQUE,
    password_hash   TEXT NOT NULL,
    role            VARCHAR(50) NOT NULL DEFAULT 'USER',
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================
-- CUSTOMERS
-- ============================================================
CREATE TABLE IF NOT EXISTS customers (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name            VARCHAR(200) NOT NULL,
    email           VARCHAR(200) NOT NULL UNIQUE,
    phone           VARCHAR(50),
    company         VARCHAR(200),
    address         TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================
-- PRODUCTS
-- ============================================================
CREATE TABLE IF NOT EXISTS products (
    id                   UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name                 VARCHAR(300) NOT NULL,
    description          TEXT,
    category             VARCHAR(100) NOT NULL,        -- HMI / LED_PANEL / LCD
    model_number         VARCHAR(100),
    specifications       JSONB,                        -- { "resolution": "1920x1080", ... }
    base_price           NUMERIC(18,2) NOT NULL DEFAULT 0,
    currency             VARCHAR(10) NOT NULL DEFAULT 'TRY',
    stock_quantity       INT NOT NULL DEFAULT 0,
    is_active            BOOLEAN NOT NULL DEFAULT TRUE,
    last_request_price   NUMERIC(18,2),               -- Son teklif edilen fiyat
    last_request_date    TIMESTAMPTZ,                  -- Son teklif tarihi
    image_url            VARCHAR(500),
    rating               NUMERIC(3,2) NOT NULL DEFAULT 4.0,
    sales_count          INT NOT NULL DEFAULT 0,
    created_at           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at           TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================
-- PRODUCT PRICE HISTORIES
-- ============================================================
CREATE TABLE IF NOT EXISTS product_price_histories (
    id             UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    product_id     UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    price          NUMERIC(18,2) NOT NULL,
    currency       VARCHAR(10) NOT NULL DEFAULT 'TRY',
    request_date   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    notes          TEXT,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================
-- REQUESTS (Teklifler)
-- ============================================================
CREATE TABLE IF NOT EXISTS requests (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    request_no      VARCHAR(50) NOT NULL UNIQUE,        -- TEK-2024-001
    customer_id     UUID NOT NULL REFERENCES customers(id),
    request_date    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    total_amount    NUMERIC(18,2) NOT NULL DEFAULT 0,
    currency        VARCHAR(10) NOT NULL DEFAULT 'TRY',
    status          VARCHAR(50) NOT NULL DEFAULT 'PENDING',
                    -- PENDING | PRICED | SENT | APPROVED | REJECTED
    notes           TEXT,
    excel_file_path VARCHAR(500),
    sent_at         TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================
-- REQUEST ITEMS (Teklif Satırları)
-- ============================================================
CREATE TABLE IF NOT EXISTS request_items (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    request_id      UUID NOT NULL REFERENCES requests(id) ON DELETE CASCADE,
    product_id      UUID NOT NULL REFERENCES products(id),
    quantity        INT NOT NULL DEFAULT 1,
    unit_price      NUMERIC(18,2),                      -- Admin tarafından girilir
    discount_rate   NUMERIC(5,2) NOT NULL DEFAULT 0,    -- Yüzde olarak: 10.00 = %10
    line_total      NUMERIC(18,2),                      -- (unit_price * quantity) * (1 - discount_rate/100)
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================
-- INDEXES
-- ============================================================
CREATE INDEX IF NOT EXISTS idx_products_category ON products(category);
CREATE INDEX IF NOT EXISTS idx_products_is_active ON products(is_active);
CREATE INDEX IF NOT EXISTS idx_product_price_histories_product_id ON product_price_histories(product_id);
CREATE INDEX IF NOT EXISTS idx_product_price_histories_request_date ON product_price_histories(request_date DESC);
CREATE INDEX IF NOT EXISTS idx_requests_customer_id ON requests(customer_id);
CREATE INDEX IF NOT EXISTS idx_requests_status ON requests(status);
CREATE INDEX IF NOT EXISTS idx_requests_request_date ON requests(request_date DESC);
CREATE INDEX IF NOT EXISTS idx_request_items_request_id ON request_items(request_id);
CREATE INDEX IF NOT EXISTS idx_request_items_product_id ON request_items(product_id);

-- ============================================================
-- SEED DATA - CUSTOMERS
-- ============================================================
INSERT INTO customers (id, name, email, phone, company, address) VALUES
    ('a1000000-0000-0000-0000-000000000001', 'Ahmet Yılmaz',     'ahmet.yilmaz@teknosoft.com.tr', '+90 212 555 0101', 'TeknoSoft A.Ş.',         'Maslak, İstanbul'),
    ('a1000000-0000-0000-0000-000000000002', 'Zeynep Kaya',      'zeynep.kaya@endustek.com.tr',   '+90 312 555 0202', 'Endüstri Teknoloji Ltd.', 'Ostim, Ankara'),
    ('a1000000-0000-0000-0000-000000000003', 'Murat Demir',      'murat.demir@otomasyonx.com',    '+90 232 555 0303', 'OtomasyonX San. Tic.',    'Konak, İzmir'),
    ('a1000000-0000-0000-0000-000000000004', 'Elif Şahin',       'elif.sahin@promatic.com.tr',    '+90 342 555 0404', 'ProMatic Sistemler',       'Şahinbey, Gaziantep'),
    ('a1000000-0000-0000-0000-000000000005', 'Burak Çelik',      'burak.celik@digitalmak.com',    '+90 224 555 0505', 'DigitalMak Mühendislik',   'Nilüfer, Bursa')
ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- SEED DATA - PRODUCTS
-- ============================================================
INSERT INTO products (id, name, description, category, model_number, specifications, base_price, currency, stock_quantity, image_url, rating, sales_count) VALUES
    ('b1000000-0000-0000-0000-000000000001',
     'Weintek MT8103iE HMI Panel',
     '10.1 inç kapasitif dokunmatik ekran, Ethernet ve RS-485 destekli endüstriyel HMI paneli.',
     'HMI', 'MT8103iE',
     '{"screen_size":"10.1 inç","resolution":"1024x600","touch":"Kapasitif"}',
     12500.00, 'TRY', 15, '/images/product.png', 4.70, 128),

    ('b1000000-0000-0000-0000-000000000002',
     'Siemens SIMATIC HMI TP700',
     '7 inç TFT dokunmatik panel, TIA Portal uyumlu, çok dilli destek.',
     'HMI', 'TP700 COMFORT',
     '{"screen_size":"7 inç","resolution":"800x480","touch":"Dirençli"}',
     18900.00, 'TRY', 8, '/images/image.png', 4.90, 95),

    ('b1000000-0000-0000-0000-000000000003',
     'Samsung Indoor LED Panel IF015H',
     '1.5mm piksel aralıklı, yüksek parlaklıklı iç mekan LED ekran paneli.',
     'LED_PANEL', 'IF015H',
     '{"pixel_pitch":"1.5mm","brightness":"1200 nit"}',
     45000.00, 'TRY', 10, '/images/product.png', 4.60, 72),

    ('b1000000-0000-0000-0000-000000000004',
     'Absen A2731 Outdoor LED',
     'IP65 koruma sınıflı, dış mekan kullanıma uygun yüksek parlaklık LED panel.',
     'LED_PANEL', 'A2731',
     '{"pixel_pitch":"3.1mm","brightness":"5500 nit","protection":"IP65"}',
     62000.00, 'TRY', 6, '/images/image.png', 4.80, 54),

    ('b1000000-0000-0000-0000-000000000005',
     'NEC MultiSync ME552 55" LCD',
     '55 inç Full HD profesyonel LCD ekran, 24/7 kullanıma uygun, 500 nit parlaklık.',
     'LCD', 'ME552',
     '{"size":"55 inç","resolution":"1920x1080","brightness":"500 nit"}',
     28000.00, 'TRY', 10, '/images/product.png', 4.50, 61),

    ('b1000000-0000-0000-0000-000000000006',
     'Philips BDL4330QL 43" LCD',
     '43 inç Full HD Android tabanlı, dahili medya oynatıcılı profesyonel ekran.',
     'LCD', 'BDL4330QL',
     '{"size":"43 inç","resolution":"1920x1080","os":"Android"}',
     18500.00, 'TRY', 18, '/images/image.png', 4.40, 88)
ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- SEED DATA - REQUESTS
-- ============================================================
INSERT INTO requests (id, request_no, customer_id, request_date, total_amount, currency, status, notes, sent_at) VALUES
    ('c1000000-0000-0000-0000-000000000001',
     'TEK-2024-001',
     'a1000000-0000-0000-0000-000000000001',
     '2024-11-15 10:30:00+03',
     88400.00, 'TRY', 'SENT',
     'TeknoSoft ilk teklif talebi - acil',
     '2024-11-16 14:00:00+03'),

    ('c1000000-0000-0000-0000-000000000002',
     'TEK-2024-002',
     'a1000000-0000-0000-0000-000000000002',
     '2024-12-01 09:00:00+03',
     156000.00, 'TRY', 'APPROVED',
     'Endüstri Teknoloji - fabrika genişletme projesi',
     '2024-12-03 11:00:00+03'),

    ('c1000000-0000-0000-0000-000000000003',
     'TEK-2025-001',
     'a1000000-0000-0000-0000-000000000003',
     '2025-01-10 14:20:00+03',
     0, 'TRY', 'PENDING',
     'OtomasyonX teklif bekleniyor',
     NULL),

    ('c1000000-0000-0000-0000-000000000004',
     'TEK-2025-002',
     'a1000000-0000-0000-0000-000000000004',
     '2025-02-05 11:15:00+03',
     62000.00, 'TRY', 'PRICED',
     'ProMatic fiyat verildi, onay bekleniyor',
     NULL)
ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- SEED DATA - REQUEST ITEMS
-- ============================================================
INSERT INTO request_items (id, request_id, product_id, quantity, unit_price, discount_rate, line_total) VALUES
    -- TEK-2024-001
    ('d1000000-0000-0000-0000-000000000001',
     'c1000000-0000-0000-0000-000000000001',
     'b1000000-0000-0000-0000-000000000001', 2, 12000.00, 5.00, 22800.00),
    ('d1000000-0000-0000-0000-000000000002',
     'c1000000-0000-0000-0000-000000000001',
     'b1000000-0000-0000-0000-000000000005', 2, 27000.00, 3.00, 52380.00),

    -- TEK-2024-002
    ('d1000000-0000-0000-0000-000000000003',
     'c1000000-0000-0000-0000-000000000002',
     'b1000000-0000-0000-0000-000000000003', 2, 43000.00, 5.00, 81700.00),
    ('d1000000-0000-0000-0000-000000000004',
     'c1000000-0000-0000-0000-000000000002',
     'b1000000-0000-0000-0000-000000000006', 3, 24000.00, 2.50, 70200.00),

    -- TEK-2025-001 (pending, no prices yet)
    ('d1000000-0000-0000-0000-000000000005',
     'c1000000-0000-0000-0000-000000000003',
     'b1000000-0000-0000-0000-000000000003', 5, NULL, 0, NULL),
    ('d1000000-0000-0000-0000-000000000006',
     'c1000000-0000-0000-0000-000000000003',
     'b1000000-0000-0000-0000-000000000006', 3, NULL, 0, NULL),

    -- TEK-2025-002
    ('d1000000-0000-0000-0000-000000000007',
     'c1000000-0000-0000-0000-000000000004',
     'b1000000-0000-0000-0000-000000000004', 1, 62000.00, 0, 62000.00)
ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- SEED DATA - PRODUCT PRICE HISTORIES
-- ============================================================
INSERT INTO product_price_histories (id, product_id, price, currency, request_date, notes) VALUES
    ('e1000000-0000-0000-0000-000000000001',
     'b1000000-0000-0000-0000-000000000001', 11500.00, 'TRY',
     '2024-09-10 10:00:00+03', 'İlk teklif - TeknoSoft öncesi'),
    ('e1000000-0000-0000-0000-000000000002',
     'b1000000-0000-0000-0000-000000000001', 12000.00, 'TRY',
     '2024-11-16 14:00:00+03', 'TEK-2024-001 teklifi'),

    ('e1000000-0000-0000-0000-000000000003',
     'b1000000-0000-0000-0000-000000000005', 25000.00, 'TRY',
     '2024-08-05 09:00:00+03', 'Pilot proje teklifi'),
    ('e1000000-0000-0000-0000-000000000004',
     'b1000000-0000-0000-0000-000000000005', 27000.00, 'TRY',
     '2024-11-16 14:00:00+03', 'TEK-2024-001 teklifi'),

    ('e1000000-0000-0000-0000-000000000005',
     'b1000000-0000-0000-0000-000000000003', 40000.00, 'TRY',
     '2024-10-20 11:00:00+03', 'Fuar fiyatı'),
    ('e1000000-0000-0000-0000-000000000006',
     'b1000000-0000-0000-0000-000000000003', 43000.00, 'TRY',
     '2024-12-03 11:00:00+03', 'TEK-2024-002 teklifi'),

    ('e1000000-0000-0000-0000-000000000007',
     'b1000000-0000-0000-0000-000000000006', 22000.00, 'TRY',
     '2024-11-01 08:00:00+03', 'Depo stok fiyatı'),
    ('e1000000-0000-0000-0000-000000000008',
     'b1000000-0000-0000-0000-000000000006', 24000.00, 'TRY',
     '2024-12-03 11:00:00+03', 'TEK-2024-002 teklifi'),

    ('e1000000-0000-0000-0000-000000000009',
     'b1000000-0000-0000-0000-000000000004', 58000.00, 'TRY',
     '2024-06-15 14:00:00+03', 'İlk outdoor teklifi'),
    ('e1000000-0000-0000-0000-000000000010',
     'b1000000-0000-0000-0000-000000000004', 62000.00, 'TRY',
     '2025-02-05 11:15:00+03', 'TEK-2025-002 teklifi')
ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- UPDATE products.last_request_price / last_request_date
-- ============================================================
UPDATE products p
SET
    last_request_price = h.price,
    last_request_date  = h.request_date
FROM (
    SELECT DISTINCT ON (product_id)
           product_id,
           price,
           request_date
    FROM   product_price_histories
    ORDER  BY product_id, request_date DESC
) h
WHERE p.id = h.product_id;
