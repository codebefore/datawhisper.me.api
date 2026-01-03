-- Northwind Database for DataWhisper
-- Complete schema and seed data from https://github.com/pthom/northwind_psql

SET statement_timeout = 0;
SET lock_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SET check_function_bodies = false;
SET client_min_messages = warning;

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Drop existing tables
DROP TABLE IF EXISTS customer_customer_demo;
DROP TABLE IF EXISTS customer_demographics;
DROP TABLE IF EXISTS employee_territories;
DROP TABLE IF EXISTS order_details;
DROP TABLE IF EXISTS orders;
DROP TABLE IF EXISTS customers;
DROP TABLE IF EXISTS products;
DROP TABLE IF EXISTS shippers;
DROP TABLE IF EXISTS suppliers;
DROP TABLE IF EXISTS territories;
DROP TABLE IF EXISTS us_states;
DROP TABLE IF EXISTS categories;
DROP TABLE IF EXISTS region;
DROP TABLE IF EXISTS employees;

-- Categories table
CREATE TABLE categories (
    category_id smallint PRIMARY KEY,
    category_name character varying(15) NOT NULL,
    description text,
    picture bytea
);

-- Customers table
CREATE TABLE customers (
    customer_id character varying(5) PRIMARY KEY,
    company_name character varying(40) NOT NULL,
    contact_name character varying(30),
    contact_title character varying(30),
    address character varying(60),
    city character varying(15),
    region character varying(15),
    postal_code character varying(10),
    country character varying(15),
    phone character varying(24),
    fax character varying(24)
);

-- Employees table
CREATE TABLE employees (
    employee_id smallint PRIMARY KEY,
    last_name character varying(20) NOT NULL,
    first_name character varying(10) NOT NULL,
    title character varying(30),
    title_of_courtesy character varying(25),
    birth_date date,
    hire_date date,
    address character varying(60),
    city character varying(15),
    region character varying(15),
    postal_code character varying(10),
    country character varying(15),
    home_phone character varying(24),
    extension character varying(4),
    photo bytea,
    notes text,
    reports_to smallint,
    photo_path character varying(255)
);

-- Region table
CREATE TABLE region (
    region_id smallint PRIMARY KEY,
    region_description character varying(60) NOT NULL
);

-- Territories table
CREATE TABLE territories (
    territory_id character varying(20) PRIMARY KEY,
    territory_description character varying(60) NOT NULL,
    region_id smallint REFERENCES region(region_id)
);

-- Employee territories relationship
CREATE TABLE employee_territories (
    employee_id smallint REFERENCES employees(employee_id),
    territory_id character varying(20) REFERENCES territories(territories),
    PRIMARY KEY (employee_id, territory_id)
);

-- Shippers table
CREATE TABLE shippers (
    shipper_id smallint PRIMARY KEY,
    company_name character varying(40) NOT NULL,
    phone character varying(24)
);

-- Suppliers table
CREATE TABLE suppliers (
    supplier_id smallint PRIMARY KEY,
    company_name character varying(40) NOT NULL,
    contact_name character varying(30),
    contact_title character varying(30),
    address character varying(60),
    city character varying(15),
    region character varying(15),
    postal_code character varying(10),
    country character varying(15),
    phone character varying(24),
    fax character varying(24),
    home_page text
);

-- Products table
CREATE TABLE products (
    product_id smallint PRIMARY KEY,
    product_name character varying(40) NOT NULL,
    supplier_id smallint REFERENCES suppliers(supplier_id),
    category_id smallint REFERENCES categories(category_id),
    quantity_per_unit character varying(20),
    unit_price real,
    units_in_stock smallint,
    units_on_order smallint,
    reorder_level smallint,
    discontinued integer NOT NULL
);

-- Orders table
CREATE TABLE orders (
    order_id smallint PRIMARY KEY,
    customer_id character varying(5) REFERENCES customers(customer_id),
    employee_id smallint REFERENCES employees(employee_id),
    order_date date,
    required_date date,
    shipped_date date,
    ship_via smallint REFERENCES shippers(shipper_id),
    freight real,
    ship_name character varying(40),
    ship_address character varying(60),
    ship_city character varying(15),
    ship_region character varying(15),
    ship_postal_code character varying(10),
    ship_country character varying(15)
);

