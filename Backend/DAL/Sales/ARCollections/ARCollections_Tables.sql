-- SQL Script for ARCollections Module Tables

-- 1. Create Lookup Tables
IF OBJECT_ID('tblEventTypes', 'U') IS NULL
BEGIN
    CREATE TABLE tblEventTypes (
        EventType INT PRIMARY KEY,
        EventDescription NVARCHAR(255) NOT NULL,
        HasTrans BIT NOT NULL DEFAULT 1,
        CreatedBy INT,
        CreatedDate DATETIME DEFAULT GETDATE(),
        ModifiedBy INT,
        ModifiedDate DATETIME
    );

    -- Insert Default Event Types
    INSERT INTO tblEventTypes (EventType, EventDescription, HasTrans, CreatedBy) VALUES
    (1, 'Comment', 1, 1),
    (2, 'First Notice', 1, 1),
    (3, 'Second Notice', 1, 1),
    (4, 'Invoice Sent', 1, 1),
    (5, 'Call Out', 0, 1),
    (6, 'Call In', 0, 1),
    (7, 'Email', 0, 1),
    (8, 'Fax', 0, 1),
    (9, 'BareComment', 0, 1),
    (10, 'Summary Comment', 0, 1);
END;

IF OBJECT_ID('tblRootCauses', 'U') IS NULL
BEGIN
    CREATE TABLE tblRootCauses (
        Code INT PRIMARY KEY,
        Description NVARCHAR(255) NOT NULL,
        CreatedBy INT,
        CreatedDate DATETIME DEFAULT GETDATE(),
        ModifiedBy INT,
        ModifiedDate DATETIME
    );

    -- Insert Default Root Causes
    INSERT INTO tblRootCauses (Code, Description, CreatedBy) VALUES
    (1, 'Slow Paying Customer', 1),
    (2, 'Customer / Rep. does not respond/ refuses to pay', 1),
    (3, 'Customer Requesting revision of Invoice (add P.O., etc)', 1),
    (4, 'Customer pays Rogers Directly', 1),
    (5, 'Bankruptcy Protection', 1),
    (6, 'Write-off underway', 1),
    (7, 'Settled/Paid', 1),
    (8, 'Customer Dispute- MSF pricing', 1),
    (9, 'Customer Dispute- H/W pricing (Wrong cost)', 1),
    (10, 'Customer Dispute- H/W already changed in V21', 1),
    (11, 'Shipping Dispute', 1),
    (12, 'Bankrupt Customer', 1),
    (13, 'Escalation to Rogers 90 & 120 Days Accounts', 1);
END;

