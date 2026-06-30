CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'material_category') THEN
        CREATE TYPE material_category AS ENUM ('profile', 'glazing', 'hardware', 'sealant', 'reinforcement', 'component', 'installation', 'additional');
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'order_status') THEN
        CREATE TYPE order_status AS ENUM ('КП', 'Договор', 'Оплата', 'Раскрой', 'Сборка', 'Доставка', 'Закрыт');
    END IF;
END $$;

-- ИСПРАВЛЕНИЕ: Таблица materials теперь с id (UUID) и color_id
CREATE TABLE IF NOT EXISTS materials (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    article VARCHAR(64) UNIQUE NOT NULL,
    name VARCHAR(200) NOT NULL,
    category material_category NOT NULL,
    price NUMERIC(18, 4) NOT NULL CHECK (price >= 0),
    currency CHAR(3) NOT NULL DEFAULT 'KGS',
    unit_of_measure VARCHAR(32) NOT NULL,
    waste_percent NUMERIC(7, 4) NOT NULL DEFAULT 0 CHECK (waste_percent >= 0),
    color_id UUID
);

-- ... (остальные таблицы clients, orders, system_logs, hardware_constraints остаются без изменений) ...
CREATE TABLE IF NOT EXISTS clients (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(200) NOT NULL,
    phone VARCHAR(32),
    address TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS orders (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_number VARCHAR(64) NOT NULL UNIQUE,
    client_id UUID NOT NULL REFERENCES clients(id) ON UPDATE CASCADE ON DELETE RESTRICT,
    contract_number VARCHAR(64),
    contract_date DATE,
    status order_status NOT NULL DEFAULT 'КП',
    lock_changes BOOLEAN NOT NULL DEFAULT FALSE,
    wind_load NUMERIC(10, 3) NOT NULL DEFAULT 0,
    exchange_rate NUMERIC(18, 6) NOT NULL DEFAULT 1 CHECK (exchange_rate > 0),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS order_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID NOT NULL REFERENCES orders(id) ON UPDATE CASCADE ON DELETE CASCADE,
    name VARCHAR(200) NOT NULL,
    width_mm INT NOT NULL CHECK (width_mm > 0),
    height_mm INT NOT NULL CHECK (height_mm > 0),
    quantity INT NOT NULL CHECK (quantity > 0),
    drawing_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    schema_version INT NOT NULL DEFAULT 1 CHECK (schema_version > 0),
    calculated_cost NUMERIC(18, 4) NOT NULL DEFAULT 0 CHECK (calculated_cost >= 0),
    calculated_price NUMERIC(18, 4) NOT NULL DEFAULT 0 CHECK (calculated_price >= 0)
);

CREATE TABLE IF NOT EXISTS system_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID REFERENCES orders(id) ON UPDATE CASCADE ON DELETE SET NULL,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    operator_name VARCHAR(150) NOT NULL,
    operation_type VARCHAR(80) NOT NULL,
    action_details TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS hardware_constraints (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    hardware_brand VARCHAR(120) NOT NULL,
    min_width_mm INT NOT NULL CHECK (min_width_mm > 0),
    max_width_mm INT NOT NULL CHECK (max_width_mm >= min_width_mm),
    min_height_mm INT NOT NULL CHECK (min_height_mm > 0),
    max_height_mm INT NOT NULL CHECK (max_height_mm >= min_height_mm),
    max_weight_kg NUMERIC(10, 3) NOT NULL CHECK (max_weight_kg > 0)
);

CREATE INDEX IF NOT EXISTS ix_orders_client_id ON orders(client_id);
CREATE INDEX IF NOT EXISTS ix_order_items_order_id ON order_items(order_id);
CREATE INDEX IF NOT EXISTS ix_system_logs_order_id_timestamp ON system_logs(order_id, timestamp DESC);
CREATE INDEX IF NOT EXISTS ix_hardware_constraints_brand ON hardware_constraints(hardware_brand);

-- ИСПРАВЛЕНИЕ: Вставка тестовых данных с id и color_id
INSERT INTO materials (
    id,
    article,
    name,
    category,
    price,
    currency,
    unit_of_measure,
    waste_percent,
    color_id
)
VALUES (
    gen_random_uuid(),
    'DEMO-PROFILE-001',
    'Демонстрационный профиль',
    'profile',
    1250.0000,
    'KGS',
    'm',
    3.5000,
    NULL
)
ON CONFLICT (article) DO UPDATE
SET price = EXCLUDED.price,
    currency = EXCLUDED.currency,
    name = EXCLUDED.name,
    category = EXCLUDED.category,
    unit_of_measure = EXCLUDED.unit_of_measure,
    waste_percent = EXCLUDED.waste_percent,
    color_id = EXCLUDED.color_id;