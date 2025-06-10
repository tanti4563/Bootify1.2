-- Migration to add ReturnVoyageId and ReturnDate fields to BookingOrders table
-- Run this script on your SQL Server database

USE [FerryBooking]
GO

-- Check if columns already exist before adding them
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BookingOrders]') AND name = 'ReturnVoyageId')
BEGIN
    ALTER TABLE [dbo].[BookingOrders]
    ADD [ReturnVoyageId] int NULL
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BookingOrders]') AND name = 'ReturnDate')
BEGIN
    ALTER TABLE [dbo].[BookingOrders]
    ADD [ReturnDate] datetime2(7) NULL
END

-- Check if StatusId column exists in TicketOrders table, if not add it
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TicketOrders]') AND name = 'StatusId')
BEGIN
    ALTER TABLE [dbo].[TicketOrders]
    ADD [StatusId] int NOT NULL DEFAULT 0
END

PRINT 'Migration completed successfully'
