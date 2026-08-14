-- Hand-written reference schema for SQL Server.
--
-- The EF Core path normally never needs this file: `dotnet ef migrations add InitialCreate`
-- (see EntityFramework/AppDbContext.cs for the exact command) generates the equivalent migration
-- straight from the Fluent API configuration in EntityFramework/Configurations. This script exists
-- because the Dapper repositories talk to raw tables with no migrations system of their own - and
-- because both ORMs in this project must agree on one identical schema for the comparison between
-- them to mean anything. If you change a Configurations/*.cs file, mirror the change here too.

CREATE TABLE Customers
(
    Id            UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Name          NVARCHAR(200)    NOT NULL,
    Email         NVARCHAR(320)    NOT NULL,
    CreatedAtUtc  DATETIME2        NOT NULL,
    UpdatedAtUtc  DATETIME2        NULL,
    CONSTRAINT UQ_Customers_Email UNIQUE (Email)
);

CREATE TABLE Products
(
    Id            UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Sku           NVARCHAR(50)     NOT NULL,
    Name          NVARCHAR(200)    NOT NULL,
    UnitPrice     DECIMAL(18, 2)   NOT NULL,
    StockQuantity INT              NOT NULL,
    CreatedAtUtc  DATETIME2        NOT NULL,
    UpdatedAtUtc  DATETIME2        NULL,
    CONSTRAINT UQ_Products_Sku UNIQUE (Sku)
);

CREATE TABLE Orders
(
    Id           UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    CustomerId   UNIQUEIDENTIFIER NOT NULL,
    Status       NVARCHAR(20)     NOT NULL,
    RowVersion   ROWVERSION, -- SQL Server generates and increments this column itself; never written to explicitly
    CreatedAtUtc DATETIME2        NOT NULL,
    UpdatedAtUtc DATETIME2        NULL,
    CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) REFERENCES Customers (Id)
);
CREATE INDEX IX_Orders_CustomerId ON Orders (CustomerId);

CREATE TABLE OrderItems
(
    Id        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    OrderId   UNIQUEIDENTIFIER NOT NULL,
    ProductId UNIQUEIDENTIFIER NOT NULL,
    Quantity  INT              NOT NULL,
    UnitPrice DECIMAL(18, 2)   NOT NULL,
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES Orders (Id) ON DELETE CASCADE,
    CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId) REFERENCES Products (Id)
);