IF OBJECT_ID('tblTerritoryGroups', 'U') IS NULL
BEGIN
    CREATE TABLE tblTerritoryGroups (
        ID INT PRIMARY KEY,
        GroupName NVARCHAR(255) NOT NULL,
        GroupCriteria NVARCHAR(MAX),
        SortOrder INT,
        Phone1 NVARCHAR(50),
        Phone2 NVARCHAR(50),
        RogersReporting BIT NOT NULL DEFAULT 0,
        RogersReportingName NVARCHAR(100),
        CreatedBy INT,
        CreatedDate DATETIME DEFAULT GETDATE(),
        ModifiedBy INT,
        ModifiedDate DATETIME
    );

    -- Insert Default Territory Groups
    INSERT INTO tblTerritoryGroups (ID, GroupName, GroupCriteria, SortOrder, Phone1, Phone2, RogersReporting, RogersReportingName, CreatedBy) VALUES
    (1, 'ENT Corporate', '(((SALES_TERR) = ''CCO'')) or (((SALES_TERR)=''CBM'')) or (((SALES_TERR)=''CFV''))', 1, '1-866-595-1075', '905-459-0760', 1, 'ENT-CORP', 1),
    (2, 'Commercial', '(((SALES_TERR)=''RDL'')) or (((SALES_TERR)=''RDC'')) or (((SALES_TERR)=''BCC'')) or (((SALES_TERR)=''BSS''))', 5, '1-866-595-1176', '905-459-0760', 1, 'RDL/RDC/BCC', 1),
    (3, 'Discover Misc', '(((SALES_TERR) like ''H*''))', 6, '1-866-728-6423', '905-459-0760', 0, '', 1),
    (5, 'Hardware Return', '(((SALES_TERR)=''RRT''))', 7, '1-866-728-6423', '905-459-0760', 0, '', 1),
    (6, 'DCI GetConnected', '(((SALES_TERR) like ''D*''))', 8, '1-866-728-6423', '905-459-0760', 0, '', 1),
    (7, 'ENT EPP', '(((SALES_TERR)=''EPP''))', 9, '1-866-728-6423', '905-459-0760', 0, '', 1),
    (8, 'Rogers Courier Repair', '(((SALES_TERR)=''RCR''))', 10, '1-866-728-6423', '905-459-0760', 0, '', 1),
    (9, 'RIL', '(((SALES_TERR)=''RIL''))', 11, '1-866-728-6423', '905-459-0760', 0, '', 1),
    (10, 'Other', '', 12, '1-866-728-6423', '905-459-0760', 0, '', 1),
    (11, 'ENT ON GOVT', '(((SALES_TERR) = ''CYG'') or ((SALES_TERR)=''CHY''))', 2, '1-866-459-7158', '905-459-0760', 1, 'ENT-ONT(MGS)', 1),
    (12, 'ENT GOC', '(((SALES_TERR)=''CGC'') or ((SALES_TERR)=''CHR''))', 3, '1-866-459-7158', '905-459-0760', 1, 'ENT-GOVT', 1),
    (13, 'ENT CSPQ', '(((SALES_TERR) = ''CLT'') or ((SALES_TERR)=''CYP''))', 4, '1-866-246-3986 X6296', '905-459-0760', 1, 'ENT-CSPQ', 1);
END;

IF OBJECT_ID('tblAllowedAccounts', 'U') IS NULL
BEGIN
    CREATE TABLE tblAllowedAccounts (
        ID INT PRIMARY KEY,
        Account NVARCHAR(100) NOT NULL UNIQUE,
        CreatedBy INT,
        CreatedDate DATETIME DEFAULT GETDATE(),
        ModifiedBy INT,
        ModifiedDate DATETIME
    );

    -- Insert Default Allowed Accounts
    INSERT INTO tblAllowedAccounts (ID, Account, CreatedBy) VALUES
    (1, '11101', 1),
    (2, '11102', 1),
    (3, '11103', 1),
    (4, '11131', 1),
    (5, '11140', 1),
    (6, '11150', 1),
    (7, '11160', 1),
    (8, '11170', 1),
    (9, '11180', 1),
    (10, '11190', 1),
    (11, '11200', 1),
    (12, '11210', 1),
    (13, '11346', 1),
    (14, '40185', 1),
    (15, '40186', 1);
END;

-- 2. Create Event and Transaction Tables
IF OBJECT_ID('tblEvents', 'U') IS NULL
BEGIN
    CREATE TABLE tblEvents (
        ID INT IDENTITY(1,1) PRIMARY KEY,
        EventType INT NOT NULL,
        CustNo NVARCHAR(100),
        CustType NVARCHAR(50),
        EventText NVARCHAR(MAX),
        EventAmount DECIMAL(18,2),
        CommentKey NVARCHAR(100),
        AddDate DATETIME,
        AddUser NVARCHAR(50),
        ModDate DATETIME,
        ModUser NVARCHAR(50),
        CreatedBy INT,
        CreatedDate DATETIME DEFAULT GETDATE(),
        ModifiedBy INT,
        ModifiedDate DATETIME
    );
END;

IF OBJECT_ID('tblEventTrans', 'U') IS NULL
BEGIN
    CREATE TABLE tblEventTrans (
        ID INT IDENTITY(1,1) PRIMARY KEY,
        EventID INT NOT NULL,
        TransNo NVARCHAR(100) NOT NULL,
        CreatedBy INT,
        CreatedDate DATETIME DEFAULT GETDATE(),
        ModifiedBy INT,
        ModifiedDate DATETIME
    );
END;

