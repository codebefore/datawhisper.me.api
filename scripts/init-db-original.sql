-- Create tables for DataWhisper MVP
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Users table
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Orders table
CREATE TABLE IF NOT EXISTS orders (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES users(id),
    amount DECIMAL(10,2) NOT NULL,
    status VARCHAR(50) NOT NULL,
    order_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Products table
CREATE TABLE IF NOT EXISTS products (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    price DECIMAL(10,2) NOT NULL,
    category VARCHAR(100),
    stock_quantity INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Sales/Revenue table (for analytics)
CREATE TABLE IF NOT EXISTS sales (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID REFERENCES orders(id),
    product_id UUID REFERENCES products(id),
    quantity INTEGER NOT NULL,
    unit_price DECIMAL(10,2) NOT NULL,
    total_price DECIMAL(10,2) GENERATED ALWAYS AS (quantity * unit_price) STORED,
    sale_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Seed data for MVP demo
INSERT INTO users (id, name, email) VALUES
    (uuid_generate_v4(), 'John Doe', 'john@example.com'),
    (uuid_generate_v4(), 'Jane Smith', 'jane@example.com'),
    (uuid_generate_v4(), 'Bob Johnson', 'bob@example.com'),
    (uuid_generate_v4(), 'Alice Brown', 'alice@example.com'),
    (uuid_generate_v4(), 'Charlie Wilson', 'charlie@example.com'),
    (uuid_generate_v4(), 'Diana Miller', 'diana@example.com'),
    (uuid_generate_v4(), 'Edward Davis', 'edward@example.com'),
    (uuid_generate_v4(), 'Fiona Garcia', 'fiona@example.com'),
    (uuid_generate_v4(), 'George Martinez', 'george@example.com'),
    (uuid_generate_v4(), 'Helen Rodriguez', 'helen@example.com');

INSERT INTO products (id, name, price, category, stock_quantity) VALUES
    (uuid_generate_v4(), 'Laptop', 999.99, 'Electronics', 50),
    (uuid_generate_v4(), 'Smartphone', 699.99, 'Electronics', 120),
    (uuid_generate_v4(), 'Headphones', 149.99, 'Electronics', 200),
    (uuid_generate_v4(), 'Coffee Maker', 89.99, 'Appliances', 75),
    (uuid_generate_v4(), 'Office Chair', 299.99, 'Furniture', 30),
    (uuid_generate_v4(), 'Desk Lamp', 49.99, 'Furniture', 100),
    (uuid_generate_v4(), 'Notebook', 12.99, 'Stationery', 500),
    (uuid_generate_v4(), 'Water Bottle', 24.99, 'Accessories', 300),
    (uuid_generate_v4(), 'Mouse Pad', 19.99, 'Accessories', 200),
    (uuid_generate_v4(), 'Keyboard', 79.99, 'Electronics', 150);

-- Insert some orders
INSERT INTO orders (id, user_id, amount, status, order_date) VALUES
    (uuid_generate_v4(), (SELECT id FROM users LIMIT 1), 1099.98, 'completed', CURRENT_TIMESTAMP - INTERVAL '5 days'),
    (uuid_generate_v4(), (SELECT id FROM users LIMIT 1 OFFSET 1), 749.98, 'pending', CURRENT_TIMESTAMP - INTERVAL '3 days'),
    (uuid_generate_v4(), (SELECT id FROM users LIMIT 1 OFFSET 2), 1299.97, 'completed', CURRENT_TIMESTAMP - INTERVAL '7 days'),
    (uuid_generate_v4(), (SELECT id FROM users LIMIT 1 OFFSET 3), 379.98, 'cancelled', CURRENT_TIMESTAMP - INTERVAL '2 days'),
    (uuid_generate_v4(), (SELECT id FROM users LIMIT 1 OFFSET 4), 1549.96, 'completed', CURRENT_TIMESTAMP - INTERVAL '10 days'),
    (uuid_generate_v4(), (SELECT id FROM users LIMIT 1 OFFSET 5), 229.98, 'pending', CURRENT_TIMESTAMP - INTERVAL '1 days'),
    (uuid_generate_v4(), (SELECT id FROM users LIMIT 1 OFFSET 6), 1799.96, 'completed', CURRENT_TIMESTAMP - INTERVAL '14 days'),
    (uuid_generate_v4(), (SELECT id FROM users LIMIT 1 OFFSET 7), 899.97, 'completed', CURRENT_TIMESTAMP - INTERVAL '6 days'),
    (uuid_generate_v4(), (SELECT id FROM users LIMIT 1 OFFSET 8), 594.98, 'pending', CURRENT_TIMESTAMP - INTERVAL '4 days'),
    (uuid_generate_v4(), (SELECT id FROM users LIMIT 1 OFFSET 9), 1099.97, 'completed', CURRENT_TIMESTAMP - INTERVAL '8 days');

-- Insert some sales data
INSERT INTO sales (order_id, product_id, quantity, unit_price)
SELECT
    o.id as order_id,
    p.id as product_id,
    random() * 5 + 1 as quantity,
    p.price as unit_price
FROM orders o, products p
WHERE o.status = 'completed'
ORDER BY random()
LIMIT 50;

-- Create some indexes for better performance
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_orders_user_id ON orders(user_id);
CREATE INDEX IF NOT EXISTS idx_orders_status ON orders(status);
CREATE INDEX IF NOT EXISTS idx_orders_date ON orders(order_date);
CREATE INDEX IF NOT EXISTS idx_products_category ON products(category);
CREATE INDEX IF NOT EXISTS idx_sales_date ON sales(sale_date);

-- Create view for summary data
CREATE OR REPLACE VIEW order_summary AS
SELECT
    DATE_TRUNC('day', order_date) as order_date,
    COUNT(*) as total_orders,
    SUM(amount) as total_revenue,
    COUNT(CASE WHEN status = 'completed' THEN 1 END) as completed_orders,
    COUNT(CASE WHEN status = 'pending' THEN 1 END) as pending_orders,
    COUNT(CASE WHEN status = 'cancelled' THEN 1 END) as cancelled_orders
FROM orders
GROUP BY DATE_TRUNC('day', order_date)
ORDER BY order_date DESC;

-- Update timestamp triggers
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_orders_updated_at BEFORE UPDATE ON orders
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_products_updated_at BEFORE UPDATE ON products
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();