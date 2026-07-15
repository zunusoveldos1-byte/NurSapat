-- Supabase SQL: create cloud tables that match the local SQLite offline cache.
-- Run in Supabase SQL Editor. Adjust RLS policies for your auth model.

create extension if not exists "pgcrypto";

create table if not exists public.materials (
  id uuid primary key,
  article text not null default '',
  name text not null default '',
  note text not null default '',
  manufacturer text not null default '',
  section integer not null default 0,
  main_unit integer not null default 0,
  linear_group integer null,
  square_group integer null,
  currency integer not null default 0,
  base_price numeric not null default 0,
  waste_percent numeric not null default 0,
  print_on_assembly_drawing boolean not null default false,
  print_on_commercial_offer boolean not null default false,
  print_on_shipping_sheet boolean not null default false,
  group_print_positions boolean not null default false,
  is_warm_profile boolean not null default false,
  inner_coating_area_per_meter numeric not null default 0,
  outer_coating_area_per_meter numeric not null default 0,
  enable_length_cut_optimization boolean not null default false,
  bar_length_mm numeric not null default 0,
  illiquid_threshold_mm numeric not null default 0,
  scrap_mm numeric not null default 0,
  enable_plane_cut_optimization boolean not null default false,
  sheet_width_mm numeric not null default 0,
  sheet_height_mm numeric not null default 0,
  glass_tint numeric not null default 0,
  minimum_calculated_area_sq_m numeric not null default 0,
  use_secondary_unit boolean not null default false,
  secondary_unit_coefficient numeric not null default 0,
  price_by_kilogram boolean not null default false,
  prices_json jsonb not null default '[]'::jsonb,
  user_id uuid null references auth.users(id),
  is_deleted boolean not null default false,
  updated_at timestamptz not null default now()
);

create table if not exists public.orders (
  id uuid primary key,
  profitability_percent numeric not null default 0,
  order_number text not null default '',
  order_date timestamptz not null default now(),
  status integer not null default 0,
  customer_id uuid null,
  dealer_id uuid null,
  current_price_mode integer not null default 0,
  total_discount_percent numeric not null default 0,
  total_discount_sum numeric not null default 0,
  customer_name text not null default '',
  customer_phone text not null default '',
  site_address text not null default '',
  dealer_name text not null default '',
  order_manager_name text not null default '',
  customer_city text not null default '',
  customer_code text not null default '',
  contract_number text not null default '',
  contract_date timestamptz null,
  production_date timestamptz null,
  delivery_date timestamptz null,
  installation_date timestamptz null,
  has_delivery boolean not null default false,
  has_installation boolean not null default false,
  profile_note text not null default '',
  production_note text not null default '',
  note text not null default '',
  currency_code text not null default 'KGS',
  constructions_subtotal numeric not null default 0,
  discount2_percent numeric not null default 0,
  delivery_cost numeric not null default 0,
  additional_materials_cost numeric not null default 0,
  additional_materials_installation_cost numeric not null default 0,
  vat_percent numeric not null default 0,
  vat_mode integer not null default 0,
  agreed_amount numeric not null default 0,
  paid_amount numeric not null default 0,
  payment_currency_code text not null default 'KGS',
  selected_construction_id uuid null,
  export_file_path text null,
  constructions_json jsonb not null default '[]'::jsonb,
  user_id uuid null references auth.users(id),
  is_deleted boolean not null default false,
  updated_at timestamptz not null default now()
);

alter table public.materials enable row level security;
alter table public.orders enable row level security;

create policy "materials_select_own" on public.materials
  for select using (auth.uid() = user_id or user_id is null);

create policy "materials_upsert_own" on public.materials
  for all using (auth.uid() = user_id) with check (auth.uid() = user_id);

create policy "orders_select_own" on public.orders
  for select using (auth.uid() = user_id or user_id is null);

create policy "orders_upsert_own" on public.orders
  for all using (auth.uid() = user_id) with check (auth.uid() = user_id);

-- Функция, которая обновляет поле updated_at текущим временем
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Навешиваем триггер на таблицу materials
CREATE TRIGGER update_materials_updated_at
    BEFORE UPDATE ON public.materials
    FOR EACH ROW
    EXECUTE PROCEDURE update_updated_at_column();

-- Навешиваем триггер на таблицу orders
CREATE TRIGGER update_orders_updated_at
    BEFORE UPDATE ON public.orders
    FOR EACH ROW
    EXECUTE PROCEDURE update_updated_at_column();