IF OBJECT_ID('tblARDetailExtra', 'U') IS NULL
BEGIN
    CREATE TABLE tblARDetailExtra (
        ID INT IDENTITY(1,1) PRIMARY KEY,
        TransNo NVARCHAR(100) NOT NULL UNIQUE,
        BAN NVARCHAR(100),
        FirstNoticeDate DATETIME,
        FirstNoticeBalance DECIMAL(18,2),
        SecondNoticeDate DATETIME,
        SecondNoticeBalance DECIMAL(18,2),
        RootCauseID INT,
        NextID INT,
        OPCResolved BIT NOT NULL DEFAULT 0,
        OPCDescription NVARCHAR(255),
        BulkID NVARCHAR(100),
        BulkIDChecked BIT NOT NULL DEFAULT 0,
        IgnoreGroup BIT NOT NULL DEFAULT 0,
        BillToCust NVARCHAR(100),
        CreatedBy INT,
        CreatedDate DATETIME DEFAULT GETDATE(),
        ModifiedBy INT,
        ModifiedDate DATETIME
    );
END;

IF OBJECT_ID('tblBulkCustomers', 'U') IS NULL
BEGIN
    CREATE TABLE tblBulkCustomers (
        ID INT IDENTITY(1,1) PRIMARY KEY,
        CustNo NVARCHAR(100) NOT NULL UNIQUE,
        CreatedBy INT,
        CreatedDate DATETIME DEFAULT GETDATE(),
        ModifiedBy INT,
        ModifiedDate DATETIME
    );
END;

IF OBJECT_ID('tblCustomerGroupsRR', 'U') IS NULL
BEGIN
    CREATE TABLE tblCustomerGroupsRR (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CustGroup NVARCHAR(100) NOT NULL,
        GroupName NVARCHAR(255),
        BVCustNo NVARCHAR(100) NOT NULL,
        BVName NVARCHAR(255),
        CreatedBy INT,
        CreatedDate DATETIME DEFAULT GETDATE(),
        ModifiedBy INT,
        ModifiedDate DATETIME
    );
END;

-- 3. Create Session Cache Tables (with UserId)
IF OBJECT_ID('tblCustomersOpen', 'U') IS NULL
BEGIN
    CREATE TABLE tblCustomersOpen (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CUST NVARCHAR(20) NOT NULL,
        CustName NVARCHAR(255),
        CustGroup NVARCHAR(255),
        GroupAndSingle BIT NOT NULL DEFAULT 0,
        SALES_TERR NVARCHAR(255),
        PostalCode NVARCHAR(25),
        BVADDRTELNO1 NVARCHAR(255),
        BVADDREMAIL NVARCHAR(255),
        BVCOCONTACT1NAME NVARCHAR(255),
        BVCOCONTACT1TEL1 NVARCHAR(255),
        BVCOCONTACT1EMAIL NVARCHAR(255),
        BVCOCONTACT2NAME NVARCHAR(255),
        BVCOCONTACT2TEL1 NVARCHAR(255),
        BVCOCONTACT2EMAIL NVARCHAR(255),
        BVCOCONTACT3NAME NVARCHAR(255),
        BVCOCONTACT3TEL1 NVARCHAR(255),
        BVCOCONTACT3EMAIL NVARCHAR(255),
        Language NVARCHAR(20),
        ChannelID INT,
        AddressID INT,
        UserId INT NOT NULL,
        CreatedBy INT,
        CreatedDate DATETIME DEFAULT GETDATE(),
        ModifiedBy INT,
        ModifiedDate DATETIME
    );
    CREATE INDEX IX_tblCustomersOpen_UserId ON tblCustomersOpen (UserId);
END;

IF OBJECT_ID('ARDetailView', 'U') IS NULL
BEGIN
    CREATE TABLE ARDetailView (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CustGroup NVARCHAR(255),
        CUST NVARCHAR(20) NOT NULL,
        FOLIO NVARCHAR(10),
        TopItem NVARCHAR(255),
        Type NVARCHAR(10),
        TRANS_NO NVARCHAR(100) NOT NULL,
        REF_NO NVARCHAR(100),
        TranDate DATETIME,
        D_AMOUNT DECIMAL(18,2) NOT NULL DEFAULT 0,
        C_AMOUNT DECIMAL(18,2) NOT NULL DEFAULT 0,
        BALANCE DECIMAL(18,2) NOT NULL DEFAULT 0,
        DaysOld INT,
        Checked BIT NOT NULL DEFAULT 0,
        ARID INT,
        UserId INT NOT NULL,
        CreatedBy INT,
        CreatedDate DATETIME DEFAULT GETDATE(),
        ModifiedBy INT,
        ModifiedDate DATETIME
    );
    CREATE INDEX IX_ARDetailView_UserId ON ARDetailView (UserId);