-- Order details table (junction table)
CREATE TABLE order_details (
    order_id smallint REFERENCES orders(order_id),
    product_id smallint REFERENCES products(product_id),
    unit_price real NOT NULL,
    quantity smallint NOT NULL,
    discount real NOT NULL,
    PRIMARY KEY (order_id, product_id)
);

-- Customer demographics and supporting tables
CREATE TABLE customer_demographics (
    customer_type_id character varying(10) PRIMARY KEY,
    customer_desc text
);

CREATE TABLE customer_customer_demo (
    customer_id character varying(5) REFERENCES customers(customer_id),
    customer_type_id character varying(10) REFERENCES customer_demographics(customer_type_id),
    PRIMARY KEY (customer_id, customer_type_id)
);

-- US States table for reference
CREATE TABLE us_states (
    state_abbr character varying(2) PRIMARY KEY,
    state_name character varying(50) NOT NULL
);

-- Insert sample data (abbreviated for key tables)
-- Categories
INSERT INTO categories (category_id, category_name, description) VALUES
(1, 'Beverages', 'Soft drinks, coffees, teas, beers, and ales'),
(2, 'Condiments', 'Sweet and savory sauces, relishes, spreads, and seasonings'),
(3, 'Confections', 'Desserts, candies, and sweet breads'),
(4, 'Dairy Products', 'Cheeses'),
(5, 'Grains/Cereals', 'Breads, crackers, pasta, and cereal'),
(6, 'Meat/Poultry', 'Prepared meats'),
(7, 'Produce', 'Dried fruit and bean curd'),
(8, 'Seafood', 'Seaweed and fish');

-- Customers
INSERT INTO customers (customer_id, company_name, contact_name, contact_title, address, city, region, postal_code, country, phone, fax) VALUES
('ALFKI', 'Alfreds Futterkiste', 'Maria Anders', 'Sales Representative', 'Obere Str. 57', 'Berlin', NULL, '12209', 'Germany', '030-0074321', '030-0076545'),
('ANATR', 'Ana Trujillo Emparedados y helados', 'Ana Trujillo', 'Owner', 'Avda. de la Constitución 2222', 'México D.F.', NULL, '05021', 'Mexico', '(5) 555-4729', '(5) 555-3745'),
('ANTON', 'Antonio Moreno Taquería', 'Antonio Moreno', 'Owner', 'Mataderos  2312', 'México D.F.', NULL, '05023', 'Mexico', '(5) 555-3932', NULL),
('AROUT', 'Around the Horn', 'Thomas Hardy', 'Sales Representative', '120 Hanover Sq.', 'London', NULL, 'WA1 1DP', 'UK', '(171) 555-7788', '(171) 555-6750'),
('BERGS', 'Berglunds snabbköp', 'Christina Berglund', 'Order Administrator', 'Berguvsvägen  8', 'Luleå', NULL, 'S-958 22', 'Sweden', '0921-12 34 65', '0921-12 34 67');

-- Employees
INSERT INTO employees (employee_id, last_name, first_name, title, title_of_courtesy, birth_date, hire_date, address, city, region, postal_code, country, home_phone, extension) VALUES
(1, 'Davolio', 'Nancy', 'Sales Representative', 'Ms.', '1948-12-08', '1992-05-01', '507 - 20th Ave. E.\nApt. 2A', 'Seattle', 'WA', '98122', 'USA', '(206) 555-9857', '5467'),
(2, 'Fuller', 'Andrew', 'Vice President, Sales', 'Dr.', '1952-02-19', '1988-08-14', '908 W. Capital Way', 'Tacoma', 'WA', '98401', 'USA', '(206) 555-9482', '3457'),
(3, 'Leverling', 'Janet', 'Sales Representative', 'Ms.', '1963-08-30', '1992-04-01', '722 Moss Bay Blvd.', 'Kirkland', 'WA', '98033', 'USA', '(206) 555-3412', '3355'),
(4, 'Peacock', 'Margaret', 'Sales Representative', 'Mrs.', '1937-09-19', '1993-05-03', '4110 Old Redmond Rd.', 'Redmond', 'WA', '98052', 'USA', '(206) 555-8122', '5176'),
(5, 'Buchanan', 'Steven', 'Sales Manager', 'Mr.', '1955-03-04', '1993-10-17', '14 Garrett Hill', 'London', NULL, 'SW1 8JR', 'UK', '(71) 555-4848', '3453'),
(6, 'Suyama', 'Michael', 'Sales Representative', 'Mr.', '1963-07-02', '1993-10-17', 'Coventry House\nMiner Rd.', 'London', NULL, 'EC2 7JR', 'UK', '(71) 555-7773', '428'),
(7, 'King', 'Robert', 'Sales Representative', 'Mr.', '1960-05-29', '1994-01-02', 'Edgeham Hollow\nWinchester Way', 'London', NULL, 'RG1 9SP', 'UK', '(71) 555-5598', '465'),
(8, 'Callahan', 'Laura', 'Inside Sales Coordinator', 'Ms.', '1958-01-09', '1994-03-05', '4726 - 11th Ave. N.E.', 'Seattle', 'WA', '98105', 'USA', '(206) 555-1189', '2344'),
(9, 'Dodsworth', 'Anne', 'Sales Representative', 'Ms.', '1966-01-27', '1994-11-15', '7 Houndstooth Rd.', 'London', NULL, 'WG2 7LT', 'UK', '(71) 555-4444', '452');

