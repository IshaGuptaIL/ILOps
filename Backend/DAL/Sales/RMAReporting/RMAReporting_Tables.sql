-- =====================================================================================
-- Database Schema Script for RMA Reporting Spire
-- Tables: dbo.tblRMA, dbo.tblRMA_Responses, dbo.tblRogersReportCMRMA, dbo.tblRogersReportCM, dbo.tblRogersReportRMA, dbo.tblRMAUsers
-- All tables include CreatedBy, CreatedDate, ModifiedBy, ModifiedDate audit fields.
-- =====================================================================================

-- 1. Table: dbo.tblRMA
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tblRMA]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tblRMA] (
        [ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [SKU] NVARCHAR(100) NULL,
        [IMEI] NVARCHAR(50) NULL,
        [ReturnReasonCode] NVARCHAR(50) NULL,
        [ExtraInfo] NVARCHAR(255) NULL,
        [OutputCSV] BIT DEFAULT 0,
        [OutputCSVDate] DATETIME NULL,
        [OutputCSVBatch] NVARCHAR(100) NULL,
        [ValidationResults] NVARCHAR(255) NULL,
        [RogersResponse] NVARCHAR(100) NULL,
        [InvoiceSold] NVARCHAR(50) NULL,
        [InvoiceSoldDate] DATETIME NULL,
        [WhseSold] NVARCHAR(50) NULL,
        [BVCreditOrder] NVARCHAR(50) NULL,
        [ReturnedRogers] NVARCHAR(100) NULL,
        [ReturnedRogersBVOrder] NVARCHAR(50) NULL,
        [Swap] NVARCHAR(50) NULL,
        [SwapCMO] NVARCHAR(50) NULL,
        [Pristine] BIT DEFAULT 0,
        [RejectedACT] BIT DEFAULT 0,
        [Closed] BIT DEFAULT 0,
        [FinalDisposition] NVARCHAR(255) NULL,
        [ReturnWaybill] NVARCHAR(100) NULL,
        [LogInDate] DATETIME NULL,
        [CreditAmtClaimed] DECIMAL(18, 2) NULL,
        [User] NVARCHAR(100) NULL,
        [Status] NVARCHAR(100) NULL,
        
        -- Audit fields
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
        [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [ModifiedBy] NVARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
        [ModifiedDate] DATETIME NOT NULL DEFAULT GETDATE()
    );

    CREATE NONCLUSTERED INDEX [IX_tblRMA_IMEI] ON [dbo].[tblRMA] ([IMEI]);
    CREATE NONCLUSTERED INDEX [IX_tblRMA_ExtraInfo] ON [dbo].[tblRMA] ([ExtraInfo]);
    CREATE NONCLUSTERED INDEX [IX_tblRMA_ReturnWaybill] ON [dbo].[tblRMA] ([ReturnWaybill]);
END
GO

-- 2. Table: dbo.tblRMA_Responses (mapped to tblRMA-Responses in Access)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tblRMA_Responses]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tblRMA_Responses] (
        [ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [IMEI] NVARCHAR(50) NULL,
        [RogersResponse] NVARCHAR(100) NULL,
        [RMANumber] NVARCHAR(50) NULL,
        [RMADate] DATETIME NULL,
        [HeaderReturnReason] NVARCHAR(100) NULL,
        [FileName] NVARCHAR(255) NULL,
        [ITEM] NVARCHAR(100) NULL,
        [Qty] INT DEFAULT 1,
        [DateReceived] DATETIME NULL,
        [DateIssued] DATETIME NULL,
        [VPFLastMoveDate] DATETIME NULL,
        [VPFAssignDate] DATETIME NULL,
        [ReturnReason] NVARCHAR(255) NULL,
        [CreditAmount] DECIMAL(18, 2) NULL,
        [RestockFee] DECIMAL(18, 2) NULL,
        [TotalCredit] DECIMAL(18, 2) NULL,
        [Status] NVARCHAR(100) NULL,
        [LastStatusMessage] NVARCHAR(255) NULL,
        [RMAUpdated] BIT DEFAULT 0,
        [RejectReason] NVARCHAR(255) NULL,
        [RejectReasonComment] NVARCHAR(500) NULL,
        
        -- Audit fields
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
        [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [ModifiedBy] NVARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
        [ModifiedDate] DATETIME NOT NULL DEFAULT GETDATE()
    );

    CREATE NONCLUSTERED INDEX [IX_tblRMA_Responses_IMEI] ON [dbo].[tblRMA_Responses] ([IMEI]);
    CREATE NONCLUSTERED INDEX [IX_tblRMA_Responses_RMANumber] ON [dbo].[tblRMA_Responses] ([RMANumber]);
END
GO

-- 3. Table: dbo.tblRogersReportCMRMA
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tblRogersReportCMRMA]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tblRogersReportCMRMA] (
        [ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [CMNumber] NVARCHAR(50) NULL,
        [CMDate] DATETIME NULL,
        [CMAmount] DECIMAL(18, 2) NULL,
        [RMA] NVARCHAR(50) NULL,
        [SKU] NVARCHAR(100) NULL,
        [Qty] INT DEFAULT 1,
        [UnitPrice] DECIMAL(18, 2) NULL,
        [RMAmount] DECIMAL(18, 2) NULL,
        [RMAmountTotal] DECIMAL(18, 2) NULL,
        [IMEIRMA] NVARCHAR(50) NULL,
        [CMImportFile] NVARCHAR(255) NULL,
        [RMImportFile] NVARCHAR(255) NULL,
        
        -- Audit fields
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
        [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [ModifiedBy] NVARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
        [ModifiedDate] DATETIME NOT NULL DEFAULT GETDATE()
    );

    CREATE NONCLUSTERED INDEX [IX_tblRogersReportCMRMA_IMEIRMA] ON [dbo].[tblRogersReportCMRMA] ([IMEIRMA]);
    CREATE NONCLUSTERED INDEX [IX_tblRogersReportCMRMA_CMNumber] ON [dbo].[tblRogersReportCMRMA] ([CMNumber]);
    CREATE NONCLUSTERED INDEX [IX_tblRogersReportCMRMA_RMA] ON [dbo].[tblRogersReportCMRMA] ([RMA]);
END
GO

-- 4. Table: dbo.tblRogersReportCM
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tblRogersReportCM]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tblRogersReportCM] (
        [ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Class] NVARCHAR(100) NULL,
        [Source] NVARCHAR(100) NULL,
        [Type] NVARCHAR(100) NULL,
        [OperatingUnit] NVARCHAR(100) NULL,
        [LegalEntityName] NVARCHAR(100) NULL,
        [Number] NVARCHAR(100) NULL,
        [BillToCustomer] NVARCHAR(255) NULL,
        [Complete] NVARCHAR(50) NULL,
        [BalanceDue] DECIMAL(18, 2) NULL,
        [Currency] NVARCHAR(10) NULL,
        [Date] DATETIME NULL,
        [GLDate] DATETIME NULL,
        [Salesperson] NVARCHAR(100) NULL,
        [Terms] NVARCHAR(100) NULL,
        [DiscoverComment] NVARCHAR(255) NULL,
        [ImportFileName] NVARCHAR(255) NULL,
        [DateImported] DATETIME NULL,
        
        -- Audit fields
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
        [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [ModifiedBy] NVARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
        [ModifiedDate] DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE NONCLUSTERED INDEX [IX_tblRogersReportCM_ImportFileName] ON [dbo].[tblRogersReportCM] ([ImportFileName]);
    CREATE NONCLUSTERED INDEX [IX_tblRogersReportCM_ClassType] ON [dbo].[tblRogersReportCM] ([Class], [Type], [Source]);
END
GO

-- 5. Table: dbo.tblRogersReportRMA
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tblRogersReportRMA]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tblRogersReportRMA] (
        [ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [RMANumber] NVARCHAR(50) NULL,
        [RMADate] DATETIME NULL,
        [ITEM] NVARCHAR(100) NULL,
        [Qty] INT DEFAULT 1,
        [FileName] NVARCHAR(255) NULL,
        [ImportFileName] NVARCHAR(255) NULL,
        
        -- Audit fields
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
        [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [ModifiedBy] NVARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
        [ModifiedDate] DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE NONCLUSTERED INDEX [IX_tblRogersReportRMA_ImportFileName] ON [dbo].[tblRogersReportRMA] ([ImportFileName]);
END
GO

-- 6. Table: dbo.tblRMAUsers
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tblRMAUsers]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tblRMAUsers] (
        [ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserName] NVARCHAR(100) NOT NULL,
        [UserInitials] NVARCHAR(10) NOT NULL,
        [UserRole] NVARCHAR(50) NULL DEFAULT 'User',
        [IsActive] BIT NOT NULL DEFAULT 1,
        
        -- Audit fields
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
        [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [ModifiedBy] NVARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
        [ModifiedDate] DATETIME NOT NULL DEFAULT GETDATE()
    );

    INSERT INTO [dbo].[tblRMAUsers] ([UserName], [UserInitials], [UserRole], [IsActive], [CreatedBy], [CreatedDate], [ModifiedBy], [ModifiedDate])
    VALUES 
    ('Administrator', 'ADM', 'Admin', 1, 'SYSTEM', GETDATE(), 'SYSTEM', GETDATE()),
    ('RMA Analyst', 'RMA', 'User', 1, 'SYSTEM', GETDATE(), 'SYSTEM', GETDATE());
END
GO