END;

IF OBJECT_ID('tblActivationsLookup', 'U') IS NULL
BEGIN
    CREATE TABLE tblActivationsLookup (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Invoice NVARCHAR(50) NOT NULL,
        InvoiceDate DATETIME,
        MaxOfID INT,
        Customer NVARCHAR(255),
        ActivationsTerritory NVARCHAR(255),
        MSD NVARCHAR(255),
        WebOrderID NVARCHAR(255),
        CustomerPostal NVARCHAR(255),
        ShipToPostal NVARCHAR(255),
        CostBudgetCode NVARCHAR(255),
        CustomerPONo NVARCHAR(255),
        UserName NVARCHAR(255),
        CellPhoneNo NVARCHAR(255),
        CountGovChannel DECIMAL(18,2),
        CountGovFee DECIMAL(18,2),
        UserId INT NOT NULL,
        CreatedBy INT,
        CreatedDate DATETIME DEFAULT GETDATE(),
        ModifiedBy INT,
        ModifiedDate DATETIME
    );
    CREATE INDEX IX_tblActivationsLookup_UserId ON tblActivationsLookup (UserId);
END;

IF OBJECT_ID('tblUsers', 'U') IS NULL
BEGIN
    CREATE TABLE tblUsers (
        ID INT IDENTITY(1,1) PRIMARY KEY,
        DomainUser NVARCHAR(255) NOT NULL,
        Initials NVARCHAR(255),
        DefaultChannel INT,
        CreatedBy INT,
        CreatedDate DATETIME DEFAULT GETDATE(),
        ModifiedBy INT,
        ModifiedDate DATETIME
    );

    -- Insert Default Mock Users
    INSERT INTO tblUsers (DomainUser, Initials, DefaultChannel, CreatedBy) VALUES
    ('domain\john.doe', 'JD', 1, 1),
    ('domain\jane.smith', 'JS', 2, 1),
    ('domain\robert.lee', 'RL', 3, 1),
    ('domain\mary.clark', 'MC', 5, 1),
    ('domain\alex.jones', 'AJ', 11, 1);
END;



---------------
SELECT 
    t.account_no, 
    a.name AS "AccountName", 
    t.date AS "Date", 
    t.trans_no, 
    t.where_from AS "Source", 
    t.gl_user AS "User", 
    t.gl_memo, 
    t.mf_who AS "Type", 
    t.mf_key AS "Entity", 
    t.mf_tran AS "Document",  
    t.debit_amt, 
    t.credit_amt, 
    (t.debit_amt - t.credit_amt) AS balance,
    t.post_date
FROM gl_transactions t
INNER JOIN gl_accounts a 
    ON a.division = t.division 
    AND a.account_no = t.account_no 
    AND a.currency = t.currency
WHERE t.account_no = '11101' -- Replace with your account number
  AND t.date BETWEEN '2026-01-01' AND '2026-06-01' -- Replace with your start/end date
ORDER BY t.date;
2. Application Database (MS SQL Server) Query (For WebOrderID)
Excel में WebOrderID column की value application database के SalesActivations table से match होती है। इसे check करने के लिए SQL Server (SSMS) में यह query run करें:

sql
SELECT Invoice10, WebOrderID 
FROM SalesActivations 
WHERE Invoice10 IN ('YOUR_DOCUMENT_NO_1', 'YOUR_DOCUMENT_NO_2'); 
-- यहाँ Spire query से मिले 'Document' numbers को डालें
Match करने का तरीका:
Spire PostgreSQL query से जो records, debits, credits, और balances आ रहे हैं, वे Excel file के corresponding columns से match होने चाहिए।
Spire query के Document number के base पर SalesActivations table से fetch किया गया WebOrderID Excel file के WebOrderID column में map हो रहा है या नहीं, यह check कर लें।