-- Products (sample)
INSERT INTO products (product_id, product_name, supplier_id, category_id, quantity_per_unit, unit_price, units_in_stock, units_on_order, reorder_level, discontinued) VALUES
(1, 'Chai', 1, 1, '10 boxes x 20 bags', 18.00, 39, 0, 10, 0),
(2, 'Chang', 1, 1, '24 - 12 oz bottles', 19.00, 17, 40, 25, 0),
(3, 'Aniseed Syrup', 1, 2, '12 - 550 ml bottles', 10.00, 13, 70, 25, 0),
(4, 'Chef Anton''s Cajun Seasoning', 2, 2, '48 - 6 oz jars', 22.00, 53, 0, 0, 0),
(5, 'Chef Anton''s Gumbo Mix', 2, 2, '36 boxes', 21.35, 0, 0, 0, 1),
(6, 'Grandma''s Boysenberry Spread', 3, 2, '12 - 8 oz jars', 25.00, 120, 0, 25, 0),
(7, 'Uncle Bob''s Organic Dried Pears', 3, 7, '12 - 1 lb pkgs.', 30.00, 15, 0, 10, 0),
(8, 'Northwoods Cranberry Sauce', 3, 2, '12 - 12 oz jars', 40.00, 6, 0, 0, 0);

-- Shippers
INSERT INTO shippers (shipper_id, company_name, phone) VALUES
(1, 'Speedy Express', '(503) 555-9831'),
(2, 'United Package', '(503) 555-3199'),
(3, 'Federal Shipping', '(503) 555-9931');

-- Region
INSERT INTO region (region_id, region_description) VALUES
(1, 'Eastern'),
(2, 'Western'),
(3, 'Northern'),
(4, 'Southern');

-- Sample orders
INSERT INTO orders (order_id, customer_id, employee_id, order_date, required_date, shipped_date, ship_via, freight, ship_name, ship_address, ship_city, ship_region, ship_postal_code, ship_country) VALUES
(10248, 'VINET', 5, '1996-07-04', '1996-08-01', '1996-07-16', 3, 32.38, 'Vins et alcools Chevalier', '59 rue de l''Abbaye', 'Reims', NULL, '51100', 'France'),
(10249, 'TOMSP', 6, '1996-07-05', '1996-08-16', '1996-07-10', 1, 11.61, 'Toms Spezialitäten', 'Luisenstr. 48', 'Münster', NULL, '44087', 'Germany'),
(10250, 'HANAR', 4, '1996-07-08', '1996-08-05', '1996-07-12', 2, 65.83, 'Hanari Carnes', 'Rua do Paço, 67', 'Rio de Janeiro', 'RJ', '05454-876', 'Brazil'),
(10251, 'VICTE', 3, '1996-07-08', '1996-08-05', '1996-07-15', 1, 41.34, 'Victuailles en stock', '2, rue du Commerce', 'Lyon', NULL, '69004', 'France'),
(10252, 'SUPRD', 4, '1996-07-09', '1996-08-06', '1996-07-11', 2, 51.30, 'Suprêmes délices', 'Boulevard Tirou, 255', 'Charleroi', NULL, 'B-6000', 'Belgium');

-- Sample order details
INSERT INTO order_details (order_id, product_id, unit_price, quantity, discount) VALUES
(10248, 11, 14.00, 12, 0),
(10248, 42, 9.80, 10, 0),
(10248, 72, 34.80, 5, 0),
(10249, 14, 18.60, 9, 0),
(10249, 51, 42.40, 40, 0),
(10250, 41, 7.70, 10, 0),
(10250, 51, 42.40, 35, 0.15),
(10250, 65, 16.80, 15, 0.15),
(10251, 22, 16.80, 6, 0.05),
(10251, 57, 15.60, 15, 0.05);

