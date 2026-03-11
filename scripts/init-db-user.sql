-- =============================================================================
-- init-db-user.sql
-- Creates a dedicated SQL Server user for the ShopApp application.
-- Run AFTER the database is created (e.g., after migrations).
-- Usage:  sqlcmd -S localhost -U sa -P <sa_password> -i scripts/init-db-user.sql
-- =============================================================================

USE [master];
GO

-- Create a login at the server level
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'shopapp_user')
BEGIN
    CREATE LOGIN [shopapp_user] WITH PASSWORD = N'$(SHOPAPP_DB_PASSWORD)', DEFAULT_DATABASE = [ShopApp];
END
GO

USE [ShopApp];
GO

-- Create a user mapped to the login
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'shopapp_user')
BEGIN
    CREATE USER [shopapp_user] FOR LOGIN [shopapp_user];
END
GO

-- Grant minimum required permissions
ALTER ROLE [db_datareader] ADD MEMBER [shopapp_user];
ALTER ROLE [db_datawriter] ADD MEMBER [shopapp_user];
ALTER ROLE [db_ddladmin]   ADD MEMBER [shopapp_user];   -- needed for EF migrations
GO

PRINT 'User shopapp_user created successfully with db_datareader, db_datawriter, db_ddladmin roles.';
GO
