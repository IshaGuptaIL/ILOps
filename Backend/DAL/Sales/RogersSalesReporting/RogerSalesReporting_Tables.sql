-- Roger Sales Reporting Tables
-- These tables support the department column functionality

-- If SalesActivations table doesn't have department columns, we can add them:
-- ALTER TABLE SalesActivations ADD 
--     CoOpAdvertisingHO DECIMAL(18,2) DEFAULT 0,
--     MiscellaneousGBMNDSIncExp DECIMAL(18,2) DEFAULT 0,
--     OtherRevenueHO DECIMAL(18,2) DEFAULT 0,
--     OtherRevenueCO DECIMAL(18,2) DEFAULT 0,
--     ReceivableUpfrontEdgeRV DECIMAL(18,2) DEFAULT 0,
--     SalesAccessoriesCO DECIMAL(18,2) DEFAULT 0,
--     SalesHardwareCO DECIMAL(18,2) DEFAULT 0,
--     StagingAndDeployment DECIMAL(18,2) DEFAULT 0,
--     UnallocatedSales DECIMAL(18,2) DEFAULT 0,
--     WebHosting DECIMAL(18,2) DEFAULT 0;

-- Department mapping table (if needed for dynamic department allocation)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='tblSalesDepartments' AND xtype='U')
BEGIN
    CREATE TABLE tblSalesDepartments (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        DeptCode NVARCHAR(50) NOT NULL,
        DeptName NVARCHAR(255) NOT NULL,
        GLAccount NVARCHAR(50),
        IsActive BIT DEFAULT 1,
        CreatedBy NVARCHAR(255) DEFAULT 'System',
        CreatedDate DATETIME DEFAULT GETDATE(),
        ModifiedBy NVARCHAR(255) DEFAULT 'System',
        ModifiedDate DATETIME DEFAULT GETDATE()
    );

    -- Insert the 10 fixed departments
    INSERT INTO tblSalesDepartments (DeptCode, DeptName, GLAccount) VALUES
    ('COOP_ADV_HO', 'Co-Op Advertising - HO', '4100'),
    ('MISC_GBM_NDS', 'Miscellaneous GBM NDS Inc/Exp', '4200'),
    ('OTHER_REV_HO', 'Other Revenue - HO', '4300'),
    ('OTHER_REV_CO', 'Other Revenue - CO', '4310'),
    ('RECV_UPFRONT_RV', 'Receivable - Upfront Edge - RV', '1200'),
    ('SALES_ACC_CO', 'SALES - Accessories - CO', '4500'),
    ('SALES_HW_CO', 'SALES - Hardware - CO', '4510'),
    ('STAGING_DEPLOY', 'Staging and Deployment', '4600'),
    ('UNALLOC_SALES', 'Unallocated Sales', '4700'),
    ('WEB_HOSTING', 'Web Hosting', '4800');
END

-- Audit log table for tracking changes (as requested)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='tblRogerSalesAuditLog' AND xtype='U')
BEGIN
    CREATE TABLE tblRogerSalesAuditLog (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        TableName NVARCHAR(255),
        RecordId NVARCHAR(255),
        Action NVARCHAR(50), -- INSERT, UPDATE, DELETE
        OldValues NVARCHAR(MAX),
        NewValues NVARCHAR(MAX),
        CreatedBy NVARCHAR(255) DEFAULT 'System',
        CreatedDate DATETIME DEFAULT GETDATE(),
        ModifiedBy NVARCHAR(255) DEFAULT 'System',
        ModifiedDate DATETIME DEFAULT GETDATE()
    );
END

-- Function to validate territory (referenced in VBA logic)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ValidateTerritory' AND xtype='FN')
BEGIN
    EXEC('
    CREATE FUNCTION dbo.ValidateTerritory(@Territory NVARCHAR(50))
    RETURNS BIT
    AS
    BEGIN
        DECLARE @IsValid BIT = 0;
        
        -- Basic validation: not null, not empty, follows pattern
        IF @Territory IS NOT NULL 
           AND LTRIM(RTRIM(@Territory)) <> ''''
           AND LEN(@Territory) >= 2
           AND LEN(@Territory) <= 10
        BEGIN
            SET @IsValid = 1;
        END
        
        RETURN @IsValid;
    END
    ')
END

-- Sample determinetype function (referenced in VBA logic)
-- This would need to be implemented based on the actual business logic
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='determinetype' AND xtype='FN')
BEGIN
    EXEC('
    CREATE FUNCTION dbo.determinetype(
        @CAPHardware NVARCHAR(255),
        @VoicePlan NVARCHAR(255),
        @DataPlan NVARCHAR(255),
        @ProductCode NVARCHAR(255),
        @Description NVARCHAR(255),
        @RecordType NVARCHAR(255),
        @AdjustmentType NVARCHAR(255),
        @OrderNo NVARCHAR(255),
        @WebOrderID NVARCHAR(255),
        @CapCost DECIMAL(18,2),
        @CommissionVoice DECIMAL(18,2),
        @CommissionData DECIMAL(18,2),
        @RecordTypeExtended NVARCHAR(255)
    )
    RETURNS NVARCHAR(255)
    AS
    BEGIN
        DECLARE @Type NVARCHAR(255) = ''Unknown'';
        
        -- Simplified logic - would need actual business rules
        IF @VoicePlan IS NOT NULL AND @VoicePlan <> '''' AND @DataPlan IS NOT NULL AND @DataPlan <> ''''
            SET @Type = ''Voice and Data'';
        ELSE IF @VoicePlan IS NOT NULL AND @VoicePlan <> ''''
            SET @Type = ''Voice'';
        ELSE IF @DataPlan IS NOT NULL AND @DataPlan <> ''''
            SET @Type = ''Data'';
        ELSE IF @CAPHardware IS NOT NULL AND @CAPHardware <> ''''
            SET @Type = ''Hardware'';
        ELSE IF @WebOrderID LIKE ''H%''
            SET @Type = ''HUP'';
        ELSE
            SET @Type = ''Misc'';
            
        RETURN @Type;
    END
    ')
END