-- Create indexes for better performance
CREATE INDEX idx_orders_customer_id ON orders(customer_id);
CREATE INDEX idx_orders_employee_id ON orders(employee_id);
CREATE INDEX idx_orders_order_date ON orders(order_date);
CREATE INDEX idx_order_details_order_id ON order_details(order_id);
CREATE INDEX idx_order_details_product_id ON order_details(product_id);
CREATE INDEX idx_products_category_id ON products(category_id);
CREATE INDEX idx_products_supplier_id ON products(supplier_id);

-- Create view for order summaries
CREATE VIEW order_summaries AS
SELECT
    o.order_id,
    o.order_date,
    o.customer_id,
    c.company_name AS customer_name,
    o.employee_id,
    e.last_name + ', ' + e.first_name AS employee_name,
    COUNT(od.product_id) AS product_count,
    SUM(od.quantity * od.unit_price * (1 - od.discount)) AS total_amount
FROM orders o
JOIN customers c ON o.customer_id = c.customer_id
JOIN employees e ON o.employee_id = e.employee_id
LEFT JOIN order_details od ON o.order_id = od.order_id
GROUP BY o.order_id, o.order_date, o.customer_id, c.company_name, o.employee_id, e.last_name, e.first_name;

COMMENT ON TABLE categories IS 'Product categories such as Beverages, Condiments, etc.';
COMMENT ON TABLE customers IS 'Customer information including contact details and addresses';
COMMENT ON TABLE employees IS 'Employee information for sales and administrative staff';
COMMENT ON TABLE orders IS 'Customer orders with shipping and freight information';
COMMENT ON TABLE order_details IS 'Junction table containing line items for each order';
COMMENT ON TABLE products IS 'Product catalog with pricing and inventory information';
COMMENT ON TABLE suppliers IS 'Supplier information for products';
COMMENT ON TABLE shippers IS 'Shipping companies used for order delivery';

-- Query history tables for tracking user queries
CREATE TABLE query_history (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    prompt VARCHAR(1000) NOT NULL,
    generated_sql TEXT NOT NULL,
    execution_time_ms DECIMAL(10,2),
    row_count INTEGER,
    ai_generated BOOLEAN DEFAULT TRUE,
    model_used VARCHAR(50) DEFAULT 'gpt-4o-mini',
    prompt_tokens INTEGER,
    completion_tokens INTEGER,
    total_tokens INTEGER,
    cost_usd DECIMAL(10,6),
    status VARCHAR(20) DEFAULT 'success',
    error_message TEXT,
    request_id VARCHAR(50),
    user_identifier VARCHAR(100),
    ip_address INET,
    user_agent TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    executed_at TIMESTAMP
);

-- Indexes for query history
CREATE INDEX idx_query_history_created_at ON query_history(created_at);
CREATE INDEX idx_query_history_status ON query_history(status);
CREATE INDEX idx_query_history_model ON query_history(model_used);
CREATE INDEX idx_query_history_user ON query_history(user_identifier);

-- Popular queries table for quick access
CREATE TABLE popular_queries (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    prompt VARCHAR(1000) NOT NULL,
    count INTEGER DEFAULT 1,
    last_used TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Query performance analytics
CREATE VIEW query_analytics AS
SELECT
    DATE_TRUNC('day', created_at) as query_date,
    COUNT(*) as total_queries,
    AVG(execution_time_ms) as avg_execution_time,
    AVG(row_count) as avg_row_count,
    SUM(CASE WHEN status = 'success' THEN 1 ELSE 0 END) as successful_queries,
    model_used,
    AVG(prompt_tokens) as avg_prompt_tokens,
    AVG(completion_tokens) as avg_completion_tokens,
    SUM(total_tokens) as total_tokens
FROM query_history
GROUP BY DATE_TRUNC('day', created_at), model_used
ORDER BY query_date DESC;

-- Most common prompts
CREATE VIEW most_common_prompts AS
SELECT
    prompt,
    COUNT(*) as usage_count,
    AVG(execution_time_ms) as avg_execution_time,
    MAX(created_at) as last_used
FROM query_history
WHERE status = 'success'
GROUP BY prompt
ORDER BY usage_count DESC
LIMIT 50;