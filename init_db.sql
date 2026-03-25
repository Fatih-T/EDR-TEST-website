CREATE DATABASE ECommerceDB;
GO
USE ECommerceDB;
GO

-- 1.xp_cmdshell'i aktif et (DB üzerinden komut çalıştırmak için kritik)
-- NOT: Bu işlem gerçek dünyada kapalıdır, test için aktif ediyoruz.
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
EXEC sp_configure 'xp_cmdshell', 1;
RECONFIGURE;
GO

CREATE TABLE Users (
    UserId INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL,
    Password NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100),
    Role NVARCHAR(20) DEFAULT 'User'
);

CREATE TABLE Products (
    ProductId INT PRIMARY KEY IDENTITY(1,1),
    ProductName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX),
    Price DECIMAL(18, 2),
    ImagePath NVARCHAR(255)
);

-- Örnek veriler
INSERT INTO Users (Username, Password, Email, Role) VALUES ('admin', 'P@ssw0rd123!', 'admin@example.com', 'Admin');
INSERT INTO Products (ProductName, Description, Price, ImagePath) VALUES ('Laptop', 'High performance laptop', 1200.00, 'laptop.jpg');
INSERT INTO Products (ProductName, Description, Price, ImagePath) VALUES ('Phone', 'Latest smartphone', 800.00, 'phone.jpg');
GO
