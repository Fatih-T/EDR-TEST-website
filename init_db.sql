CREATE DATABASE ECommerceDB;
GO
USE ECommerceDB;
GO

-- 1. xp_cmdshell'i aktif et (Siber güvenlik testleri için kritik)
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
EXEC sp_configure 'xp_cmdshell', 1;
RECONFIGURE;
GO

-- 2. Tabloların Oluşturulması
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

CREATE TABLE Comments (
    CommentId INT PRIMARY KEY IDENTITY(1,1),
    ProductId INT FOREIGN KEY REFERENCES Products(ProductId),
    UserNickname NVARCHAR(50),
    CommentText NVARCHAR(MAX),
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- 3. Örnek Verilerin Eklenmesi
INSERT INTO Users (Username, Password, Email, Role) VALUES ('admin', 'P@ssw0rd123!', 'admin@example.com', 'Admin');
INSERT INTO Users (Username, Password, Email, Role) VALUES ('user1', 'user123', 'user1@example.com', 'User');

INSERT INTO Products (ProductName, Description, Price, ImagePath) VALUES ('Laptop', 'High performance gaming laptop', 1500.00, 'laptop.jpg');
INSERT INTO Products (ProductName, Description, Price, ImagePath) VALUES ('Phone', 'Latest generation smartphone', 900.00, 'phone.jpg');
INSERT INTO Products (ProductName, Description, Price, ImagePath) VALUES ('Headphones', 'Noise cancelling bluetooth headphones', 250.00, 'headphones.jpg');

INSERT INTO Comments (ProductId, UserNickname, CommentText) VALUES (1, 'Hacker', '<script>alert("Stored XSS Test!");</script> Ürün harika!');
INSERT INTO Comments (ProductId, UserNickname, CommentText) VALUES (1, 'User123', 'Güzel bir bilgisayar, tavsiye ederim.');
GO
