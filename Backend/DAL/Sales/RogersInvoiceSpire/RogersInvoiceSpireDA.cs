using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.IO;
using OfficeOpenXml;

namespace DAL.Sales.RogersInvoiceSpire
{
    public class RogersInvoiceSpireDA : IRogersInvoiceSpireDA
    {
        private readonly string _sqlConnStr;
        private readonly string _pgConnStr;

        public RogersInvoiceSpireDA(IConfiguration config)
        {
            _sqlConnStr = config.GetConnectionString("bvactivation_Connection") ?? "";
            _pgConnStr = config.GetConnectionString("spire_Connection") ?? "";
        }

        private async Task EnsureTablesExistAsync()
        {
            using (var conn = new SqlConnection(_sqlConnStr))
            {
                await conn.OpenAsync();

                // 1. tblACCReceipts
                string checkAccReceipts = @"
                    IF OBJECT_ID('tblACCReceipts', 'U') IS NULL
                    BEGIN
                        CREATE TABLE tblACCReceipts (
                            RECPT_KEY INT PRIMARY KEY,
                            WHSE VARCHAR(50) NULL,
                            CODE VARCHAR(50) NULL,
                            INVR_DATE VARCHAR(50) NULL,
                            SUPP VARCHAR(255) NULL,
                            LOCN VARCHAR(255) NULL,
                            QTY INT NULL,
                            COST FLOAT NULL,
                            PO_NO VARCHAR(255) NULL
                        );
                    END;";
                using (var cmd = new SqlCommand(checkAccReceipts, conn))
                {
                    cmd.CommandTimeout = 600;
                    await cmd.ExecuteNonQueryAsync();
                }

                // 2. tblRogersInvoiceSalesTaxes
                string checkSalesTaxes = @"
                    IF OBJECT_ID('tblRogersInvoiceSalesTaxes', 'U') IS NULL
                    BEGIN
                        CREATE TABLE tblRogersInvoiceSalesTaxes (
                            S_TAX_NO NUMERIC(18,0) PRIMARY KEY,
                            NAME VARCHAR(255) NULL,
                            RATE FLOAT NULL,
                            GL_ACCOUNT VARCHAR(255) NULL,
                            CreatedBy INT NULL,
                            CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
                            ModifiedBy INT NULL,
                            ModifiedDate DATETIME NULL
                        );
                    END;";
                using (var cmd = new SqlCommand(checkSalesTaxes, conn))
                {
                    cmd.CommandTimeout = 600;
                    await cmd.ExecuteNonQueryAsync();
                }

                // 3. tblRogersInvoiceOutputData
                string checkOutputData = @"
                    IF OBJECT_ID('tblRogersInvoiceOutputData', 'U') IS NULL
                    BEGIN
                        CREATE TABLE tblRogersInvoiceOutputData (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            UserId INT NOT NULL,
                            ChannelName VARCHAR(255) NULL,
                            PaymentMethod VARCHAR(255) NULL,
                            Type VARCHAR(255) NULL,
                            Type2 VARCHAR(255) NULL,
                            Invoice VARCHAR(50) NULL,
                            BVLineNo INT NULL,
                            InvoiceDate DATETIME NULL,
                            OrderDate DATETIME NULL,
                            CustName VARCHAR(255) NULL,
                            CustTerritory VARCHAR(50) NULL,
                            DealerCode VARCHAR(255) NULL,
                            MSD VARCHAR(255) NULL,
                            UserName VARCHAR(50) NULL,
                            CellPhoneNo VARCHAR(255) NULL,
                            PortedCTN VARCHAR(255) NULL,
                            VoicePlan VARCHAR(255) NULL,
                            DataPlan VARCHAR(255) NULL,
                            WebOrderID VARCHAR(255) NULL,
                            Qty FLOAT NULL,
                            PartNumber VARCHAR(50) NULL,
                            FreeAccessory VARCHAR(255) NULL,
                            IMEIESN VARCHAR(255) NULL,
                            CostPrice FLOAT NULL,
                            SellPrice FLOAT NULL,
                            TopUpOwing FLOAT NULL,
                            TopUpSDFAccCostAdjusted FLOAT NULL,
                            TopUpTotal FLOAT NULL,
                            AccessoryCost FLOAT NULL,
                            AccessoryPrice FLOAT NULL,
                            Fee FLOAT NULL,
                            FeeCount FLOAT NULL,
                            ARAmount FLOAT NULL,
                            AdjustmentType VARCHAR(50) NULL,
                            AccCurrentCostTotal FLOAT NULL,
                            AccSRPTotal FLOAT NULL,
                            AccSRP17Total FLOAT NULL,
                            AccSRP25Total FLOAT NULL,
                            AccSRP50Total FLOAT NULL,
                            AccCurrentCostDetails VARCHAR(255) NULL,
                            AccSRPDetails VARCHAR(255) NULL,
                            AccSRP17Details VARCHAR(255) NULL,
                            AccSRP25Details VARCHAR(255) NULL,
                            AccSRP50Details VARCHAR(255) NULL,
                            FreeAccessoryPart VARCHAR(255) NULL,
                            AccSellingPriceTotal FLOAT NULL,
                            ACCSellingPriceDetails VARCHAR(255) NULL,
                            Province VARCHAR(50) NULL,
                            BVARAmount FLOAT NULL,
                            HDWSRP FLOAT NULL,
                            SCOA VARCHAR(50) NULL,
                            InvoiceNet DECIMAL(18,2) NULL,
                            InvoiceShipping DECIMAL(18,2) NULL,
                            InvoiceTaxes DECIMAL(18,2) NULL,
                            InvoiceTotal DECIMAL(18,2) NULL,
                            ShipToProvince VARCHAR(10) NULL,
                            ACCQtys VARCHAR(255) NULL,
                            M2MOrderID VARCHAR(255) NULL,
                            TaxCode1 INT NULL,
                            TaxCode2 INT NULL,
                            BVReceiptCost DECIMAL(18,2) NULL,
                            NetIMEIReceiveCost DECIMAL(18,2) NULL,
                            NetPriceProtection DECIMAL(18,2) NULL,
                            ReturnClassification VARCHAR(255) NULL,
                            GSTRate FLOAT NULL,
                            PSTRate FLOAT NULL,
                            UpFrontEdgePrice DECIMAL(18,2) NULL,
                            ClaimCarrier VARCHAR(255) NULL,
                            ClaimNumber VARCHAR(255) NULL,
                            AROutstanding DECIMAL(18,2) NULL,
                            OriginalInvoice VARCHAR(255) NULL,
                            DeviceOfferTypeID INT NULL,
                            TaxFlag1 VARCHAR(255) NULL,
                            TaxFlag2 VARCHAR(255) NULL,
                            AccountNumber VARCHAR(255) NULL,
                            AgentName VARCHAR(255) NULL,
                            AgentEmail VARCHAR(255) NULL,
                            AgentContactNumber VARCHAR(255) NULL,
                            RogersHWMarginShare DECIMAL(18,2) NULL,
                            Term VARCHAR(255) NULL,
                            RDType VARCHAR(255) NULL,
                            PPOverpayment DECIMAL(18,2) NULL,
                            CreatedBy INT NULL,
                            CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
                            ModifiedBy INT NULL,
                            ModifiedDate DATETIME NULL
                        );
                        CREATE INDEX IX_tblRogersInvoiceOutputData_UserId ON tblRogersInvoiceOutputData(UserId);
                    END;";
                using (var cmd = new SqlCommand(checkOutputData, conn))
                {
                    cmd.CommandTimeout = 600;
                    await cmd.ExecuteNonQueryAsync();
                }

                // 4. tblRogersInvoiceAcquisitionDetail
                string checkAcqDetail = @"
                    IF OBJECT_ID('tblRogersInvoiceAcquisitionDetail', 'U') IS NOT NULL AND COL_LENGTH('tblRogersInvoiceAcquisitionDetail', 'WHSE') IS NULL
                    BEGIN
                        DROP TABLE tblRogersInvoiceAcquisitionDetail;
                    END;
                    IF OBJECT_ID('tblRogersInvoiceAcquisitionDetail', 'U') IS NULL
                    BEGIN
                        CREATE TABLE tblRogersInvoiceAcquisitionDetail (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            UserId INT NOT NULL,
                            ChannelName VARCHAR(255) NULL,
                            PaymentMethod VARCHAR(255) NULL,
                            Type VARCHAR(255) NULL,
                            Invoice VARCHAR(50) NULL,
                            WHSE VARCHAR(50) NULL,
                            BVLineNo INT NULL,
                            RecNoDetail INT NULL,
                            InvoiceDate DATETIME NULL,
                            OrderDate DATETIME NULL,
                            CustName VARCHAR(255) NULL,
                            CustTerritory VARCHAR(50) NULL,
                            DealerCode VARCHAR(255) NULL,
                            CellPhoneNo VARCHAR(255) NULL,
                            WebOrderID VARCHAR(255) NULL,
                            Type2 VARCHAR(255) NULL,
                            FreeAccessory VARCHAR(255) NULL,
                            [Topup Acc] FLOAT NULL,
                            AccessoryCost FLOAT NULL,
                            AccessoryPrice FLOAT NULL,
                            AccCurrentCostTotal FLOAT NULL,
                            AccSRPTotal FLOAT NULL,
                            AccSRP17Total FLOAT NULL,
                            AccSRP25Total FLOAT NULL,
                            AccSRP50Total FLOAT NULL,
                            ACCSellingPrice FLOAT NULL,
                            CODE VARCHAR(50) NULL,
                            Description VARCHAR(255) NULL,
                            ProdCode VARCHAR(50) NULL,
                            Qty FLOAT NULL,
                            BVCost FLOAT NULL,
                            BVCostExt FLOAT NULL,
                            BVPrice FLOAT NULL,
                            BVPriceExt FLOAT NULL,
                            TopUpAmt FLOAT NULL,
                            TopUpOrig FLOAT NULL,
                            TopUpModify VARCHAR(10) NULL,
                            CustPayExt FLOAT NULL,
                            AccGroup VARCHAR(50) NULL,
                            Margin VARCHAR(255) NULL,
                            AccSRP FLOAT NULL,
                            AccSRP17 FLOAT NULL,
                            AccSRP25 FLOAT NULL,
                            AccSRP50 FLOAT NULL,
                            TopUpRecalc FLOAT NULL,
                            MSD VARCHAR(255) NULL,
                            UserName VARCHAR(255) NULL,
                            VoicePlan VARCHAR(255) NULL,
                            DataPlan VARCHAR(255) NULL,
                            GSTRate FLOAT NULL,
                            PSTRate FLOAT NULL,
                            Fee FLOAT NULL,
                            FeePayback FLOAT NULL,
                            M2MOrderID VARCHAR(255) NULL,
                            ReturnClassification VARCHAR(255) NULL,
                            GSTFlag VARCHAR(255) NULL,
                            PSTFlag VARCHAR(255) NULL,
                            AdjustmentType VARCHAR(255) NULL,
                            BVReceiptNo VARCHAR(255) NULL,
                            BVReceiptNoInt INT NULL,
                            BVReceiptQty INT NULL,
                            BVReceiptCost FLOAT NULL,
                            BVReceiptDate DATETIME NULL,
                            RogersInvoiceCost FLOAT NULL,
                            DeviceOfferTypeID INT NULL,
                            ShipToProvince VARCHAR(255) NULL,
                            OriginalInvoice VARCHAR(255) NULL,
                            RDAccUnitCost FLOAT NULL,
                            AccessoryType VARCHAR(255) NULL,
                            RogersACCMarginShare DECIMAL(18,2) NULL,
                            AccountNumber VARCHAR(255) NULL,
                            AgentName VARCHAR(255) NULL,
                            AgentEmail VARCHAR(255) NULL,
                            AgentContactNumber VARCHAR(255) NULL,
                            RDType VARCHAR(255) NULL,
                            CreatedBy INT NULL,
                            CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
                            ModifiedBy INT NULL,
                            ModifiedDate DATETIME NULL
                        );
                        CREATE INDEX IX_tblRogersInvoiceAcquisitionDetail_UserId ON tblRogersInvoiceAcquisitionDetail(UserId);
                    END;";
                using (var cmd = new SqlCommand(checkAcqDetail, conn))
                {
                    cmd.CommandTimeout = 600;
                    await cmd.ExecuteNonQueryAsync();
                }

                // 5. tblRogersInvoiceAcquisitionAR
                string checkAcqAR = @"
                    IF OBJECT_ID('tblRogersInvoiceAcquisitionAR', 'U') IS NOT NULL AND COL_LENGTH('tblRogersInvoiceAcquisitionAR', 'WHSE') IS NULL
                    BEGIN
                        DROP TABLE tblRogersInvoiceAcquisitionAR;
                    END;
                    IF OBJECT_ID('tblRogersInvoiceAcquisitionAR', 'U') IS NULL
                    BEGIN
                        CREATE TABLE tblRogersInvoiceAcquisitionAR (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            UserId INT NOT NULL,
                            ChannelName VARCHAR(255) NULL,
                            PaymentMethod VARCHAR(255) NULL,
                            Type VARCHAR(255) NULL,
                            Invoice VARCHAR(50) NULL,
                            WHSE VARCHAR(50) NULL,
                            InvoiceDate DATETIME NULL,
                            OrderDate DATETIME NULL,
                            CustName VARCHAR(255) NULL,
                            CustTerritory VARCHAR(50) NULL,
                            DealerCode VARCHAR(255) NULL,
                            MSD VARCHAR(255) NULL,
                            UserName VARCHAR(50) NULL,
                            CellPhoneNo VARCHAR(255) NULL,
                            PortedCTN VARCHAR(255) NULL,
                            VoicePlan VARCHAR(255) NULL,
                            DataPlan VARCHAR(255) NULL,
                            WebOrderID VARCHAR(255) NULL,
                            Type2 VARCHAR(255) NULL,
                            Qty FLOAT NULL,
                            PartNumber VARCHAR(50) NULL,
                            FreeAccessory VARCHAR(255) NULL,
                            IMEIESN VARCHAR(255) NULL,
                            CostPrice FLOAT NULL,
                            SellPrice FLOAT NULL,
                            TopUpOwing FLOAT NULL,
                            [Topup Acc] FLOAT NULL,
                            TopUpTotal FLOAT NULL,
                            AccessoryPrice FLOAT NULL,
                            Fee FLOAT NULL,
                            FeeCount INT NULL,
                            GST FLOAT NULL,
                            PST FLOAT NULL,
                            HST FLOAT NULL,
                            QST FLOAT NULL,
                            ARAmount FLOAT NULL,
                            HDWChargeToCustomer DECIMAL(18,2) NULL,
                            [True HDW TopUp] VARCHAR(255) NULL,
                            SCOA VARCHAR(50) NULL,
                            ShipToProvince VARCHAR(10) NULL,
                            CreatedBy INT NULL,
                            CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
                            ModifiedBy INT NULL,
                            ModifiedDate DATETIME NULL
                        );
                        CREATE INDEX IX_tblRogersInvoiceAcquisitionAR_UserId ON tblRogersInvoiceAcquisitionAR(UserId);
                    END;";
                using (var cmd = new SqlCommand(checkAcqAR, conn))
                {
                    cmd.CommandTimeout = 600;
                    await cmd.ExecuteNonQueryAsync();
                }

                // 6. tblRogersInvoiceRecentReceipts
                string checkRecentRec = @"
                    IF OBJECT_ID('tblRogersInvoiceRecentReceipts', 'U') IS NULL
                    BEGIN
                        CREATE TABLE tblRogersInvoiceRecentReceipts (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            UserId INT NOT NULL,
                            PartNumber VARCHAR(255) NULL,
                            ReceiptDate1 DATETIME NULL, Qty1 INT NULL, Cost1 FLOAT NULL, Invoice1 VARCHAR(255) NULL,
                            ReceiptDate2 DATETIME NULL, Qty2 INT NULL, Cost2 FLOAT NULL, Invoice2 VARCHAR(255) NULL,
                            ReceiptDate3 DATETIME NULL, Qty3 INT NULL, Cost3 FLOAT NULL, Invoice3 VARCHAR(255) NULL,
                            ReceiptDate4 DATETIME NULL, Qty4 INT NULL, Cost4 FLOAT NULL, Invoice4 VARCHAR(255) NULL,
                            ReceiptDate5 DATETIME NULL, Qty5 INT NULL, Cost5 FLOAT NULL, Invoice5 VARCHAR(255) NULL,
                            ReceiptDate6 DATETIME NULL, Qty6 INT NULL, Cost6 FLOAT NULL, Invoice6 VARCHAR(255) NULL,
                            ReceiptDate7 DATETIME NULL, Qty7 INT NULL, Cost7 FLOAT NULL, Invoice7 VARCHAR(255) NULL,
                            CreatedBy INT NULL,
                            CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
                            ModifiedBy INT NULL,
                            ModifiedDate DATETIME NULL
                        );
                        CREATE INDEX IX_tblRogersInvoiceRecentReceipts_UserId ON tblRogersInvoiceRecentReceipts(UserId);
                    END;";
                using (var cmd = new SqlCommand(checkRecentRec, conn))
                {
                    cmd.CommandTimeout = 600;
                    await cmd.ExecuteNonQueryAsync();
                }

                // 7. tblRogersInvoiceUPSLost
                string checkUPSLost = @"
                    IF OBJECT_ID('tblRogersInvoiceUPSLost', 'U') IS NULL
                    BEGIN
                        CREATE TABLE tblRogersInvoiceUPSLost (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            UserId INT NOT NULL,
                            Invoice VARCHAR(50) NULL,
                            InvoiceDate DATETIME NULL,
                            OrderDate DATETIME NULL,
                            OriginalInvoice VARCHAR(255) NULL,
                            CustName VARCHAR(255) NULL,
                            Territory VARCHAR(255) NULL,
                            WebOrderID VARCHAR(255) NULL,
                            CellPhoneNo VARCHAR(255) NULL,
                            Qty FLOAT NULL,
                            PartNumber VARCHAR(50) NULL,
                            FreeAccessory VARCHAR(255) NULL,
                            IMEIESN VARCHAR(255) NULL,
                            CostPrice FLOAT NULL,
                            AccessoryCost FLOAT NULL,
                            TotalBeforeTaxes FLOAT NULL,
                            HST FLOAT NULL,
                            TotalClaim FLOAT NULL,
                            Courier VARCHAR(255) NULL,
                            Claim VARCHAR(255) NULL,
                            [Group] VARCHAR(255) NULL,
                            OutstandingAR DECIMAL(18,2) NULL,
                            NetIMEIReceiveCost DECIMAL(18,2) NULL,
                            NetPriceProtection DECIMAL(18,2) NULL,
                            NetCost DECIMAL(18,2) NULL,
                            CreatedBy INT NULL,
                            CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
                            ModifiedBy INT NULL,
                            ModifiedDate DATETIME NULL
                        );
                        CREATE INDEX IX_tblRogersInvoiceUPSLost_UserId ON tblRogersInvoiceUPSLost(UserId);
                    END;";
                using (var cmd = new SqlCommand(checkUPSLost, conn))
                {
                    cmd.CommandTimeout = 600;
                    await cmd.ExecuteNonQueryAsync();
                }

                // 8. tblRogersInvoiceUPSLostUSER
                string checkUPSLostUser = @"
                    IF OBJECT_ID('tblRogersInvoiceUPSLostUSER', 'U') IS NULL
                    BEGIN
                        CREATE TABLE tblRogersInvoiceUPSLostUSER (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            UserId INT NOT NULL,
                            Invoice VARCHAR(50) NULL,
                            InvoiceDate DATETIME NULL,
                            OrderDate DATETIME NULL,
                            OriginalInvoice VARCHAR(255) NULL,
                            CustName VARCHAR(255) NULL,
                            Territory VARCHAR(255) NULL,
                            WebOrderID VARCHAR(255) NULL,
                            CellPhoneNo VARCHAR(255) NULL,
                            Qty FLOAT NULL,
                            PartNumber VARCHAR(50) NULL,
                            FreeAccessory VARCHAR(255) NULL,
                            IMEIESN VARCHAR(255) NULL,
                            CostPrice FLOAT NULL,
                            AccessoryCost FLOAT NULL,
                            TotalBeforeTaxes FLOAT NULL,
                            HST FLOAT NULL,
                            TotalClaim FLOAT NULL,
                            Courier VARCHAR(255) NULL,
                            Claim VARCHAR(255) NULL,
                            [Group] VARCHAR(255) NULL,
                            OutstandingAR DECIMAL(18,2) NULL,
                            NetIMEIReceiveCost DECIMAL(18,2) NULL,
                            NetPriceProtection DECIMAL(18,2) NULL,
                            NetCost DECIMAL(18,2) NULL,
                            CreatedBy INT NULL,
                            CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
                            ModifiedBy INT NULL,
                            ModifiedDate DATETIME NULL
                        );
                        CREATE INDEX IX_tblRogersInvoiceUPSLostUSER_UserId ON tblRogersInvoiceUPSLostUSER(UserId);
                    END;";
                using (var cmd = new SqlCommand(checkUPSLostUser, conn))
                {
                    cmd.CommandTimeout = 600;
                    await cmd.ExecuteNonQueryAsync();
                }

                // 9. tblRogersInvoiceTempReturnsValidation
                string checkReturnsVal = @"
                    IF OBJECT_ID('tblRogersInvoiceTempReturnsValidation', 'U') IS NULL
                    BEGIN
                        CREATE TABLE tblRogersInvoiceTempReturnsValidation (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            UserId INT NOT NULL,
                            ChannelName VARCHAR(255) NULL,
                            PaymentMethod VARCHAR(255) NULL,
                            Type VARCHAR(255) NULL,
                            Invoice VARCHAR(255) NULL,
                            InvoiceDate DATETIME NULL,
                            CustTerritory VARCHAR(255) NULL,
                            CellPhoneNo VARCHAR(255) NULL,
                            WebOrderID VARCHAR(255) NULL,
                            Qty FLOAT NULL,
                            PartNumber VARCHAR(255) NULL,
                            FreeAccessory VARCHAR(255) NULL,
                            IMEIESN VARCHAR(255) NULL,
                            CostPrice FLOAT NULL,
                            SellPrice FLOAT NULL,
                            TopUpOwing FLOAT NULL,
                            AccessoryCost FLOAT NULL,
                            AccessoryPrice FLOAT NULL,
                            [TopUp Acc] FLOAT NULL,
                            TopUpTotal FLOAT NULL,
                            ARAmount FLOAT NULL,
                            HDWChargeToCustomer FLOAT NULL,
                            [True HDW TopUp] FLOAT NULL,
                            ACCChargeToCx FLOAT NULL,
                            AccMargin FLOAT NULL,
                            [Group] VARCHAR(255) NULL,
                            Source VARCHAR(255) NULL,
                            ChannelName2 VARCHAR(255) NULL,
                            PaymentMethod2 VARCHAR(255) NULL,
                            Type2 VARCHAR(255) NULL,
                            Invoice2 VARCHAR(255) NULL,
                            InvoiceDate2 DATETIME NULL,
                            CustTerritory2 VARCHAR(255) NULL,
                            CellPhoneNo2 VARCHAR(255) NULL,
                            WebOrderID2 VARCHAR(255) NULL,
                            Qty2 FLOAT NULL,
                            PartNumber2 VARCHAR(255) NULL,
                            FreeAccessory2 VARCHAR(255) NULL,
                            IMEIESN2 VARCHAR(255) NULL,
                            CostPrice2 FLOAT NULL,
                            SellPrice2 FLOAT NULL,
                            TopUpOwing2 FLOAT NULL,
                            AccessoryCost2 FLOAT NULL,
                            AccessoryPrice2 FLOAT NULL,
                            [TopUp Acc2] FLOAT NULL,
                            TopUpTotal2 FLOAT NULL,
                            ARAmount2 FLOAT NULL,
                            HDWChargeToCustomer2 FLOAT NULL,
                            [True HDW TopUp2] FLOAT NULL,
                            ACCChargeToCx2 FLOAT NULL,
                            AccMargin2 FLOAT NULL,
                            Group2 VARCHAR(255) NULL,
                            CreatedBy INT NULL,
                            CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
                            ModifiedBy INT NULL,
                            ModifiedDate DATETIME NULL
                        );
                        CREATE INDEX IX_tblRogersInvoiceTempReturnsValidation_UserId ON tblRogersInvoiceTempReturnsValidation(UserId);
                    END;";
                using (var cmd = new SqlCommand(checkReturnsVal, conn))
                {
                    cmd.CommandTimeout = 600;
                    await cmd.ExecuteNonQueryAsync();
                }

                // 10. tblRogersInvoiceAcquisitionOutput
                string checkOutput = @"
                    IF OBJECT_ID('tblRogersInvoiceAcquisitionOutput', 'U') IS NOT NULL AND COL_LENGTH('tblRogersInvoiceAcquisitionOutput', 'WHSE') IS NULL
                    BEGIN
                        DROP TABLE tblRogersInvoiceAcquisitionOutput;
                    END;
                    IF OBJECT_ID('tblRogersInvoiceAcquisitionOutput', 'U') IS NULL
                    BEGIN
                        CREATE TABLE tblRogersInvoiceAcquisitionOutput (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            UserId INT NOT NULL,
                            ChannelName VARCHAR(255) NULL,
                            PaymentMethod VARCHAR(255) NULL,
                            Type VARCHAR(255) NULL,
                            Invoice VARCHAR(255) NULL,
                            WHSE VARCHAR(50) NULL,
                            InvoiceDate DATETIME NULL,
                            OrderDate DATETIME NULL,
                            CustName VARCHAR(255) NULL,
                            CustTerritory VARCHAR(255) NULL,
                            DealerCode VARCHAR(255) NULL,
                            MSD VARCHAR(255) NULL,
                            UserName VARCHAR(255) NULL,
                            CellPhoneNo VARCHAR(255) NULL,
                            PortedCTN VARCHAR(255) NULL,
                            VoicePlan VARCHAR(255) NULL,
                            DataPlan VARCHAR(255) NULL,
                            WebOrderID VARCHAR(255) NULL,
                            Type2 VARCHAR(255) NULL,
                            Qty FLOAT NULL,
                            PartNumber VARCHAR(255) NULL,
                            FreeAccessory VARCHAR(255) NULL,
                            IMEIESN VARCHAR(255) NULL,
                            CostPrice FLOAT NULL,
                            SellPrice FLOAT NULL,
                            TopUpOwing FLOAT NULL,
                            AccessoryCost FLOAT NULL,
                            AccessoryPrice FLOAT NULL,
                            [TopUp Acc] FLOAT NULL,
                            TopUpTotal FLOAT NULL,
                            Fee FLOAT NULL,
                            FeeCount FLOAT NULL,
                            ARAmount FLOAT NULL,
                            [RV-UEValue] DECIMAL(18,2) NULL,
                            HDWChargeToCustomer FLOAT NULL,
                            [HDWRV-UEValue] DECIMAL(18,2) NULL,
                            [True HDW TopUp] FLOAT NULL,
                            HDWMargin DECIMAL(18,2) NULL,
                            ACCChargeToCx FLOAT NULL,
                            AccMargin FLOAT NULL,
                            FeePayback FLOAT NULL,
                            DealerHDWMargin DECIMAL(18,2) NULL,
                            DealerACCMargin DECIMAL(18,2) NULL,
                            [Group] VARCHAR(255) NULL,
                            AccSellingPrice DECIMAL(18,2) NULL,
                            SalesBeforeTax FLOAT NULL,
                            [GST-HST] FLOAT NULL,
                            [PST-QST] FLOAT NULL,
                            M2MOrderID VARCHAR(255) NULL,
                            ReturnClassification VARCHAR(255) NULL,
                            Comments VARCHAR(255) NULL,
                            RogersHWMarginShare DECIMAL(18,2) NULL,
                            RogersACCMarginShare DECIMAL(18,2) NULL,
                            RateTierTransactionCount FLOAT NULL,
                            AccSeq INT NULL,
                            RDAccUnitCost FLOAT NULL,
                            RDAccExtendedCost FLOAT NULL,
                            Term VARCHAR(255) NULL,
                            AccessoryType VARCHAR(255) NULL,
                            BAN VARCHAR(255) NULL,
                            AgentName VARCHAR(255) NULL,
                            AgentEmail VARCHAR(255) NULL,
                            AgentContactNumber VARCHAR(255) NULL,
                            BVReceiptCost DECIMAL(18,2) NULL,
                            IMEIReceiveAppCost DECIMAL(18,2) NULL,
                            NetPriceProtection DECIMAL(18,2) NULL,
                            NetIMEIReceiveAppCost DECIMAL(18,2) NULL,
                            ReceiptDate1 DATETIME NULL, Invoice1 VARCHAR(255) NULL, Qty1 INT NULL, Cost1 FLOAT NULL,
                            ReceiptDate2 DATETIME NULL, Invoice2 VARCHAR(255) NULL, Qty2 INT NULL, Cost2 FLOAT NULL,
                            ReceiptDate3 DATETIME NULL, Invoice3 VARCHAR(255) NULL, Qty3 INT NULL, Cost3 FLOAT NULL,
                            ReceiptDate4 DATETIME NULL, Invoice4 VARCHAR(255) NULL, Qty4 INT NULL, Cost4 FLOAT NULL,
                            ReceiptDate5 DATETIME NULL, Invoice5 VARCHAR(255) NULL, Qty5 INT NULL, Cost5 FLOAT NULL,
                            ReceiptDate6 DATETIME NULL, Invoice6 VARCHAR(255) NULL, Qty6 INT NULL, Cost6 FLOAT NULL,
                            ReceiptDate7 DATETIME NULL, Invoice7 VARCHAR(255) NULL, Qty7 INT NULL, Cost7 FLOAT NULL,
                            BVReceiptNo VARCHAR(255) NULL,
                            BVReceiptNoInt INT NULL,
                            BVReceiptQty INT NULL,
                            BVReceiptCostAcc FLOAT NULL,
                            BVReceiptDate DATETIME NULL,
                            RogersInvoiceCost FLOAT NULL,
                            DeviceOfferTypeID INT NULL,
                            ShipToProvince VARCHAR(255) NULL,
                            GSTRate FLOAT NULL,
                            PSTRate FLOAT NULL,
                            OriginalInvoice VARCHAR(255) NULL,
                            TaxFlag1 VARCHAR(255) NULL,
                            TaxFlag2 VARCHAR(255) NULL,
                            RDType VARCHAR(255) NULL,
                            PPOverpayment DECIMAL(18,2) NULL,
                            CreatedBy INT NULL,
                            CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
                            ModifiedBy INT NULL,
                            ModifiedDate DATETIME NULL
                        );
                        CREATE INDEX IX_tblRogersInvoiceAcquisitionOutput_UserId ON tblRogersInvoiceAcquisitionOutput(UserId);
                    END;";
                using (var cmd = new SqlCommand(checkOutput, conn))
                {
                    cmd.CommandTimeout = 600;
                    await cmd.ExecuteNonQueryAsync();
                }

                // 11. tblRogersInvoiceAcquisitionOutputUSER
                string checkOutputUser = @"
                    IF OBJECT_ID('tblRogersInvoiceAcquisitionOutputUSER', 'U') IS NOT NULL AND COL_LENGTH('tblRogersInvoiceAcquisitionOutputUSER', 'WHSE') IS NULL
                    BEGIN
                        DROP TABLE tblRogersInvoiceAcquisitionOutputUSER;
                    END;
                    IF OBJECT_ID('tblRogersInvoiceAcquisitionOutputUSER', 'U') IS NULL
                    BEGIN
                        CREATE TABLE tblRogersInvoiceAcquisitionOutputUSER (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            UserId INT NOT NULL,
                            ChannelName VARCHAR(255) NULL,
                            PaymentMethod VARCHAR(255) NULL,
                            Type VARCHAR(255) NULL,
                            Invoice VARCHAR(255) NULL,
                            WHSE VARCHAR(50) NULL,
                            InvoiceDate DATETIME NULL,
                            OrderDate DATETIME NULL,
                            CustName VARCHAR(255) NULL,
                            CustTerritory VARCHAR(255) NULL,
                            DealerCode VARCHAR(255) NULL,
                            MSD VARCHAR(255) NULL,
                            UserName VARCHAR(255) NULL,
                            CellPhoneNo VARCHAR(255) NULL,
                            PortedCTN VARCHAR(255) NULL,
                            VoicePlan VARCHAR(255) NULL,
                            DataPlan VARCHAR(255) NULL,
                            WebOrderID VARCHAR(255) NULL,
                            Type2 VARCHAR(255) NULL,
                            Qty FLOAT NULL,
                            PartNumber VARCHAR(255) NULL,
                            PartNumberDescription VARCHAR(255) NULL,
                            IMEIESN VARCHAR(255) NULL,
                            HdwCost FLOAT NULL,
                            HdwSellPrice FLOAT NULL,
                            TopUpHdw FLOAT NULL,
                            AccessoryCost FLOAT NULL,
                            AccessoryPrice FLOAT NULL,
                            [TopUp Acc] FLOAT NULL,
                            TopUpTotal FLOAT NULL,
                            Fee FLOAT NULL,
                            FeePayback FLOAT NULL,
                            FeeCount FLOAT NULL,
                            ARAmount FLOAT NULL,
                            [RV-UEValue] DECIMAL(18,2) NULL,
                            HDWChargeToCustomer FLOAT NULL,
                            [HDWRV-UEValue] DECIMAL(18,2) NULL,
                            [True HDW TopUp] FLOAT NULL,
                            ACCChargeToCx FLOAT NULL,
                            AccMargin FLOAT NULL,
                            HDWMargin FLOAT NULL,
                            [Group] VARCHAR(255) NULL,
                            ShipToProvince VARCHAR(255) NULL,
                            SalesBeforeTax FLOAT NULL,
                            [GST-HST] FLOAT NULL,
                            [PST-QST] FLOAT NULL,
                            Total FLOAT NULL,
                            R4BOrderID VARCHAR(255) NULL,
                            ReturnClassification VARCHAR(255) NULL,
                            RateTierTransactionCount FLOAT NULL,
                            Comments VARCHAR(255) NULL,
                            AccSeq INT NULL,
                            RDAccUnitCost FLOAT NULL,
                            RDAccExtendedCost FLOAT NULL,
                            AccessoryType VARCHAR(255) NULL,
                            BAN VARCHAR(255) NULL,
                            AgentName VARCHAR(255) NULL,
                            AgentEmail VARCHAR(255) NULL,
                            AgentContactNumber VARCHAR(255) NULL,
                            RogersHWMarginShare DECIMAL(18,2) NULL,
                            RogersACCMarginShare DECIMAL(18,2) NULL,
                            Term VARCHAR(255) NULL,
                            BVReceiptCost DECIMAL(18,2) NULL,
                            IMEIReceiveAppCost DECIMAL(18,2) NULL,
                            NetPriceProtection DECIMAL(18,2) NULL,
                            NetIMEIReceiveAppCost DECIMAL(18,2) NULL,
                            ReceiptDate1 DATETIME NULL, Invoice1 VARCHAR(255) NULL, Qty1 INT NULL, Cost1 FLOAT NULL,
                            ReceiptDate2 DATETIME NULL, Invoice2 VARCHAR(255) NULL, Qty2 INT NULL, Cost2 FLOAT NULL,
                            ReceiptDate3 DATETIME NULL, Invoice3 VARCHAR(255) NULL, Qty3 INT NULL, Cost3 FLOAT NULL,
                            BVReceiptNo VARCHAR(255) NULL,
                            BVReceiptNoInt INT NULL,
                            BVReceiptQty INT NULL,
                            BVReceiptCostAcc FLOAT NULL,
                            BVReceiptDate DATETIME NULL,
                            RogersInvoiceCost FLOAT NULL,
                            DeviceOfferTypeID INT NULL,
                            DealerHDWMargin DECIMAL(18,2) NULL,
                            DealerACCMargin DECIMAL(18,2) NULL,
                            RDType VARCHAR(255) NULL,
                            PPOverpayment DECIMAL(18,2) NULL,
                            CreatedBy INT NULL,
                            CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
                            ModifiedBy INT NULL,
                            ModifiedDate DATETIME NULL
                        );
                        CREATE INDEX IX_tblRogersInvoiceAcquisitionOutputUSER_UserId ON tblRogersInvoiceAcquisitionOutputUSER(UserId);
                    END;";
                using (var cmd = new SqlCommand(checkOutputUser, conn))
                {
                    cmd.CommandTimeout = 600;
                    await cmd.ExecuteNonQueryAsync();
                }

                // 12. tblRogersInvoiceAcquisitionARUSER
                string checkARUser = @"
                    IF OBJECT_ID('tblRogersInvoiceAcquisitionARUSER', 'U') IS NOT NULL AND COL_LENGTH('tblRogersInvoiceAcquisitionARUSER', 'WHSE') IS NULL
                    BEGIN
                        DROP TABLE tblRogersInvoiceAcquisitionARUSER;
                    END;
                    IF OBJECT_ID('tblRogersInvoiceAcquisitionARUSER', 'U') IS NULL
                    BEGIN
                        CREATE TABLE tblRogersInvoiceAcquisitionARUSER (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            UserId INT NOT NULL,
                            ChannelName VARCHAR(255) NULL,
                            PaymentMethod VARCHAR(255) NULL,
                            Type VARCHAR(255) NULL,
                            Invoice VARCHAR(50) NULL,
                            WHSE VARCHAR(50) NULL,
                            InvoiceDate DATETIME NULL,
                            OrderDate DATETIME NULL,
                            CustName VARCHAR(255) NULL,
                            CustTerritory VARCHAR(50) NULL,
                            DealerCode VARCHAR(255) NULL,
                            MSD VARCHAR(255) NULL,
                            UserName VARCHAR(50) NULL,
                            CellPhoneNo VARCHAR(255) NULL,
                            PortedCTN VARCHAR(255) NULL,
                            VoicePlan VARCHAR(255) NULL,
                            DataPlan VARCHAR(255) NULL,
                            WebOrderID VARCHAR(255) NULL,
                            Type2 VARCHAR(255) NULL,
                            Qty FLOAT NULL,
                            PartNumber VARCHAR(50) NULL,
                            PartNumberDescription VARCHAR(255) NULL,
                            IMEIESN VARCHAR(255) NULL,
                            HdwCost FLOAT NULL,
                            HdwSellPrice FLOAT NULL,
                            TopUpHdw FLOAT NULL,
                            [Topup Acc] FLOAT NULL,
                            TopUpTotal FLOAT NULL,
                            AccessoryPrice FLOAT NULL,
                            Fee FLOAT NULL,
                            FeeCount INT NULL,
                            GST FLOAT NULL,
                            PST FLOAT NULL,
                            HST FLOAT NULL,
                            QST FLOAT NULL,
                            ARAmount FLOAT NULL,
                            HDWChargeToCustomer FLOAT NULL,
                            [True HDW TopUp] VARCHAR(255) NULL,
                            SCOA VARCHAR(50) NULL,
                            ShipToProvince VARCHAR(10) NULL,
                            CreatedBy INT NULL,
                            CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
                            ModifiedBy INT NULL,
                            ModifiedDate DATETIME NULL
                        );
                        CREATE INDEX IX_tblRogersInvoiceAcquisitionARUSER_UserId ON tblRogersInvoiceAcquisitionARUSER(UserId);
                    END;";
                using (var cmd = new SqlCommand(checkARUser, conn))
                {
                    cmd.CommandTimeout = 600;
                    await cmd.ExecuteNonQueryAsync();
                }

                // 13. tblRogersInvoiceAcquisitionDetailUSER
                string checkDetailUser = @"
                    IF OBJECT_ID('tblRogersInvoiceAcquisitionDetailUSER', 'U') IS NOT NULL AND COL_LENGTH('tblRogersInvoiceAcquisitionDetailUSER', 'WHSE') IS NULL
                    BEGIN
                        DROP TABLE tblRogersInvoiceAcquisitionDetailUSER;
                    END;
                    IF OBJECT_ID('tblRogersInvoiceAcquisitionDetailUSER', 'U') IS NULL
                    BEGIN
                        CREATE TABLE tblRogersInvoiceAcquisitionDetailUSER (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            UserId INT NOT NULL,
                            ChannelName VARCHAR(255) NULL,
                            PaymentMethod VARCHAR(255) NULL,
                            Type VARCHAR(255) NULL,
                            Invoice VARCHAR(50) NULL,
                            WHSE VARCHAR(50) NULL,
                            BVLineNo INT NULL,
                            RecNoDetail INT NULL,
                            InvoiceDate DATETIME NULL,
                            OrderDate DATETIME NULL,
                            CustName VARCHAR(255) NULL,
                            CustTerritory VARCHAR(50) NULL,
                            DealerCode VARCHAR(255) NULL,
                            CellPhoneNo VARCHAR(255) NULL,
                            WebOrderID VARCHAR(255) NULL,
                            Type2 VARCHAR(255) NULL,
                            FreeAccessory VARCHAR(255) NULL,
                            [Topup Acc] FLOAT NULL,
                            AccessoryCost FLOAT NULL,
                            AccessoryPrice FLOAT NULL,
                            AccCurrentCostTotal FLOAT NULL,
                            AccSRPTotal FLOAT NULL,
                            AccSRP17Total FLOAT NULL,
                            AccSRP25Total FLOAT NULL,
                            AccSRP50Total FLOAT NULL,
                            ACCSellingPrice FLOAT NULL,
                            CODE VARCHAR(50) NULL,
                            Description VARCHAR(255) NULL,
                            ProdCode VARCHAR(50) NULL,
                            Qty FLOAT NULL,
                            BVCost FLOAT NULL,
                            BVCostExt FLOAT NULL,
                            BVPrice FLOAT NULL,
                            BVPriceExt FLOAT NULL,
                            TopUpAmt FLOAT NULL,
                            TopUpOrig FLOAT NULL,
                            TopUpModify VARCHAR(10) NULL,
                            CustPayExt FLOAT NULL,
                            AccGroup VARCHAR(50) NULL,
                            Margin VARCHAR(255) NULL,
                            AccSRP FLOAT NULL,
                            AccSRP17 FLOAT NULL,
                            AccSRP25 FLOAT NULL,
                            AccSRP50 FLOAT NULL,
                            TopUpRecalc FLOAT NULL,
                            MSD VARCHAR(255) NULL,
                            UserName VARCHAR(255) NULL,
                            VoicePlan VARCHAR(255) NULL,
                            DataPlan VARCHAR(255) NULL,
                            GSTRate FLOAT NULL,
                            PSTRate FLOAT NULL,
                            Fee FLOAT NULL,
                            FeePayback FLOAT NULL,
                            M2MOrderID VARCHAR(255) NULL,
                            ReturnClassification VARCHAR(255) NULL,
                            GSTFlag VARCHAR(255) NULL,
                            PSTFlag VARCHAR(255) NULL,
                            AdjustmentType VARCHAR(255) NULL,
                            BVReceiptNo VARCHAR(255) NULL,
                            BVReceiptNoInt INT NULL,
                            BVReceiptQty INT NULL,
                            BVReceiptCost FLOAT NULL,
                            BVReceiptDate DATETIME NULL,
                            RogersInvoiceCost FLOAT NULL,
                            DeviceOfferTypeID INT NULL,
                            ShipToProvince VARCHAR(255) NULL,
                            OriginalInvoice VARCHAR(255) NULL,
                            RDAccUnitCost FLOAT NULL,
                            AccessoryType VARCHAR(255) NULL,
                            RogersACCMarginShare DECIMAL(18,2) NULL,
                            AccountNumber VARCHAR(255) NULL,
                            AgentName VARCHAR(255) NULL,
                            AgentEmail VARCHAR(255) NULL,
                            AgentContactNumber VARCHAR(255) NULL,
                            RDType VARCHAR(255) NULL,
                            CreatedBy INT NULL,
                            CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
                            ModifiedBy INT NULL,
                            ModifiedDate DATETIME NULL
                        );
                        CREATE INDEX IX_tblRogersInvoiceAcquisitionDetailUSER_UserId ON tblRogersInvoiceAcquisitionDetailUSER(UserId);
                    END;";
                using (var cmd = new SqlCommand(checkDetailUser, conn))
                {
                    cmd.CommandTimeout = 600;
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<ProcessDataResult> ProcessDataAsync(ProcessDataRequest request, int userId)
        {
            try
            {
                // Auto-tables creation
                await EnsureTablesExistAsync();

                // 1. Clear tables for the current User
                using (var sqlConn = new SqlConnection(_sqlConnStr))
                {
                    await sqlConn.OpenAsync();

                    string clearSql = @"
                        DELETE FROM tblRogersInvoiceSalesTaxes; -- Common lookup, refresh on process
                        DELETE FROM tblRogersInvoiceOutputData WHERE UserId = @UserId;
                        DELETE FROM tblRogersInvoiceAcquisitionDetail WHERE UserId = @UserId;
                        DELETE FROM tblRogersInvoiceAcquisitionAR WHERE UserId = @UserId;
                        DELETE FROM tblRogersInvoiceRecentReceipts WHERE UserId = @UserId;
                        DELETE FROM tblRogersInvoiceUPSLost WHERE UserId = @UserId;
                        DELETE FROM tblRogersInvoiceUPSLostUSER WHERE UserId = @UserId;
                        DELETE FROM tblRogersInvoiceTempReturnsValidation WHERE UserId = @UserId;
                        DELETE FROM tblRogersInvoiceAcquisitionOutput WHERE UserId = @UserId;
                        DELETE FROM tblRogersInvoiceAcquisitionOutputUSER WHERE UserId = @UserId;
                        DELETE FROM tblRogersInvoiceAcquisitionARUSER WHERE UserId = @UserId;
                        DELETE FROM tblRogersInvoiceAcquisitionDetailUSER WHERE UserId = @UserId;";
                    
                    using (var cmd = new SqlCommand(clearSql, sqlConn))
                    {
                        cmd.CommandTimeout = 600;
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                // 2. Append Sales Taxes from Spire PostgreSQL database
                using (var pgConn = new NpgsqlConnection(_pgConnStr))
                using (var sqlConn = new SqlConnection(_sqlConnStr))
                {
                    await pgConn.OpenAsync();
                    await sqlConn.OpenAsync();

                    var taxList = new List<(decimal taxNo, string name, double rate, string glAcct)>();
                    string pgTaxSql = "SELECT tax_no, name, rate, gl_account FROM sales_taxes";
                    using (var cmd = new NpgsqlCommand(pgTaxSql, pgConn))
                    {
                        cmd.CommandTimeout = 600;
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                taxList.Add((
                                    Convert.ToDecimal(reader.GetValue(0)),
                                    reader.IsDBNull(1) ? "" : reader.GetString(1),
                                    reader.IsDBNull(2) ? 0.0 : Convert.ToDouble(reader.GetValue(2)),
                                    reader.IsDBNull(3) ? "" : reader.GetString(3)
                                ));
                            }
                        }
                    }

                    using (var bulkCmd = sqlConn.CreateCommand())
                    {
                        bulkCmd.CommandTimeout = 600;
                        foreach (var tax in taxList)
                        {
                            bulkCmd.CommandText = "INSERT INTO tblRogersInvoiceSalesTaxes (S_TAX_NO, NAME, RATE, GL_ACCOUNT, CreatedBy) VALUES (@TaxNo, @Name, @Rate, @GlAcct, @UserId)";
                            bulkCmd.Parameters.Clear();
                            bulkCmd.Parameters.AddWithValue("@TaxNo", tax.taxNo);
                            bulkCmd.Parameters.AddWithValue("@Name", tax.name);
                            bulkCmd.Parameters.AddWithValue("@Rate", tax.rate);
                            bulkCmd.Parameters.AddWithValue("@GlAcct", tax.glAcct);
                            bulkCmd.Parameters.AddWithValue("@UserId", userId);
                            await bulkCmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                // 3. Resolve missing BVReceipt numbers from Spire PostgreSQL database
                using (var sqlConn = new SqlConnection(_sqlConnStr))
                {
                    await sqlConn.OpenAsync();

                    var missingReceipts = new List<(string invoice, DateTime invDate, string sku, string imei)>();
                    string selectSql = @"
                        SELECT Invoice, InvoiceDate, PartNumber, IMEIESN 
                        FROM SalesActivations 
                        WHERE InvoiceDate BETWEEN @StartDate AND @EndDate 
                          AND IMEIESN <> '' 
                          AND (BVReceipt IS NULL OR BVReceipt = '')
                        ORDER BY Invoice DESC";
                    
                    using (var cmd = new SqlCommand(selectSql, sqlConn))
                    {
                        cmd.CommandTimeout = 600;
                        cmd.Parameters.AddWithValue("@StartDate", request.StartDate.Trim());
                        cmd.Parameters.AddWithValue("@EndDate", request.EndDate.Trim());

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                missingReceipts.Add((
                                    reader.GetString(0),
                                    reader.GetDateTime(1),
                                    reader.IsDBNull(2) ? "" : reader.GetString(2),
                                    reader.IsDBNull(3) ? "" : reader.GetString(3)
                                ));
                            }
                        }
                    }

                    if (missingReceipts.Any())
                    {
                        var dt = new DataTable();
                        dt.Columns.Add("Invoice", typeof(string));
                        dt.Columns.Add("PartNumber", typeof(string));
                        dt.Columns.Add("IMEIESN", typeof(string));
                        dt.Columns.Add("BVReceipt", typeof(string));
                        dt.Columns.Add("BVReceiptNo", typeof(long));

                        var missingImeis = missingReceipts.Select(x => x.imei).Distinct().ToList();

                        using (var pgConn = new NpgsqlConnection(_pgConnStr))
                        {
                            await pgConn.OpenAsync();
                            string pgSql = @"
                                SELECT part_no, number, MAX(receipt_no) 
                                FROM inventory_serial_transactions 
                                WHERE whse = 'CO' AND link_type = 'PORD' 
                                  AND number = ANY(@Imeis)
                                GROUP BY part_no, number";
                            
                            var receiptLookup = new Dictionary<string, string>();

                            using (var pgCmd = new NpgsqlCommand(pgSql, pgConn))
                            {
                                pgCmd.CommandTimeout = 600;
                                pgCmd.Parameters.AddWithValue("Imeis", missingImeis.ToArray());

                                using (var reader = await pgCmd.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        string part = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                        string num = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                        string recNo = reader.GetValue(2).ToString();
                                        receiptLookup[$"{part}|{num}"] = recNo;
                                    }
                                }
                            }

                            foreach (var item in missingReceipts)
                            {
                                string key = $"{item.sku}|{item.imei}";
                                if (receiptLookup.TryGetValue(key, out string receiptNo))
                                {
                                    long receiptNoLong = 0;
                                    long.TryParse(receiptNo, out receiptNoLong);
                                    dt.Rows.Add(item.invoice, item.sku, item.imei, receiptNo, receiptNoLong);
                                }
                            }
                        }

                        if (dt.Rows.Count > 0)
                        {
                            string createTemp = @"
                                CREATE TABLE #TempBVReceipts (
                                    Invoice VARCHAR(50) COLLATE DATABASE_DEFAULT,
                                    PartNumber VARCHAR(50) COLLATE DATABASE_DEFAULT,
                                    IMEIESN VARCHAR(255) COLLATE DATABASE_DEFAULT,
                                    BVReceipt VARCHAR(255) COLLATE DATABASE_DEFAULT,
                                    BVReceiptNo BIGINT
                                )";
                            using (var cmd = new SqlCommand(createTemp, sqlConn)) await cmd.ExecuteNonQueryAsync();

                            using (var bulkCopy = new SqlBulkCopy(sqlConn))
                            {
                                bulkCopy.BulkCopyTimeout = 600;
                                bulkCopy.DestinationTableName = "#TempBVReceipts";
                                await bulkCopy.WriteToServerAsync(dt);
                            }

                            string updateSql = @"
                                UPDATE s
                                SET s.BVReceipt = t.BVReceipt, 
                                    s.BVReceiptNo = t.BVReceiptNo
                                FROM SalesActivations s
                                INNER JOIN #TempBVReceipts t 
                                    ON s.Invoice = t.Invoice AND s.PartNumber = t.PartNumber AND s.IMEIESN = t.IMEIESN";
                            
                            using (var cmd = new SqlCommand(updateSql, sqlConn))
                            {
                                cmd.CommandTimeout = 600;
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                // 4. Update accessory LIFO costs
                await UpdateACCcostsAsync(userId);

                // 5. Populate intermediate OutputData table
                using (var sqlConn = new SqlConnection(_sqlConnStr))
                {
                    await sqlConn.OpenAsync();

                    // Generate intermediate OutputData records based on VBA query
                    var tempOutput = new List<dynamic>();
                    string queryText = @"
                        SELECT 
                            sa.Invoice, sa.BVInvoiceLine, sa.InvoiceDate, sa.OrderDate, sa.CustName, sa.CustTerritory, 
                            sa.V21DealerCode, sa.MSD, sa.UserName, sa.CellPhoneNo, sa.PortedCTN, sa.VoicePlan, 
                        sa.DataPlan, sa.WebOrderID, sa.Qty, sa.PartNumber, sa.Description, sa.IMEIESN, 
                            sa.ItemCost, sa.ItemSellPrice, sa.TopUpSDFAccCost, sa.AccessoryCost, sa.AccessoryPrice, 
                            sa.Fee, sa.PayMeth, sa.GSTFlag, sa.PSTFlag, sa.AdjustmentType, sa.FreeAccessoryPart, 
                            sa.SCOA, sa.InvoiceNet, sa.InvoiceShipping, sa.InvoiceTaxes, sa.InvoiceTotal, 
                            sa.ShipToProvince, sa.M2MOrderID, sa.Tax1Code, sa.Tax2Code, sa.BVReceiptNo, 
                            sa.DeviceOfferTypeID, sa.CustPayAmount, sa.AccountNumber, sa.ClaimCarrier, sa.ClaimNumber, 
                            sa.OriginalInvoice, sa.AgentName, sa.AgentEmail, sa.AgentContactNumber, sa.RogersHWMarginShare, 
                            sa.Term, sa.RecordTypeExtended, sa.RecordType, sa.CustomerPostalFirstDigit, 
                            sa.ShipToPostalFirstDigit, sa.UpFrontEdgePrice,
                            t1.RATE AS Rate1, t2.RATE AS Rate2, p1.Province AS Province1, p2.Province AS Province2
                        FROM SalesActivations sa
                        LEFT JOIN tblRogersInvoiceSalesTaxes t1 ON sa.Tax1Code = t1.S_TAX_NO
                        LEFT JOIN tblRogersInvoiceSalesTaxes t2 ON sa.Tax2Code = t2.S_TAX_NO
                        LEFT JOIN PostalProvince p1 ON sa.CustomerPostalFirstDigit = p1.FirstDigit
                        LEFT JOIN PostalProvince p2 ON sa.ShipToPostalFirstDigit = p2.FirstDigit
                        WHERE sa.InvoiceDate BETWEEN @StartDate AND @EndDate
                          AND sa.CustTerritory NOT LIKE 'D%'
                          AND (sa.Supress IS NULL OR sa.Supress = 0)";

                    using (var cmd = new SqlCommand(queryText, sqlConn))
                    {
                        cmd.CommandTimeout = 600;
                        cmd.Parameters.AddWithValue("@StartDate", request.StartDate.Trim());
                        cmd.Parameters.AddWithValue("@EndDate", request.EndDate.Trim());

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string custTerritory = reader.IsDBNull(5) ? "" : reader.GetString(5);
                                string channel = reader.IsDBNull(6) ? "" : reader.GetString(6);
                                string feeType = ""; // Lookup if needed, default empty
                                string controlCentre = ""; // Lookup if needed, default empty
                                string termsText = ""; // Lookup if needed

                                double qtyVal = reader.IsDBNull(14) ? 0 : Convert.ToDouble(reader.GetValue(14));
                                string recTypeExtended = reader.IsDBNull(50) ? "" : reader.GetString(50);
                                string recType = reader.IsDBNull(51) ? "" : reader.GetString(51);
                                string partNo = reader.IsDBNull(15) ? "" : reader.GetString(15);

                                // Adjust Qty based on return rules
                                double adjustedQty = qtyVal;
                                string typeCol = DetermineType(
                                    reader.IsDBNull(16) ? "" : reader.GetString(16), // CAPHardware if mapped to Description? Wait, CAPHardware column
                                    reader.IsDBNull(11) ? "" : reader.GetString(11), // VoicePlan
                                    reader.IsDBNull(12) ? "" : reader.GetString(12), // DataPlan
                                    "", // ProdCode
                                    reader.IsDBNull(16) ? "" : reader.GetString(16), // Description
                                    recType,
                                    reader.IsDBNull(27) ? "" : reader.GetString(27), // AdjustmentType
                                    "", // OrderNo
                                    reader.IsDBNull(13) ? "" : reader.GetString(13), // WebOrderID
                                    recTypeExtended
                                );

                                if (typeCol.StartsWith("Return") && recTypeExtended.StartsWith("COAM") && qtyVal > 0)
                                {
                                    adjustedQty = qtyVal * -1;
                                }

                                double itemCost = reader.IsDBNull(18) ? 0 : Convert.ToDouble(reader.GetValue(18));
                                double itemSell = reader.IsDBNull(19) ? 0 : Convert.ToDouble(reader.GetValue(19));
                                double costPrice = qtyVal == 0 ? itemCost : itemCost * qtyVal;
                                double sellPrice = qtyVal == 0 ? itemSell : itemSell * qtyVal;

                                double topUpOwing = Math.Abs(sellPrice) > Math.Abs(costPrice) ? 0 : costPrice - sellPrice;
                                double topUpSDFAccCost = reader.IsDBNull(20) ? 0 : Convert.ToDouble(reader.GetValue(20));
                                double topUpSDFAccCostAdjusted = topUpSDFAccCost * -1;
                                double topUpTotal = topUpOwing + topUpSDFAccCostAdjusted;

                                double feeVal = reader.IsDBNull(23) ? 0 : Convert.ToDouble(reader.GetValue(23));
                                double feeCount = feeVal > 0 ? 1 : (feeVal < 0 ? -1 : 0);

                                string payMethVal = reader.IsDBNull(24) ? "" : reader.GetString(24);
                                double gstRate = reader.IsDBNull(55) ? 0 : Convert.ToDouble(reader.GetValue(55));
                                double pstRate = reader.IsDBNull(56) ? 0 : Convert.ToDouble(reader.GetValue(56));
                                string gstFlag = reader.IsDBNull(25) ? "" : reader.GetString(25);
                                string pstFlag = reader.IsDBNull(26) ? "" : reader.GetString(26);

                                double arAmount = 0;
                                if ((payMethVal == "1" || payMethVal == "0") && sellPrice != 0)
                                {
                                    double gstTaxRate = gstFlag == "Y" ? gstRate : 0;
                                    double pstTaxRate = pstFlag == "Y" ? pstRate : 0;
                                    arAmount = Math.Round(sellPrice * (1 + ((gstTaxRate + pstTaxRate) / 100.0)), 2);
                                }

                                string shipToProv = reader.IsDBNull(34) ? "" : reader.GetString(34);
                                if (string.IsNullOrEmpty(shipToProv))
                                {
                                    shipToProv = reader.IsDBNull(58) ? "" : reader.GetString(58);
                                }

                                double ufep = (reader.IsDBNull(54) ? 0 : Convert.ToDouble(reader.GetValue(54))) * qtyVal;
                                double rogersHWShare = reader.IsDBNull(48) ? 0 : Convert.ToDouble(reader.GetValue(48));
                                double rhms = qtyVal > 0 ? rogersHWShare : rogersHWShare * -1;

                                string termVal = reader.IsDBNull(49) ? "" : reader.GetValue(49).ToString() ?? "";

                                tempOutput.Add(new {
                                    ChannelName = DetermineChannel(custTerritory, channel, "", controlCentre),
                                    PaymentMethod = DeterminePayMethod(termsText),
                                    Type = typeCol,
                                    Type2 = DetermineTypeOld2(recTypeExtended, "", partNo, termVal),
                                    Invoice = reader.IsDBNull(0) ? "" : reader.GetString(0),
                                    BVLineNo = reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetValue(1)),
                                    InvoiceDate = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2),
                                    OrderDate = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                                    CustName = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                    CustTerritory = custTerritory,
                                    DealerCode = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                    MSD = reader.IsDBNull(7) ? "" : reader.GetString(7),
                                    UserName = reader.IsDBNull(8) ? "" : reader.GetString(8),
                                    CellPhoneNo = reader.IsDBNull(9) ? "" : reader.GetString(9),
                                    PortedCTN = reader.IsDBNull(10) ? "" : reader.GetString(10),
                                    VoicePlan = reader.IsDBNull(11) ? "" : reader.GetString(11),
                                    DataPlan = reader.IsDBNull(12) ? "" : reader.GetString(12),
                                    WebOrderID = reader.IsDBNull(13) ? "" : reader.GetString(13),
                                    Qty = adjustedQty,
                                    PartNumber = partNo,
                                    FreeAccessory = reader.IsDBNull(16) ? "" : reader.GetString(16),
                                    IMEIESN = reader.IsDBNull(17) ? "" : reader.GetString(17),
                                    CostPrice = costPrice,
                                    SellPrice = sellPrice,
                                    TopUpOwing = topUpOwing,
                                    TopUpSDFAccCostAdjusted = topUpSDFAccCostAdjusted,
                                    TopUpTotal = topUpTotal,
                                    AccessoryCost = reader.IsDBNull(21) ? 0.0 : Convert.ToDouble(reader.GetValue(21)),
                                    AccessoryPrice = reader.IsDBNull(22) ? 0.0 : Convert.ToDouble(reader.GetValue(22)),
                                    Fee = feeVal,
                                    FeeCount = feeCount,
                                    ARAmount = arAmount,
                                    AdjustmentType = reader.IsDBNull(27) ? "" : reader.GetString(27),
                                    FreeAccessoryPart = reader.IsDBNull(28) ? "" : reader.GetString(28),
                                    Province = reader.IsDBNull(57) ? "" : reader.GetString(57),
                                    BVARAmount = 0.0, // Calculated later if needed
                                    SCOA = reader.IsDBNull(29) ? "" : reader.GetString(29),
                                    InvoiceNet = reader.IsDBNull(30) ? 0m : Convert.ToDecimal(reader.GetValue(30)),
                                    InvoiceShipping = reader.IsDBNull(31) ? 0m : Convert.ToDecimal(reader.GetValue(31)),
                                    InvoiceTaxes = reader.IsDBNull(32) ? 0m : Convert.ToDecimal(reader.GetValue(32)),
                                    InvoiceTotal = reader.IsDBNull(33) ? 0m : Convert.ToDecimal(reader.GetValue(33)),
                                    ShipToProvince = shipToProv,
                                    M2MOrderID = reader.IsDBNull(35) ? "" : reader.GetString(35),
                                    TaxCode1 = reader.IsDBNull(36) ? 0 : Convert.ToInt32(reader.GetValue(36)),
                                    TaxCode2 = reader.IsDBNull(37) ? 0 : Convert.ToInt32(reader.GetValue(37)),
                                    BVReceiptCost = 0m, // Resolved via join
                                    NetIMEIReceiveCost = 0m,
                                    NetPriceProtection = 0m,
                                    GSTRate = gstRate,
                                    PSTRate = pstRate,
                                    UpFrontEdgePrice = ufep,
                                    ClaimCarrier = reader.IsDBNull(42) ? "" : reader.GetString(42),
                                    ClaimNumber = reader.IsDBNull(43) ? "" : reader.GetString(43),
                                    OriginalInvoice = reader.IsDBNull(44) ? "" : reader.GetString(44),
                                    DeviceOfferTypeID = reader.IsDBNull(39) ? 0 : Convert.ToInt32(reader.GetValue(39)),
                                    TaxFlag1 = gstFlag,
                                    TaxFlag2 = pstFlag,
                                    AccountNumber = reader.IsDBNull(41) ? "" : reader.GetString(41),
                                    AgentName = reader.IsDBNull(45) ? "" : reader.GetString(45),
                                    AgentEmail = reader.IsDBNull(46) ? "" : reader.GetString(46),
                                    AgentContactNumber = reader.IsDBNull(47) ? "" : reader.GetString(47),
                                    RogersHWMarginShare = rhms,
                                    Term = termVal,
                                    RDType = recTypeExtended
                                });
                            }
                        }
                    }

                    // Insert records into intermediate OutputData table
                    using (var bulkCmd = sqlConn.CreateCommand())
                    {
                        bulkCmd.CommandTimeout = 600;
                        foreach (var item in tempOutput)
                        {
                            bulkCmd.CommandText = @"
                                INSERT INTO tblRogersInvoiceOutputData (
                                    UserId, ChannelName, PaymentMethod, Type, Type2, Invoice, BVLineNo, InvoiceDate, OrderDate, 
                                    CustName, CustTerritory, DealerCode, MSD, UserName, CellPhoneNo, PortedCTN, VoicePlan, 
                                    DataPlan, WebOrderID, Qty, PartNumber, FreeAccessory, IMEIESN, CostPrice, SellPrice, 
                                    TopUpOwing, TopUpSDFAccCostAdjusted, TopUpTotal, AccessoryCost, AccessoryPrice, Fee, 
                                    FeeCount, ARAmount, AdjustmentType, FreeAccessoryPart, Province, BVARAmount, SCOA, 
                                    InvoiceNet, InvoiceShipping, InvoiceTaxes, InvoiceTotal, ShipToProvince, M2MOrderID, 
                                    TaxCode1, TaxCode2, ReturnClassification, GSTRate, PSTRate, UpFrontEdgePrice, 
                                    ClaimCarrier, ClaimNumber, OriginalInvoice, DeviceOfferTypeID, TaxFlag1, TaxFlag2, 
                                    AccountNumber, AgentName, AgentEmail, AgentContactNumber, RogersHWMarginShare, Term, RDType
                                ) VALUES (
                                    @UserId, @ChannelName, @PaymentMethod, @Type, @Type2, @Invoice, @BVLineNo, @InvoiceDate, @OrderDate, 
                                    @CustName, @CustTerritory, @DealerCode, @MSD, @UserName, @CellPhoneNo, @PortedCTN, @VoicePlan, 
                                    @DataPlan, @WebOrderID, @Qty, @PartNumber, @FreeAccessory, @IMEIESN, @CostPrice, @SellPrice, 
                                    @TopUpOwing, @TopUpSDFAccCostAdjusted, @TopUpTotal, @AccessoryCost, @AccessoryPrice, @Fee, 
                                    @FeeCount, @ARAmount, @AdjustmentType, @FreeAccessoryPart, @Province, @BVARAmount, @SCOA, 
                                    @InvoiceNet, @InvoiceShipping, @InvoiceTaxes, @InvoiceTotal, @ShipToProvince, @M2MOrderID, 
                                    @TaxCode1, @TaxCode2, @ReturnClassification, @GSTRate, @PSTRate, @UpFrontEdgePrice, 
                                    @ClaimCarrier, @ClaimNumber, @OriginalInvoice, @DeviceOfferTypeID, @TaxFlag1, @TaxFlag2, 
                                    @AccountNumber, @AgentName, @AgentEmail, @AgentContactNumber, @RogersHWMarginShare, @Term, @RDType
                                )";
                            
                            bulkCmd.Parameters.Clear();
                            bulkCmd.Parameters.AddWithValue("@UserId", userId);
                            bulkCmd.Parameters.AddWithValue("@ChannelName", (object)item.ChannelName ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@PaymentMethod", (object)item.PaymentMethod ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@Type", (object)item.Type ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@Type2", (object)item.Type2 ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@Invoice", (object)item.Invoice ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@BVLineNo", item.BVLineNo);
                            bulkCmd.Parameters.AddWithValue("@InvoiceDate", (object)item.InvoiceDate ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@OrderDate", (object)item.OrderDate ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@CustName", (object)item.CustName ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@CustTerritory", (object)item.CustTerritory ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@DealerCode", (object)item.DealerCode ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@MSD", (object)item.MSD ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@UserName", (object)item.UserName ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@CellPhoneNo", (object)item.CellPhoneNo ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@PortedCTN", (object)item.PortedCTN ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@VoicePlan", (object)item.VoicePlan ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@DataPlan", (object)item.DataPlan ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@WebOrderID", (object)item.WebOrderID ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@Qty", item.Qty);
                            bulkCmd.Parameters.AddWithValue("@PartNumber", (object)item.PartNumber ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@FreeAccessory", (object)item.FreeAccessory ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@IMEIESN", (object)item.IMEIESN ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@CostPrice", item.CostPrice);
                            bulkCmd.Parameters.AddWithValue("@SellPrice", item.SellPrice);
                            bulkCmd.Parameters.AddWithValue("@TopUpOwing", item.TopUpOwing);
                            bulkCmd.Parameters.AddWithValue("@TopUpSDFAccCostAdjusted", item.TopUpSDFAccCostAdjusted);
                            bulkCmd.Parameters.AddWithValue("@TopUpTotal", item.TopUpTotal);
                            bulkCmd.Parameters.AddWithValue("@AccessoryCost", item.AccessoryCost);
                            bulkCmd.Parameters.AddWithValue("@AccessoryPrice", item.AccessoryPrice);
                            bulkCmd.Parameters.AddWithValue("@Fee", item.Fee);
                            bulkCmd.Parameters.AddWithValue("@FeeCount", item.FeeCount);
                            bulkCmd.Parameters.AddWithValue("@ARAmount", item.ARAmount);
                            bulkCmd.Parameters.AddWithValue("@AdjustmentType", (object)item.AdjustmentType ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@FreeAccessoryPart", (object)item.FreeAccessoryPart ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@Province", (object)item.Province ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@BVARAmount", item.BVARAmount);
                            bulkCmd.Parameters.AddWithValue("@SCOA", (object)item.SCOA ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@InvoiceNet", item.InvoiceNet);
                            bulkCmd.Parameters.AddWithValue("@InvoiceShipping", item.InvoiceShipping);
                            bulkCmd.Parameters.AddWithValue("@InvoiceTaxes", item.InvoiceTaxes);
                            bulkCmd.Parameters.AddWithValue("@InvoiceTotal", item.InvoiceTotal);
                            bulkCmd.Parameters.AddWithValue("@ShipToProvince", (object)item.ShipToProvince ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@M2MOrderID", (object)item.M2MOrderID ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@TaxCode1", item.TaxCode1);
                            bulkCmd.Parameters.AddWithValue("@TaxCode2", item.TaxCode2);
                            
                            string returnClass = "";
                            if (item.Type == "Return")
                            {
                                returnClass = (item.Type2 == "Exchange Only" || item.Type2 == "Hardware Only" || item.Type2 == "HUP") ? "HUP" : "Acquisition";
                            }
                            bulkCmd.Parameters.AddWithValue("@ReturnClassification", returnClass);
                            bulkCmd.Parameters.AddWithValue("@GSTRate", item.GSTRate);
                            bulkCmd.Parameters.AddWithValue("@PSTRate", item.PSTRate);
                            bulkCmd.Parameters.AddWithValue("@UpFrontEdgePrice", item.UpFrontEdgePrice);
                            bulkCmd.Parameters.AddWithValue("@ClaimCarrier", (object)item.ClaimCarrier ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@ClaimNumber", (object)item.ClaimNumber ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@OriginalInvoice", (object)item.OriginalInvoice ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@DeviceOfferTypeID", item.DeviceOfferTypeID);
                            bulkCmd.Parameters.AddWithValue("@TaxFlag1", (object)item.TaxFlag1 ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@TaxFlag2", (object)item.TaxFlag2 ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@AccountNumber", (object)item.AccountNumber ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@AgentName", (object)item.AgentName ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@AgentEmail", (object)item.AgentEmail ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@AgentContactNumber", (object)item.AgentContactNumber ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@RogersHWMarginShare", item.RogersHWMarginShare);
                            bulkCmd.Parameters.AddWithValue("@Term", (object)item.Term ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@RDType", (object)item.RDType ?? DBNull.Value);

                            await bulkCmd.ExecuteNonQueryAsync();
                        }
                    }

                    // Update output ADV Fee count
                    string updateAdvFee = "UPDATE tblRogersInvoiceOutputData SET FeeCount = 0.4 WHERE UserId = @UserId AND CustTerritory = 'ADV' AND Fee <> 0";
                    using (var updCmd = new SqlCommand(updateAdvFee, sqlConn))
                    {
                        updCmd.CommandTimeout = 600;
                        updCmd.Parameters.AddWithValue("@UserId", userId);
                        await updCmd.ExecuteNonQueryAsync();
                    }
                }

                // 6. Populate AcquisitionDetail & AcquisitionAR
                await BuildAcquisitionDetailAsync(userId);
                await BuildAcquisitionARAsync(userId);

                // 7. Assemble AcquisitionOutput (UNION ALL)
                await AssembleAcquisitionOutputAsync(userId);

                // 8. Populate Recent Receipts and copy columns
                await BuildRecentReceiptsAsync(userId);

                // 9. Generate UPS Lost
                await BuildUPSLostAsync(userId);

                // 10. Perform final updates (TestNewTaxes & RecalcFDDAmounts)
                await ExecuteFinalUpdatesAsync(userId);

                // 11. Move final outputs to staging USER tables
                await MoveToUserStagingAsync(userId);

                // 12. Programmatically populate and update Excel files in C:\RogersInvoice
                await UpdateExcelReportsAsync(userId, request.StartDate, request.EndDate);

                return new ProcessDataResult { Success = true, Message = "Data preparation and Excel generation completed successfully." };
            }
            catch (Exception ex)
            {
                return new ProcessDataResult { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        private async Task UpdateACCcostsAsync(int userId)
        {
            try
            {
                using (var sqlConn = new SqlConnection(_sqlConnStr))
            {
                await sqlConn.OpenAsync();

                // 1. Get last RECPT_KEY in tblACCReceipts
                int lastReceiptKey = 0;
                string maxSql = "SELECT ISNULL(MAX(RECPT_KEY), 0) FROM tblACCReceipts";
                using (var cmd = new SqlCommand(maxSql, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    var res = await cmd.ExecuteScalarAsync();
                    if (res != null) lastReceiptKey = Convert.ToInt32(res);
                }

                // 2. Fetch new receipts from Postgres and insert into tblACCReceipts
                var newReceipts = new List<dynamic>();
                using (var pgConn = new NpgsqlConnection(_pgConnStr))
                {
                    await pgConn.OpenAsync();
                    string pgSql = @"
                        SELECT pr.id, pi.whse, pi.part_no, to_char(pr.receive_date, 'YYYYMMDD'), pr.vendor_no, pr.whse_location, pr.qty, pr.cost, pr.link_no
                        FROM inventory_receipts pr
                        INNER JOIN inventory pi ON pr.inventory_id = pi.id
                        WHERE pr.id > @LastKey AND pi.product_code = 'ACC' AND pr.link_table = 'PORD'";
                    
                    using (var pgCmd = new NpgsqlCommand(pgSql, pgConn))
                    {
                        pgCmd.CommandTimeout = 600;
                        pgCmd.Parameters.AddWithValue("LastKey", lastReceiptKey);

                        using (var reader = await pgCmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                newReceipts.Add(new {
                                    RecptKey = Convert.ToInt32(reader.GetValue(0)),
                                    Whse = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                    Code = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                    InvrDate = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                    Supp = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                    Locn = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                    Qty = reader.IsDBNull(6) ? 0 : Convert.ToInt32(reader.GetValue(6)),
                                    Cost = reader.IsDBNull(7) ? 0.0 : Convert.ToDouble(reader.GetValue(7)),
                                    PoNo = reader.IsDBNull(8) ? "" : reader.GetString(8)
                                });
                            }
                        }
                    }
                }

                if (newReceipts.Any())
                {
                    using (var bulkCmd = sqlConn.CreateCommand())
                    {
                        bulkCmd.CommandTimeout = 600;
                        foreach (var r in newReceipts)
                        {
                            bulkCmd.CommandText = @"
                                INSERT INTO tblACCReceipts (RECPT_KEY, WHSE, CODE, INVR_DATE, SUPP, LOCN, QTY, COST, PO_NO)
                                VALUES (@Key, @Whse, @Code, @InvrDate, @Supp, @Locn, @Qty, @Cost, @PoNo)";
                            bulkCmd.Parameters.Clear();
                            bulkCmd.Parameters.AddWithValue("@Key", r.RecptKey);
                            bulkCmd.Parameters.AddWithValue("@Whse", r.Whse);
                            bulkCmd.Parameters.AddWithValue("@Code", r.Code);
                            bulkCmd.Parameters.AddWithValue("@InvrDate", r.InvrDate);
                            bulkCmd.Parameters.AddWithValue("@Supp", r.Supp);
                            bulkCmd.Parameters.AddWithValue("@Locn", r.Locn);
                            bulkCmd.Parameters.AddWithValue("@Qty", r.Qty);
                            bulkCmd.Parameters.AddWithValue("@Cost", r.Cost);
                            bulkCmd.Parameters.AddWithValue("@PoNo", r.PoNo);
                            await bulkCmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                // 3. Select distinct items sold without receipt
                var soldItems = new List<(string whse, string code)>();
                string soldSql = @"
                    SELECT WHSE, CODE 
                    FROM SalesActivationsDetail 
                    WHERE Invoice >= '0006485532' AND BVReceiptNo IS NULL
                    GROUP BY WHSE, CODE";
                
                using (var cmd = new SqlCommand(soldSql, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            soldItems.Add((
                                reader.GetString(0),
                                reader.GetString(1)
                            ));
                        }
                    }
                }

                // 4. Match using LIFO order
                var updateDt = new DataTable();
                updateDt.Columns.Add("RecNoStr", typeof(string));
                updateDt.Columns.Add("RecNoInt", typeof(int));
                updateDt.Columns.Add("Qty", typeof(int));
                updateDt.Columns.Add("Cost", typeof(double));
                updateDt.Columns.Add("Date", typeof(DateTime));
                updateDt.Columns.Add("Inv", typeof(string));
                updateDt.Columns.Add("Whse", typeof(string));
                updateDt.Columns.Add("Code", typeof(string));

                using (var pgConn = new NpgsqlConnection(_pgConnStr))
                {
                    await pgConn.OpenAsync();

                    foreach (var item in soldItems)
                    {
                        // Get current inventory onhand from Postgres
                        double onhand = 0;
                        string onhandSql = "SELECT onhand_qty FROM inventory WHERE whse = @Whse AND part_no = @Part LIMIT 1";
                        using (var pgCmd = new NpgsqlCommand(onhandSql, pgConn))
                        {
                            pgCmd.CommandTimeout = 600;
                            pgCmd.Parameters.AddWithValue("Whse", item.whse);
                            pgCmd.Parameters.AddWithValue("Part", item.code);
                            var val = await pgCmd.ExecuteScalarAsync();
                            if (val != null) onhand = Convert.ToDouble(val);
                        }

                        // Get receipts from SQL Server tblACCReceipts
                        var receipts = new List<(int key, int qty, double cost, string date)>();
                        string recsSql = "SELECT RECPT_KEY, QTY, COST, INVR_DATE FROM tblACCReceipts WHERE WHSE = @Whse AND CODE = @Code ORDER BY RECPT_KEY DESC";
                        using (var cmd = new SqlCommand(recsSql, sqlConn))
                        {
                            cmd.CommandTimeout = 600;
                            cmd.Parameters.AddWithValue("@Whse", item.whse);
                            cmd.Parameters.AddWithValue("@Code", item.code);

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    receipts.Add((
                                        Convert.ToInt32(reader.GetValue(0)),
                                        reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetValue(1)),
                                        reader.IsDBNull(2) ? 0.0 : Convert.ToDouble(reader.GetValue(2)),
                                        reader.IsDBNull(3) ? "" : reader.GetString(3)
                                    ));
                                }
                            }
                        }

                        if (!receipts.Any()) continue;

                        // Skip onhand items
                        int recIdx = 0;
                        while (onhand >= 0 && recIdx < receipts.Count)
                        {
                            onhand -= receipts[recIdx].qty;
                            if (onhand >= 0)
                            {
                                recIdx++;
                            }
                        }

                        if (recIdx >= receipts.Count) continue;

                        double toConsume = onhand * -1;

                        // Query sold records
                        var sales = new List<(string inv, int qty)>();
                        string salesSql = "SELECT Invoice, Qty FROM SalesActivationsDetail WHERE Invoice >= '0006485532' AND WHSE = @Whse AND CODE = @Code AND BVReceiptNo IS NULL ORDER BY Invoice DESC";
                        using (var cmd = new SqlCommand(salesSql, sqlConn))
                        {
                            cmd.CommandTimeout = 600;
                            cmd.Parameters.AddWithValue("@Whse", item.whse);
                            cmd.Parameters.AddWithValue("@Code", item.code);

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    sales.Add((
                                        reader.GetString(0),
                                        Convert.ToInt32(reader.GetValue(1))
                                    ));
                                }
                            }
                        }

                        if (!sales.Any()) continue;

                        int saleIdx = 0;
                        while (saleIdx < sales.Count && recIdx < receipts.Count)
                        {
                            var currentRec = receipts[recIdx];

                            while (toConsume > 0 && saleIdx < sales.Count)
                            {
                                var currentSale = sales[saleIdx];
                                toConsume -= currentSale.qty;

                                string rKeyStr = currentRec.key.ToString("0000000000");
                                DateTime? rDate = null;
                                if (!string.IsNullOrEmpty(currentRec.date) && currentRec.date.Length == 8)
                                {
                                    int y = int.Parse(currentRec.date.Substring(0, 4));
                                    int m = int.Parse(currentRec.date.Substring(4, 2));
                                    int d = int.Parse(currentRec.date.Substring(6, 2));
                                    rDate = new DateTime(y, m, d);
                                }

                                updateDt.Rows.Add(
                                    rKeyStr, 
                                    currentRec.key, 
                                    currentRec.qty, 
                                    currentRec.cost, 
                                    rDate == null ? DBNull.Value : (object)rDate.Value, 
                                    currentSale.inv, 
                                    item.whse, 
                                    item.code
                                );

                                saleIdx++;
                            }

                            if (saleIdx >= sales.Count) break;

                            recIdx++;
                            if (recIdx < receipts.Count)
                            {
                                toConsume += receipts[recIdx].qty;
                            }
                        }
                    }
                }

                if (updateDt.Rows.Count > 0)
                {
                    string createTemp = @"
                        CREATE TABLE #TempAccUpdates (
                            RecNoStr VARCHAR(50) COLLATE DATABASE_DEFAULT,
                            RecNoInt INT,
                            Qty INT,
                            Cost FLOAT,
                            Date DATETIME,
                            Invoice VARCHAR(50) COLLATE DATABASE_DEFAULT,
                            WHSE VARCHAR(50) COLLATE DATABASE_DEFAULT,
                            CODE VARCHAR(50) COLLATE DATABASE_DEFAULT
                        )";
                    using (var cmd = new SqlCommand(createTemp, sqlConn)) await cmd.ExecuteNonQueryAsync();

                    using (var bulkCopy = new SqlBulkCopy(sqlConn))
                    {
                        bulkCopy.BulkCopyTimeout = 600;
                        bulkCopy.DestinationTableName = "#TempAccUpdates";
                        await bulkCopy.WriteToServerAsync(updateDt);
                    }

                    string updateSql = @"
                        UPDATE s
                        SET s.BVReceiptNo = t.RecNoStr, 
                            s.BVReceiptNoInt = t.RecNoInt, 
                            s.BVReceiptQty = t.Qty, 
                            s.BVReceiptCost = t.Cost, 
                            s.BVReceiptDate = t.Date 
                        FROM SalesActivationsDetail s
                        INNER JOIN #TempAccUpdates t 
                            ON s.Invoice = t.Invoice AND s.WHSE = t.WHSE AND s.CODE = t.CODE
                        WHERE s.BVReceiptNo IS NULL";
                    
                    using (var cmd = new SqlCommand(updateSql, sqlConn))
                    {
                        cmd.CommandTimeout = 600;
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in UpdateACCcostsAsync: " + ex.Message, ex);
            }
        }

        private async Task BuildAcquisitionDetailAsync(int userId)
        {
            using (var sqlConn = new SqlConnection(_sqlConnStr))
            {
                await sqlConn.OpenAsync();

                // Fetch inventory MISC_1 data from postgres into a temp table for joining
                var invDt = new System.Data.DataTable();
                using (var pgConn = new NpgsqlConnection(_pgConnStr))
                {
                    await pgConn.OpenAsync();
                    string pgSql = "SELECT part_no, whse, misc_1 FROM inventory";
                    using (var pgCmd = new NpgsqlCommand(pgSql, pgConn))
                    {
                        pgCmd.CommandTimeout = 600;
                        using (var pgReader = await pgCmd.ExecuteReaderAsync())
                        {
                            invDt.Load(pgReader);
                        }
                    }
                }

                string createTemp = @"
                    CREATE TABLE #TempInventory (
                        part_no VARCHAR(255) COLLATE DATABASE_DEFAULT,
                        whse VARCHAR(255) COLLATE DATABASE_DEFAULT,
                        misc_1 VARCHAR(MAX) COLLATE DATABASE_DEFAULT
                    )";
                using (var cmd = new SqlCommand(createTemp, sqlConn)) await cmd.ExecuteNonQueryAsync();

                using (var bulkCopy = new SqlBulkCopy(sqlConn))
                {
                    bulkCopy.BulkCopyTimeout = 600;
                    bulkCopy.DestinationTableName = "#TempInventory";
                    await bulkCopy.WriteToServerAsync(invDt);
                }

                // Select from OutputData joined with detail tables
                var detailRecords = new List<dynamic>();
                string sqlText = @"
                    SELECT 
                        o.ChannelName, o.PaymentMethod, o.Type, o.Invoice, o.BVLineNo, sad.RecNoDetail, 
                        o.InvoiceDate, o.OrderDate, o.CustName, o.CustTerritory, o.DealerCode, o.CellPhoneNo, 
                        o.WebOrderID, o.Type2, o.FreeAccessory, o.TopUpSDFAccCostAdjusted, o.AccessoryCost, 
                        o.AccessoryPrice, o.AccCurrentCostTotal, o.AccSRPTotal, o.AccSRP17Total, o.AccSRP25Total, 
                        o.AccSRP50Total, o.AccSellingPriceTotal, sad.CODE, sad.Description, sad.ProdCode, 
                        sad.Qty, sad.Cost, sad.Price, sad.TopUp, sad.TopUpEdit, pi.misc_1 AS MISC_1, 
                        sad.CustPayAmount, sad.WebSRP, sad.WebCost, t1.RATE AS Rate1, t2.RATE AS Rate2, 
                        sad.FeeAcc, sad.AccPayback, o.M2MOrderID, o.ReturnClassification, sad.Tax1Flag, 
                        sad.Tax2Flag, o.AdjustmentType, sad.BVReceiptNo, sad.BVReceiptNoInt,
                        o.MSD, o.UserName, o.VoicePlan, o.DataPlan, o.DeviceOfferTypeID, 
                        o.ShipToProvince, o.OriginalInvoice, sad.AccessoryType, o.AccountNumber, 
                        o.RogersHWMarginShare, o.AgentName, o.AgentEmail, o.AgentContactNumber, o.RDType, sad.WHSE
                    FROM tblRogersInvoiceOutputData o
                    LEFT JOIN SalesActivationsDetail sad ON o.Invoice = sad.Invoice AND o.BVLineNo = sad.RECNO
                    LEFT JOIN tblRogersInvoiceSalesTaxes t1 ON o.TaxCode1 = t1.S_TAX_NO
                    LEFT JOIN tblRogersInvoiceSalesTaxes t2 ON o.TaxCode2 = t2.S_TAX_NO
                    LEFT JOIN #TempInventory pi ON sad.CODE = pi.part_no AND sad.WHSE = pi.whse
                    WHERE o.UserId = @UserId
                    ORDER BY o.Invoice, o.BVLineNo, sad.RecNoDetail";

                using (var cmd = new SqlCommand(sqlText, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string territory = reader.IsDBNull(9) ? "" : reader.GetString(9);
                            string partNo = reader.IsDBNull(24) ? "" : reader.GetString(24);
                            double custPay = reader.IsDBNull(33) ? 0 : Convert.ToDouble(reader.GetValue(33));
                            double qtyVal = reader.IsDBNull(27) ? 0 : Convert.ToDouble(reader.GetValue(27));
                            double bvPrice = reader.IsDBNull(29) ? 0 : Convert.ToDouble(reader.GetValue(29));
                            double srpVal = reader.IsDBNull(34) ? 0 : Convert.ToDouble(reader.GetValue(34));
                            string channel = reader.IsDBNull(0) ? "" : reader.GetString(0);
                            string accGroup = reader.IsDBNull(32) ? "" : reader.GetString(32);
                            double costVal = reader.IsDBNull(35) ? 0 : Convert.ToDouble(reader.GetValue(35));
                            string typeCol = reader.IsDBNull(2) ? "" : reader.GetString(2);

                            double qtyMod = (typeCol.StartsWith("Return") && qtyVal > 0) ? qtyVal * -1 : qtyVal;
                            double bvCostExt = Math.Round(qtyVal, 0) * Math.Round(reader.IsDBNull(28) ? 0 : Convert.ToDouble(reader.GetValue(28)), 2);
                            double bvPriceExt = Math.Round(qtyVal, 0) * Math.Round(bvPrice, 2);

                            double topupAcc = reader.IsDBNull(15) ? 0 : Convert.ToDouble(reader.GetValue(15));
                            
                            double topUp = reader.IsDBNull(30) ? 0 : Convert.ToDouble(reader.GetValue(30));
                            double topUpEdit = reader.IsDBNull(31) ? 0 : Convert.ToDouble(reader.GetValue(31));
                            double topUpAmt = (reader.IsDBNull(31) ? topUp : topUpEdit) * -1;
                            double topUpOrig = topUp * -1;

                            string topUpModify = Math.Round(topUpAmt, 2) != Math.Round(topUpOrig, 2) ? "*" : "";

                            double custPayExt = Math.Round(qtyVal, 0) * Math.Round(custPay, 2);

                            double topUpRecalc = AccTopUpCalculation(territory, partNo, custPay, qtyVal, bvPrice, srpVal, channel, accGroup, costVal);

                            detailRecords.Add(new {
                                ChannelName = channel,
                                PaymentMethod = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                Type = typeCol,
                                Invoice = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                BVLineNo = reader.IsDBNull(4) ? 0 : Convert.ToInt32(reader.GetValue(4)),
                                RecNoDetail = reader.IsDBNull(5) ? 0 : Convert.ToInt32(reader.GetValue(5)),
                                InvoiceDate = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6),
                                OrderDate = reader.IsDBNull(7) ? (DateTime?)null : reader.GetDateTime(7),
                                CustName = reader.IsDBNull(8) ? "" : reader.GetString(8),
                                CustTerritory = territory,
                                DealerCode = reader.IsDBNull(10) ? "" : reader.GetString(10),
                                CellPhoneNo = reader.IsDBNull(11) ? "" : reader.GetString(11),
                                WebOrderID = reader.IsDBNull(12) ? "" : reader.GetString(12),
                                Type2 = reader.IsDBNull(13) ? "" : reader.GetString(13),
                                FreeAccessory = reader.IsDBNull(14) ? "" : reader.GetString(14),
                                TopupAcc = topupAcc,
                                AccessoryCost = reader.IsDBNull(16) ? 0.0 : Convert.ToDouble(reader.GetValue(16)),
                                AccessoryPrice = reader.IsDBNull(17) ? 0.0 : Convert.ToDouble(reader.GetValue(17)),
                                AccCurrentCostTotal = reader.IsDBNull(18) ? 0.0 : Convert.ToDouble(reader.GetValue(18)),
                                AccSRPTotal = reader.IsDBNull(19) ? 0.0 : Convert.ToDouble(reader.GetValue(19)),
                                AccSRP17Total = reader.IsDBNull(20) ? 0.0 : Convert.ToDouble(reader.GetValue(20)),
                                AccSRP25Total = reader.IsDBNull(21) ? 0.0 : Convert.ToDouble(reader.GetValue(21)),
                                AccSRP50Total = reader.IsDBNull(22) ? 0.0 : Convert.ToDouble(reader.GetValue(22)),
                                ACCSellingPrice = reader.IsDBNull(23) ? 0.0 : Convert.ToDouble(reader.GetValue(23)),
                                CODE = partNo,
                                Description = reader.IsDBNull(25) ? "" : reader.GetString(25),
                                ProdCode = reader.IsDBNull(26) ? "" : reader.GetString(26),
                                Qty = qtyMod,
                                BVCost = reader.IsDBNull(28) ? 0.0 : Convert.ToDouble(reader.GetValue(28)),
                                BVCostExt = bvCostExt,
                                BVPrice = bvPrice,
                                BVPriceExt = bvPriceExt,
                                TopUpAmt = topUpAmt,
                                TopUpOrig = topUpOrig,
                                TopUpModify = topUpModify,
                                CustPayExt = custPayExt,
                                AccGroup = accGroup,
                                Margin = custPay != 0 ? "Yes" : "No",
                                AccSRP = qtyVal * srpVal,
                                AccSRP17 = Math.Round((qtyVal * srpVal) - (0.17 * (qtyVal * srpVal)), 2),
                                AccSRP25 = Math.Round((qtyVal * srpVal) - (0.25 * (qtyVal * srpVal)), 2),
                                AccSRP50 = Math.Round((qtyVal * srpVal) - (0.5 * (qtyVal * srpVal)), 2),
                                TopUpRecalc = topUpRecalc,
                                MSD = reader.IsDBNull(47) ? "" : reader.GetValue(47).ToString() ?? "",
                                UserName = reader.IsDBNull(48) ? "" : reader.GetValue(48).ToString() ?? "",
                                VoicePlan = reader.IsDBNull(49) ? "" : reader.GetValue(49).ToString() ?? "",
                                DataPlan = reader.IsDBNull(50) ? "" : reader.GetValue(50).ToString() ?? "",
                                GSTRate = reader.IsDBNull(36) ? 0.0 : Convert.ToDouble(reader.GetValue(36)),
                                PSTRate = reader.IsDBNull(37) ? 0.0 : Convert.ToDouble(reader.GetValue(37)),
                                Fee = reader.IsDBNull(38) ? 0.0 : Convert.ToDouble(reader.GetValue(38)),
                                FeePayback = reader.IsDBNull(39) ? 0.0 : Convert.ToDouble(reader.GetValue(39)),
                                M2MOrderID = reader.IsDBNull(40) ? "" : reader.GetString(40),
                                ReturnClassification = reader.IsDBNull(41) ? "" : reader.GetString(41),
                                GSTFlag = reader.IsDBNull(42) ? "" : reader.GetString(42),
                                PSTFlag = reader.IsDBNull(43) ? "" : reader.GetString(43),
                                AdjustmentType = reader.IsDBNull(44) ? "" : reader.GetString(44),
                                BVReceiptNo = reader.IsDBNull(45) ? "" : reader.GetString(45),
                                BVReceiptNoInt = reader.IsDBNull(46) ? 0 : Convert.ToInt32(reader.GetValue(46)),
                                DeviceOfferTypeID = reader.IsDBNull(51) ? 0 : Convert.ToInt32(reader.GetValue(51)),
                                ShipToProvince = reader.IsDBNull(52) ? "" : reader.GetString(52),
                                OriginalInvoice = reader.IsDBNull(53) ? "" : reader.GetString(53),
                                AccessoryType = reader.IsDBNull(54) ? "" : reader.GetString(54),
                                AccountNumber = reader.IsDBNull(55) ? "" : reader.GetString(55),
                                RogersACCMarginShare = (reader.IsDBNull(56) ? 0m : Convert.ToDecimal(reader.GetValue(56))) * (decimal)qtyVal,
                                AgentName = reader.IsDBNull(57) ? "" : reader.GetString(57),
                                AgentEmail = reader.IsDBNull(58) ? "" : reader.GetString(58),
                                AgentContactNumber = reader.IsDBNull(59) ? "" : reader.GetString(59),
                                RDType = reader.IsDBNull(60) ? "" : reader.GetString(60),
                                Whse = reader.IsDBNull(61) ? "" : reader.GetString(61)
                            });
                        }
                    }
                }

                // Insert into tblRogersInvoiceAcquisitionDetail
                using (var bulkCmd = sqlConn.CreateCommand())
                {
                    bulkCmd.CommandTimeout = 600;
                    foreach (var d in detailRecords)
                    {
                        bulkCmd.CommandText = @"
                            INSERT INTO tblRogersInvoiceAcquisitionDetail (
                                UserId, ChannelName, PaymentMethod, Type, Invoice, BVLineNo, RecNoDetail, InvoiceDate, 
                                OrderDate, CustName, CustTerritory, DealerCode, CellPhoneNo, WebOrderID, Type2, 
                                FreeAccessory, [Topup Acc], AccessoryCost, AccessoryPrice, AccCurrentCostTotal, 
                                AccSRPTotal, AccSRP17Total, AccSRP25Total, AccSRP50Total, ACCSellingPrice, 
                                CODE, Description, ProdCode, Qty, BVCost, BVCostExt, BVPrice, BVPriceExt, 
                                TopUpAmt, TopUpOrig, TopUpModify, CustPayExt, AccGroup, Margin, AccSRP, AccSRP17, 
                                AccSRP25, AccSRP50, TopUpRecalc, MSD, UserName, VoicePlan, DataPlan, GSTRate, PSTRate, 
                                Fee, FeePayback, M2MOrderID, ReturnClassification, GSTFlag, PSTFlag, AdjustmentType, 
                                BVReceiptNo, BVReceiptNoInt, DeviceOfferTypeID, ShipToProvince, OriginalInvoice, 
                                AccessoryType, AccountNumber, RogersACCMarginShare, AgentName, AgentEmail, AgentContactNumber, RDType,
                                WHSE
                            ) VALUES (
                                @UserId, @ChannelName, @PaymentMethod, @Type, @Invoice, @BVLineNo, @RecNoDetail, @InvoiceDate, 
                                @OrderDate, @CustName, @CustTerritory, @DealerCode, @CellPhoneNo, @WebOrderID, @Type2, 
                                @FreeAccessory, @TopupAcc, @AccessoryCost, @AccessoryPrice, @AccCurrentCostTotal, 
                                @AccSRPTotal, @AccSRP17Total, @AccSRP25Total, @AccSRP50Total, @ACCSellingPrice, 
                                @CODE, @Description, @ProdCode, @Qty, @BVCost, @BVCostExt, @BVPrice, @BVPriceExt, 
                                @TopUpAmt, @TopUpOrig, @TopUpModify, @CustPayExt, @AccGroup, @Margin, @AccSRP, @AccSRP17, 
                                @AccSRP25, @AccSRP50, @TopUpRecalc, @MSD, @UserName, @VoicePlan, @DataPlan, @GSTRate, @PSTRate, 
                                @Fee, @FeePayback, @M2MOrderID, @ReturnClassification, @GSTFlag, @PSTFlag, @AdjustmentType, 
                                @BVReceiptNo, @BVReceiptNoInt, @DeviceOfferTypeID, @ShipToProvince, @OriginalInvoice, 
                                @AccessoryType, @AccountNumber, @RogersACCMarginShare, @AgentName, @AgentEmail, @AgentContactNumber, @RDType,
                                @WHSE
                            )";
                        
                        bulkCmd.Parameters.Clear();
                        bulkCmd.Parameters.AddWithValue("@UserId", userId);
                        bulkCmd.Parameters.AddWithValue("@ChannelName", (object)d.ChannelName ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@PaymentMethod", (object)d.PaymentMethod ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@Type", (object)d.Type ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@Invoice", (object)d.Invoice ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@BVLineNo", d.BVLineNo);
                        bulkCmd.Parameters.AddWithValue("@RecNoDetail", d.RecNoDetail);
                        bulkCmd.Parameters.AddWithValue("@InvoiceDate", (object)d.InvoiceDate ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@OrderDate", (object)d.OrderDate ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@CustName", (object)d.CustName ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@CustTerritory", (object)d.CustTerritory ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@DealerCode", (object)d.DealerCode ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@CellPhoneNo", (object)d.CellPhoneNo ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@WebOrderID", (object)d.WebOrderID ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@Type2", (object)d.Type2 ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@FreeAccessory", (object)d.FreeAccessory ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@TopupAcc", d.TopupAcc);
                        bulkCmd.Parameters.AddWithValue("@AccessoryCost", d.AccessoryCost);
                        bulkCmd.Parameters.AddWithValue("@AccessoryPrice", d.AccessoryPrice);
                        bulkCmd.Parameters.AddWithValue("@AccCurrentCostTotal", d.AccCurrentCostTotal);
                        bulkCmd.Parameters.AddWithValue("@AccSRPTotal", d.AccSRPTotal);
                        bulkCmd.Parameters.AddWithValue("@AccSRP17Total", d.AccSRP17Total);
                        bulkCmd.Parameters.AddWithValue("@AccSRP25Total", d.AccSRP25Total);
                        bulkCmd.Parameters.AddWithValue("@AccSRP50Total", d.AccSRP50Total);
                        bulkCmd.Parameters.AddWithValue("@ACCSellingPrice", d.ACCSellingPrice);
                        bulkCmd.Parameters.AddWithValue("@CODE", (object)d.CODE ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@Description", (object)d.Description ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@ProdCode", (object)d.ProdCode ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@Qty", d.Qty);
                        bulkCmd.Parameters.AddWithValue("@BVCost", d.BVCost);
                        bulkCmd.Parameters.AddWithValue("@BVCostExt", d.BVCostExt);
                        bulkCmd.Parameters.AddWithValue("@BVPrice", d.BVPrice);
                        bulkCmd.Parameters.AddWithValue("@BVPriceExt", d.BVPriceExt);
                        bulkCmd.Parameters.AddWithValue("@TopUpAmt", d.TopUpAmt);
                        bulkCmd.Parameters.AddWithValue("@TopUpOrig", d.TopUpOrig);
                        bulkCmd.Parameters.AddWithValue("@TopUpModify", (object)d.TopUpModify ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@CustPayExt", d.CustPayExt);
                        bulkCmd.Parameters.AddWithValue("@AccGroup", (object)d.AccGroup ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@Margin", (object)d.Margin ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@AccSRP", d.AccSRP);
                        bulkCmd.Parameters.AddWithValue("@AccSRP17", d.AccSRP17);
                        bulkCmd.Parameters.AddWithValue("@AccSRP25", d.AccSRP25);
                        bulkCmd.Parameters.AddWithValue("@AccSRP50", d.AccSRP50);
                        bulkCmd.Parameters.AddWithValue("@TopUpRecalc", d.TopUpRecalc);
                        bulkCmd.Parameters.AddWithValue("@MSD", (object)d.MSD ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@UserName", (object)d.UserName ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@VoicePlan", (object)d.VoicePlan ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@DataPlan", (object)d.DataPlan ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@GSTRate", d.GSTRate);
                        bulkCmd.Parameters.AddWithValue("@PSTRate", d.PSTRate);
                        bulkCmd.Parameters.AddWithValue("@Fee", d.Fee);
                        bulkCmd.Parameters.AddWithValue("@FeePayback", d.FeePayback);
                        bulkCmd.Parameters.AddWithValue("@M2MOrderID", (object)d.M2MOrderID ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@ReturnClassification", (object)d.ReturnClassification ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@GSTFlag", (object)d.GSTFlag ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@PSTFlag", (object)d.PSTFlag ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@AdjustmentType", (object)d.AdjustmentType ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@BVReceiptNo", (object)d.BVReceiptNo ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@BVReceiptNoInt", d.BVReceiptNoInt);
                        bulkCmd.Parameters.AddWithValue("@DeviceOfferTypeID", d.DeviceOfferTypeID);
                        bulkCmd.Parameters.AddWithValue("@ShipToProvince", (object)d.ShipToProvince ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@OriginalInvoice", (object)d.OriginalInvoice ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@AccessoryType", (object)d.AccessoryType ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@AccountNumber", (object)d.AccountNumber ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@RogersACCMarginShare", d.RogersACCMarginShare);
                        bulkCmd.Parameters.AddWithValue("@AgentName", (object)d.AgentName ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@AgentEmail", (object)d.AgentEmail ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@AgentContactNumber", (object)d.AgentContactNumber ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@RDType", (object)d.RDType ?? DBNull.Value);
                        bulkCmd.Parameters.AddWithValue("@WHSE", (object)d.Whse ?? DBNull.Value);

                        await bulkCmd.ExecuteNonQueryAsync();
                    }
                }

                // Update detail unit cost from tblImportCapture
                string updateDetailCost = @"
                    UPDATE ad
                    SET ad.RDAccUnitCost = ic.AccessoryDealerCostPerUnit
                    FROM tblRogersInvoiceAcquisitionDetail ad
                    INNER JOIN tblImportCapture ic ON ad.CODE = ic.ACCPartNo AND ad.WebOrderID = ic.WebOrderID
                    WHERE ad.UserId = @UserId";
                using (var cmd = new SqlCommand(updateDetailCost, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task BuildAcquisitionARAsync(int userId)
        {
            using (var sqlConn = new SqlConnection(_sqlConnStr))
            {
                await sqlConn.OpenAsync();

                // Select from OutputData joined with tblSalesTaxes & Spire Postgres sales_history
                var arRecords = new List<dynamic>();
                string selectSql = @"
                    SELECT 
                        o.ChannelName, o.PaymentMethod, o.Type, o.Invoice, o.InvoiceDate, o.OrderDate, o.CustName, 
                        o.CustTerritory, o.DealerCode, o.MSD, o.UserName, o.CellPhoneNo, o.PortedCTN, o.VoicePlan, 
                        o.DataPlan, o.WebOrderID, o.Type2, o.Qty, o.PartNumber, o.FreeAccessoryPart, o.IMEIESN, 
                        o.CostPrice, o.SellPrice, o.TopUpOwing, o.TopUpSDFAccCostAdjusted, o.TopUpTotal, 
                        o.AccessoryPrice, o.Fee, o.FeeCount, o.HDWSRP, o.SCOA, o.ShipToProvince,
                        t1.NAME AS Tax1Name, t2.NAME AS Tax2Name,
                        o.InvoiceTaxes * 0.5 AS TempTax1, -- fallback if sales_history is missing
                        o.InvoiceTaxes * 0.5 AS TempTax2,
                        sad.WHSE
                    FROM tblRogersInvoiceOutputData o
                    LEFT JOIN SalesActivationsDetail sad ON o.Invoice = sad.Invoice AND o.BVLineNo = sad.RECNO
                    LEFT JOIN tblRogersInvoiceSalesTaxes t1 ON o.TaxCode1 = t1.S_TAX_NO
                    LEFT JOIN tblRogersInvoiceSalesTaxes t2 ON o.TaxCode2 = t2.S_TAX_NO
                    WHERE o.UserId = @UserId";

                using (var cmd = new SqlCommand(selectSql, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            arRecords.Add(new {
                                ChannelName = reader.IsDBNull(0) ? "" : reader.GetString(0),
                                PaymentMethod = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                Type = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                Invoice = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                InvoiceDate = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4),
                                OrderDate = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
                                CustName = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                CustTerritory = reader.IsDBNull(7) ? "" : reader.GetString(7),
                                DealerCode = reader.IsDBNull(8) ? "" : reader.GetString(8),
                                MSD = reader.IsDBNull(9) ? "" : reader.GetString(9),
                                UserName = reader.IsDBNull(10) ? "" : reader.GetString(10),
                                CellPhoneNo = reader.IsDBNull(11) ? "" : reader.GetString(11),
                                PortedCTN = reader.IsDBNull(12) ? "" : reader.GetString(12),
                                VoicePlan = reader.IsDBNull(13) ? "" : reader.GetString(13),
                                DataPlan = reader.IsDBNull(14) ? "" : reader.GetString(14),
                                WebOrderID = reader.IsDBNull(15) ? "" : reader.GetString(15),
                                Type2 = reader.IsDBNull(16) ? "" : reader.GetString(16),
                                Qty = reader.IsDBNull(17) ? 0.0 : Convert.ToDouble(reader.GetValue(17)),
                                PartNumber = reader.IsDBNull(18) ? "" : reader.GetString(18),
                                FreeAccessory = reader.IsDBNull(19) ? "" : reader.GetString(19),
                                IMEIESN = reader.IsDBNull(20) ? "" : reader.GetString(20),
                                CostPrice = reader.IsDBNull(21) ? 0.0 : Convert.ToDouble(reader.GetValue(21)),
                                SellPrice = reader.IsDBNull(22) ? 0.0 : Convert.ToDouble(reader.GetValue(22)),
                                TopUpOwing = reader.IsDBNull(23) ? 0.0 : Convert.ToDouble(reader.GetValue(23)),
                                TopupAcc = reader.IsDBNull(24) ? 0.0 : Convert.ToDouble(reader.GetValue(24)),
                                TopUpTotal = reader.IsDBNull(25) ? 0.0 : Convert.ToDouble(reader.GetValue(25)),
                                AccessoryPrice = reader.IsDBNull(26) ? 0.0 : Convert.ToDouble(reader.GetValue(26)),
                                Fee = reader.IsDBNull(27) ? 0.0 : Convert.ToDouble(reader.GetValue(27)),
                                FeeCount = reader.IsDBNull(28) ? 0 : Convert.ToInt32(Convert.ToDouble(reader.GetValue(28))),
                                HDWChargeToCustomer = reader.IsDBNull(29) ? 0.0 : Convert.ToDouble(reader.GetValue(29)),
                                SCOA = reader.IsDBNull(30) ? "" : reader.GetString(30),
                                ShipToProvince = reader.IsDBNull(31) ? "" : reader.GetString(31),
                                Tax1Name = reader.IsDBNull(32) ? "" : reader.GetString(32),
                                Tax2Name = reader.IsDBNull(33) ? "" : reader.GetString(33),
                                TempTax1 = reader.IsDBNull(34) ? 0.0 : Convert.ToDouble(reader.GetValue(34)),
                                TempTax2 = reader.IsDBNull(35) ? 0.0 : Convert.ToDouble(reader.GetValue(35)),
                                GST = 0.0,
                                PST = 0.0,
                                HST = 0.0,
                                QST = 0.0,
                                ARAmount = 0.0,
                                Whse = reader.IsDBNull(36) ? "" : reader.GetString(36)
                            });
                        }
                    }
                }

                // Query totals from Postgres sales_history and calculate tax components
                if (arRecords.Any())
                {
                    var invoices = arRecords.Select(r => (string)r.Invoice).Distinct().ToList();
                    var totals = new Dictionary<string, (decimal total, decimal tax1, decimal tax2)>();

                    using (var pgConn = new NpgsqlConnection(_pgConnStr))
                    {
                        await pgConn.OpenAsync();

                        string pgSql = "SELECT invoice_no, total, sales_tax_total[1], sales_tax_total[2] FROM sales_history WHERE invoice_no = ANY(@Invoices)";
                        using (var cmd = new NpgsqlCommand(pgSql, pgConn))
                        {
                            cmd.Parameters.AddWithValue("Invoices", invoices);
                            cmd.CommandTimeout = 600;

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    totals[reader.GetString(0)] = (
                                        Convert.ToDecimal(reader.GetValue(1)),
                                        reader.IsDBNull(2) ? 0m : Convert.ToDecimal(reader.GetValue(2)),
                                        reader.IsDBNull(3) ? 0m : Convert.ToDecimal(reader.GetValue(3))
                                    );
                                }
                            }
                        }
                    }

                    using (var bulkCmd = sqlConn.CreateCommand())
                    {
                        bulkCmd.CommandTimeout = 600;

                        foreach (var r in arRecords)
                        {
                            double gst = 0, pst = 0, hst = 0, qst = 0;
                            double arAmount = 0;

                            if (totals.TryGetValue(r.Invoice, out (decimal total, decimal tax1, decimal tax2) t))
                            {
                                arAmount = (double)t.total;
                                double t1 = (double)t.tax1;
                                double t2 = (double)t.tax2;

                                if (r.Tax1Name.StartsWith("GST")) gst = t1;
                                else if (r.Tax1Name.StartsWith("HST")) hst = t1;

                                if (r.Tax2Name.StartsWith("PST")) pst = t2;
                                else if (r.Tax2Name.StartsWith("QST")) qst = t2;
                            }
                            else
                            {
                                // Fallback
                                arAmount = r.SellPrice; // default fallback
                                if (r.Tax1Name.StartsWith("GST")) gst = r.TempTax1;
                                else if (r.Tax1Name.StartsWith("HST")) hst = r.TempTax1;

                                if (r.Tax2Name.StartsWith("PST")) pst = r.TempTax2;
                                else if (r.Tax2Name.StartsWith("QST")) qst = r.TempTax2;
                            }

                            bulkCmd.CommandText = @"
                                INSERT INTO tblRogersInvoiceAcquisitionAR (
                                    UserId, ChannelName, PaymentMethod, Type, Invoice, InvoiceDate, OrderDate, CustName, 
                                    CustTerritory, DealerCode, MSD, UserName, CellPhoneNo, PortedCTN, VoicePlan, 
                                    DataPlan, WebOrderID, Type2, Qty, PartNumber, FreeAccessory, IMEIESN, CostPrice, 
                                    SellPrice, TopUpOwing, [Topup Acc], TopUpTotal, AccessoryPrice, Fee, FeeCount, 
                                    GST, PST, HST, QST, ARAmount, HDWChargeToCustomer, [True HDW TopUp], SCOA, ShipToProvince,
                                    WHSE
                                ) VALUES (
                                    @UserId, @ChannelName, @PaymentMethod, @Type, @Invoice, @InvoiceDate, @OrderDate, @CustName, 
                                    @CustTerritory, @DealerCode, @MSD, @UserName, @CellPhoneNo, @PortedCTN, @VoicePlan, 
                                    @DataPlan, @WebOrderID, @Type2, @Qty, @PartNumber, @FreeAccessory, @IMEIESN, @CostPrice, 
                                    @SellPrice, @TopUpOwing, @TopupAcc, @TopUpTotal, @AccessoryPrice, @Fee, @FeeCount, 
                                    @GST, @PST, @HST, @QST, @ARAmount, @HDWChargeToCustomer, @TrueHDWTopUp, @SCOA, @ShipToProvince,
                                    @WHSE
                                )";

                            bulkCmd.Parameters.Clear();
                            bulkCmd.Parameters.AddWithValue("@UserId", userId);
                            bulkCmd.Parameters.AddWithValue("@ChannelName", (object)r.ChannelName ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@PaymentMethod", (object)r.PaymentMethod ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@Type", (object)r.Type ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@Invoice", (object)r.Invoice ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@InvoiceDate", (object)r.InvoiceDate ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@OrderDate", (object)r.OrderDate ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@CustName", (object)r.CustName ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@CustTerritory", (object)r.CustTerritory ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@DealerCode", (object)r.DealerCode ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@MSD", (object)r.MSD ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@UserName", (object)r.UserName ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@CellPhoneNo", (object)r.CellPhoneNo ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@PortedCTN", (object)r.PortedCTN ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@VoicePlan", (object)r.VoicePlan ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@DataPlan", (object)r.DataPlan ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@WebOrderID", (object)r.WebOrderID ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@Type2", (object)r.Type2 ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@Qty", r.Qty);
                            bulkCmd.Parameters.AddWithValue("@PartNumber", (object)r.PartNumber ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@FreeAccessory", (object)r.FreeAccessory ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@IMEIESN", (object)r.IMEIESN ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@CostPrice", r.CostPrice);
                            bulkCmd.Parameters.AddWithValue("@SellPrice", r.SellPrice);
                            bulkCmd.Parameters.AddWithValue("@TopUpOwing", r.TopUpOwing);
                            bulkCmd.Parameters.AddWithValue("@TopupAcc", r.TopupAcc);
                            bulkCmd.Parameters.AddWithValue("@TopUpTotal", r.TopUpTotal);
                            bulkCmd.Parameters.AddWithValue("@AccessoryPrice", r.AccessoryPrice);
                            bulkCmd.Parameters.AddWithValue("@Fee", r.Fee);
                            bulkCmd.Parameters.AddWithValue("@FeeCount", r.FeeCount);
                            bulkCmd.Parameters.AddWithValue("@GST", gst);
                            bulkCmd.Parameters.AddWithValue("@PST", pst);
                            bulkCmd.Parameters.AddWithValue("@HST", hst);
                            bulkCmd.Parameters.AddWithValue("@QST", qst);
                            bulkCmd.Parameters.AddWithValue("@ARAmount", arAmount);
                            bulkCmd.Parameters.AddWithValue("@HDWChargeToCustomer", r.HDWChargeToCustomer);
                            bulkCmd.Parameters.AddWithValue("@TrueHDWTopUp", "");
                            bulkCmd.Parameters.AddWithValue("@SCOA", (object)r.SCOA ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@ShipToProvince", (object)r.ShipToProvince ?? DBNull.Value);
                            bulkCmd.Parameters.AddWithValue("@WHSE", (object)r.Whse ?? DBNull.Value);

                            await bulkCmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
        }

        private async Task AssembleAcquisitionOutputAsync(int userId)
        {
            using (var sqlConn = new SqlConnection(_sqlConnStr))
            {
                await sqlConn.OpenAsync();

                // Select from tblRogersInvoiceAcquisitionAR (UNION of AR)
                string sqlAR = @"
                    INSERT INTO tblRogersInvoiceAcquisitionOutput (
                        UserId, ChannelName, PaymentMethod, Type, Invoice, InvoiceDate, OrderDate, CustName, 
                        CustTerritory, DealerCode, MSD, UserName, CellPhoneNo, PortedCTN, VoicePlan, DataPlan, 
                        WebOrderID, Type2, Qty, PartNumber, FreeAccessory, IMEIESN, CostPrice, SellPrice, 
                        TopUpOwing, [TopUp Acc], TopUpTotal, HDWMargin, AccessoryCost, AccessoryPrice, 
                        Fee, FeePayback, FeeCount, ARAmount, [RV-UEValue], 
                        HDWChargeToCustomer, [HDWRV-UEValue], [True HDW TopUp], ACCChargeToCx, 
                        AccMargin, [Group], AccSellingPrice, SalesBeforeTax, M2MOrderID, 
                        ReturnClassification, Comments, IMEIReceiveAppCost, NetPriceProtection, 
                        NetIMEIReceiveAppCost, AccSeq, BVReceiptNo, BVReceiptNoInt, DeviceOfferTypeID, 
                        ShipToProvince, GSTRate, PSTRate, OriginalInvoice, TaxFlag1, TaxFlag2, DealerHDWMargin, 
                        DealerACCMargin, RDAccUnitCost, RDAccExtendedCost, AccessoryType, BAN, AgentName, 
                        AgentEmail, AgentContactNumber, RogersHWMarginShare, RogersACCMarginShare, Term, RDType, PPOverpayment,
                        WHSE
                    )
                    SELECT 
                        @UserId, ChannelName, PaymentMethod, Type, Invoice, InvoiceDate, OrderDate, CustName, 
                        CustTerritory, DealerCode, MSD, UserName, CellPhoneNo, PortedCTN, VoicePlan, DataPlan, 
                        WebOrderID, Type2, Qty, PartNumber, FreeAccessory, IMEIESN, CostPrice, SellPrice, 
                        TopUpOwing, 0 AS TopUpOrig, TopUpOwing AS TopUpTotal, 
                        CASE WHEN ChannelName = 'RDDealer' AND (UPPER(Type2) LIKE '%HARDWARE%' OR UPPER(Type2) LIKE '%NO TERM%') THEN (HDWChargeToCustomer - CostPrice) ELSE 0 END AS HDWMargin,
                        0 AS BVCostExt, 0 AS BVPriceExt, Fee, 0 AS FeePayback, FeeCount, ARAmount, 0 AS [RV-UEValue],
                        HDWChargeToCustomer, 0 AS [HDWRV-UEValue], 0 AS [True HDW TopUp], 0 AS CustPayExt, 
                        0 AS AccMargin, 'HDW' AS AccGroup, 0 AS AccSellingPrice, 0 AS SalesBeforeTax, '' AS M2MOrderID, 
                        '' AS ReturnClassification, '' AS Comments, 0 AS IMEIReceiveAppCost, 0 AS NetPriceProtection, 
                        0 AS NetIMEIReceiveAppCost, 0 AS SortColumn, '' AS BVReceiptNo, 0 AS BVReceiptNoInt, 
                        0 AS DeviceOfferTypeID, ShipToProvince, 0 AS GSTRate, 0 AS PSTRate, '' AS OriginalInvoice, 
                        '' AS TaxFlag1, '' AS TaxFlag2, 
                        0 AS DealerHDWMargin,
                        0 AS DealerACCMargin, 0 AS RDAccUnitCost, 0 AS RDAccExtendedCost, '' AS AccessoryType, 
                        '' AS BAN, '' AS AgentName, '' AS AgentEmail, '' AS AgentContactNumber, 0 AS RogersHWMarginShare, 0 AS RogersACCMarginShare, 
                        '' AS Term, '' AS RDType, 0 AS PPOverpayment,
                        WHSE
                    FROM tblRogersInvoiceAcquisitionAR
                    WHERE UserId = @UserId AND Type <> 'Acc' AND Type2 <> 'Acc'";

                using (var cmd = new SqlCommand(sqlAR, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Select from tblRogersInvoiceAcquisitionDetail (UNION of Detail)
                string sqlDetail = @"
                    INSERT INTO tblRogersInvoiceAcquisitionOutput (
                        UserId, ChannelName, PaymentMethod, Type, Invoice, InvoiceDate, OrderDate, CustName, 
                        CustTerritory, DealerCode, MSD, UserName, CellPhoneNo, PortedCTN, VoicePlan, DataPlan, 
                        WebOrderID, Type2, Qty, PartNumber, FreeAccessory, IMEIESN, CostPrice, SellPrice, 
                        TopUpOwing, [TopUp Acc], TopUpTotal, HDWMargin, AccessoryCost, AccessoryPrice, 
                        Fee, FeePayback, FeeCount, ARAmount, [RV-UEValue], 
                        HDWChargeToCustomer, [HDWRV-UEValue], [True HDW TopUp], ACCChargeToCx, 
                        AccMargin, [Group], AccSellingPrice, SalesBeforeTax, M2MOrderID, 
                        ReturnClassification, Comments, IMEIReceiveAppCost, NetPriceProtection, 
                        NetIMEIReceiveAppCost, AccSeq, BVReceiptNo, BVReceiptNoInt, DeviceOfferTypeID, 
                        ShipToProvince, GSTRate, PSTRate, OriginalInvoice, TaxFlag1, TaxFlag2, DealerHDWMargin, 
                        DealerACCMargin, RDAccUnitCost, RDAccExtendedCost, AccessoryType, BAN, AgentName, 
                        AgentEmail, AgentContactNumber, RogersHWMarginShare, RogersACCMarginShare, Term, RDType, PPOverpayment,
                        WHSE
                    )
                    SELECT 
                        @UserId, ChannelName, PaymentMethod, Type, Invoice, InvoiceDate, OrderDate, CustName, 
                        CustTerritory, DealerCode, MSD, UserName, CellPhoneNo, '' AS PortedCTN, VoicePlan, DataPlan, 
                        WebOrderID, Type2, Qty, CODE, Description AS FreeAccessory, '' AS IMEIESN, 0 AS CostPrice, 
                        0 AS SellPrice, 0 AS TopUpOwing, [Topup Acc], [Topup Acc] AS TopUpTotal, 0 AS HdwMargin, 
                        BVCostExt, BVPriceExt, Fee, FeePayback, 0 AS FeeCount, 0 AS ARAmount, 0 AS [RV-UEValue], 
                        0 AS HDWChargeToCustomer, 0 AS [HDWRV-UEValue], 0 AS TrueHDWTopUp, CustPayExt, 
                        [CustPayExt] - [BVCostExt] AS AccMargin, CASE WHEN AccGroup = 'REGULAR' THEN 'ACC' ELSE AccGroup END AS AccGroupMod, 
                        CASE WHEN ChannelName = 'M2M' THEN CustPayExt ELSE 0 END AS AccSellingPrice, 0 AS SalesBeforeTax, 
                        M2MOrderID, ReturnClassification, AdjustmentType AS Comments, NULL AS IMEIReceiveAppCost, 
                        NULL AS NetPriceProtection, NULL AS NetIMEIReceiveAppCost, RecNoDetail, BVReceiptNo, 
                        BVReceiptNoInt, 0 AS DeviceOfferTypeID, ShipToProvince, GSTRate, PSTRate, OriginalInvoice, 
                        GSTFlag, PSTFlag, 0 AS DealerHDWMargin, 
                        CASE WHEN ChannelName = 'RDDealer' THEN ([CustPayExt] - [BVCostExt] - Fee) ELSE 0 END AS DealerACCMargin, 
                        0 AS RDAccUnitCost, 0 AS RDAccExtendedCost, 
                        AccessoryType, AccountNumber, AgentName, AgentEmail, AgentContactNumber, 0 AS RogersHWMarginShare, 
                        RogersACCMarginShare, NULL AS Term, RDType, 0 AS PPOverpayment,
                        WHSE
                    FROM tblRogersInvoiceAcquisitionDetail
                    WHERE UserId = @UserId AND RecNoDetail IS NOT NULL";

                using (var cmd = new SqlCommand(sqlDetail, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task BuildRecentReceiptsAsync(int userId)
        {
            using (var sqlConn = new SqlConnection(_sqlConnStr))
            {
                await sqlConn.OpenAsync();

                // Select distinct PartNumbers from tblRogersInvoiceAcquisitionOutput
                var parts = new List<string>();
                string getPartsSql = "SELECT DISTINCT PartNumber FROM tblRogersInvoiceAcquisitionOutput WHERE UserId = @UserId AND [Group] = 'ACC'";
                using (var cmd = new SqlCommand(getPartsSql, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            parts.Add(reader.GetString(0));
                        }
                    }
                }

                // Query and populate tblRogersInvoiceRecentReceipts
                foreach (var part in parts)
                {
                    var recs = new List<dynamic>();
                    string getRecsSql = @"
                        SELECT TOP 3 hr.BVReceiptDate, hr.Qty, ri.PerUnitAmount, ri.RefNo
                        FROM hardwarereceived hr
                        INNER JOIN tblRogersInvoice ri ON hr.BVReceiptNo = ri.BVReceiptNo
                        WHERE hr.ItemType = 'ACC' AND hr.Part = @Part
                        ORDER BY ri.TransDate DESC, hr.BVReceiptNo DESC";
                    
                    using (var cmd = new SqlCommand(getRecsSql, sqlConn))
                    {
                        cmd.CommandTimeout = 600;
                        cmd.Parameters.AddWithValue("@Part", part);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                recs.Add(new {
                                    Date = reader.IsDBNull(0) ? (DateTime?)null : reader.GetDateTime(0),
                                    Qty = reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetValue(1)),
                                    Cost = reader.IsDBNull(2) ? 0.0 : Convert.ToDouble(reader.GetValue(2)),
                                    Invoice = reader.IsDBNull(3) ? "" : reader.GetString(3)
                                });
                            }
                        }
                    }

                    if (recs.Any())
                    {
                        string insSql = @"
                            INSERT INTO tblRogersInvoiceRecentReceipts (
                                UserId, PartNumber, 
                                ReceiptDate1, Qty1, Cost1, Invoice1,
                                ReceiptDate2, Qty2, Cost2, Invoice2,
                                ReceiptDate3, Qty3, Cost3, Invoice3
                            ) VALUES (
                                @UserId, @PartNumber, 
                                @Date1, @Qty1, @Cost1, @Inv1,
                                @Date2, @Qty2, @Cost2, @Inv2,
                                @Date3, @Qty3, @Cost3, @Inv3
                            )";
                        
                        using (var cmd = new SqlCommand(insSql, sqlConn))
                        {
                            cmd.CommandTimeout = 600;
                            cmd.Parameters.AddWithValue("@UserId", userId);
                            cmd.Parameters.AddWithValue("@PartNumber", part);

                            for (int i = 0; i < 3; i++)
                            {
                                if (i < recs.Count)
                                {
                                    cmd.Parameters.AddWithValue($"@Date{i+1}", (object)recs[i].Date ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue($"@Qty{i+1}", recs[i].Qty);
                                    cmd.Parameters.AddWithValue($"@Cost{i+1}", recs[i].Cost);
                                    cmd.Parameters.AddWithValue($"@Inv{i+1}", recs[i].Invoice);
                                }
                                else
                                {
                                    cmd.Parameters.AddWithValue($"@Date{i+1}", DBNull.Value);
                                    cmd.Parameters.AddWithValue($"@Qty{i+1}", DBNull.Value);
                                    cmd.Parameters.AddWithValue($"@Cost{i+1}", DBNull.Value);
                                    cmd.Parameters.AddWithValue($"@Inv{i+1}", DBNull.Value);
                                }
                            }

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                // Update recent receipts in output
                string updateOutputRecs = @"
                    UPDATE ao
                    SET ao.ReceiptDate1 = rr.ReceiptDate1, ao.Qty1 = rr.Qty1, ao.Cost1 = rr.Cost1, ao.Invoice1 = rr.Invoice1,
                        ao.ReceiptDate2 = rr.ReceiptDate2, ao.Qty2 = rr.Qty2, ao.Cost2 = rr.Cost2, ao.Invoice2 = rr.Invoice2,
                        ao.ReceiptDate3 = rr.ReceiptDate3, ao.Qty3 = rr.Qty3, ao.Cost3 = rr.Cost3, ao.Invoice3 = rr.Invoice3
                    FROM tblRogersInvoiceAcquisitionOutput ao
                    INNER JOIN tblRogersInvoiceRecentReceipts rr ON ao.PartNumber = rr.PartNumber AND rr.UserId = @UserId
                    WHERE ao.UserId = @UserId";
                using (var cmd = new SqlCommand(updateOutputRecs, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Copy Rogers costs
                string updateRogersCost = @"
                    UPDATE ao
                    SET ao.RogersInvoiceCost = ri.PerUnitAmount
                    FROM tblRogersInvoiceAcquisitionOutput ao
                    INNER JOIN tblRogersInvoice ri ON ao.BVReceiptNo = ri.BVReceiptNo
                    WHERE ao.UserId = @UserId";
                using (var cmd = new SqlCommand(updateRogersCost, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Copy ACC receipts
                string updateAccReceipt = @"
                    UPDATE ao
                    SET ao.BVReceiptQty = r.QTY, ao.BVReceiptCostAcc = r.COST,
                        ao.BVReceiptDate = CONVERT(DATETIME, SUBSTRING(r.INVR_DATE, 1, 4) + '-' + SUBSTRING(r.INVR_DATE, 5, 2) + '-' + SUBSTRING(r.INVR_DATE, 7, 2) + ' 00:00:00')
                    FROM tblRogersInvoiceAcquisitionOutput ao
                    INNER JOIN tblACCReceipts r ON ao.BVReceiptNoInt = r.RECPT_KEY
                    WHERE ao.UserId = @UserId AND LEN(r.INVR_DATE) = 8";
                using (var cmd = new SqlCommand(updateAccReceipt, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Sequence accessories sequentially per invoice
                var invoices = new List<string>();
                string getInvs = "SELECT DISTINCT Invoice FROM tblRogersInvoiceAcquisitionOutput WHERE UserId = @UserId ORDER BY Invoice";
                using (var cmd = new SqlCommand(getInvs, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            invoices.Add(reader.GetString(0));
                        }
                    }
                }

                foreach (var inv in invoices)
                {
                    var items = new List<int>();
                    string getItems = "SELECT Id FROM tblRogersInvoiceAcquisitionOutput WHERE UserId = @UserId AND Invoice = @Inv AND [Group] = 'ACC' ORDER BY Id";
                    using (var cmd = new SqlCommand(getItems, sqlConn))
                    {
                        cmd.CommandTimeout = 600;
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@Inv", inv);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                items.Add(Convert.ToInt32(reader.GetValue(0)));
                            }
                        }
                    }

                    int seq = 1;
                    foreach (var id in items)
                    {
                        string updSeq = "UPDATE tblRogersInvoiceAcquisitionOutput SET AccSeq = @Seq WHERE Id = @Id";
                        using (var cmd = new SqlCommand(updSeq, sqlConn))
                        {
                            cmd.CommandTimeout = 600;
                            cmd.Parameters.AddWithValue("@Seq", seq++);
                            cmd.Parameters.AddWithValue("@Id", id);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
        }

        private async Task BuildUPSLostAsync(int userId)
        {
            using (var sqlConn = new SqlConnection(_sqlConnStr))
            {
                await sqlConn.OpenAsync();

                // Select from tblRogersInvoiceAcquisitionOutput where CustTerritory = 'HMS' and populate tblRogersInvoiceUPSLost
                string insertUps = @"
                    INSERT INTO tblRogersInvoiceUPSLost (
                        UserId, Invoice, InvoiceDate, OrderDate, OriginalInvoice, CustName, Territory, 
                        WebOrderID, CellPhoneNo, Qty, PartNumber, FreeAccessory, IMEIESN, CostPrice, 
                        AccessoryCost, TotalBeforeTaxes, HST, TotalClaim, Courier, Claim, [Group], 
                        OutstandingAR, NetIMEIReceiveCost, NetPriceProtection, NetCost
                    )
                    SELECT 
                        @UserId, Invoice, InvoiceDate, OrderDate, OriginalInvoice, CustName, CustTerritory, 
                        WebOrderID, CellPhoneNo, Qty, PartNumber, FreeAccessory, IMEIESN, CostPrice, 
                        AccessoryCost, SalesBeforeTax, [GST-HST] AS HST, ARAmount AS TotalClaim, 
                        '' AS Courier, '' AS Claim, [Group], 0 AS OutstandingAR, IMEIReceiveAppCost, 
                        NetPriceProtection, NetIMEIReceiveAppCost
                    FROM tblRogersInvoiceAcquisitionOutput
                    WHERE UserId = @UserId AND CustTerritory = 'HMS'";
                
                using (var cmd = new SqlCommand(insertUps, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Copy to tblRogersInvoiceUPSLostUSER
                string copyUpsUser = @"
                    INSERT INTO tblRogersInvoiceUPSLostUSER (
                        UserId, Invoice, InvoiceDate, OrderDate, OriginalInvoice, CustName, Territory, 
                        WebOrderID, CellPhoneNo, Qty, PartNumber, FreeAccessory, IMEIESN, CostPrice, 
                        AccessoryCost, TotalBeforeTaxes, HST, TotalClaim, Courier, Claim, [Group], 
                        OutstandingAR, NetIMEIReceiveCost, NetPriceProtection, NetCost
                    )
                    SELECT 
                        UserId, Invoice, InvoiceDate, OrderDate, OriginalInvoice, CustName, Territory, 
                        WebOrderID, CellPhoneNo, Qty, PartNumber, FreeAccessory, IMEIESN, CostPrice, 
                        AccessoryCost, TotalBeforeTaxes, HST, TotalClaim, Courier, Claim, [Group], 
                        OutstandingAR, NetIMEIReceiveCost, NetPriceProtection, NetCost
                    FROM tblRogersInvoiceUPSLost
                    WHERE UserId = @UserId";
                
                using (var cmd = new SqlCommand(copyUpsUser, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task ExecuteFinalUpdatesAsync(int userId)
        {
            using (var sqlConn = new SqlConnection(_sqlConnStr))
            {
                await sqlConn.OpenAsync();

                // 1. TestNewTaxes logic
                var records = new List<dynamic>();
                string getRecs = @"
                    SELECT 
                        Id, Invoice, OriginalInvoice, CustTerritory, PaymentMethod, [Group], 
                        CostPrice, SellPrice, AccessoryCost, AccessoryPrice, GSTRate, PSTRate, TaxFlag1, TaxFlag2
                    FROM tblRogersInvoiceAcquisitionOutput
                    WHERE UserId = @UserId
                    ORDER BY Invoice";
                
                using (var cmd = new SqlCommand(getRecs, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            records.Add(new {
                                Id = Convert.ToInt32(reader.GetValue(0)),
                                Invoice = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                OriginalInvoice = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                CustTerritory = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                PaymentMethod = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                Group = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                CostPrice = reader.IsDBNull(6) ? 0.0 : Convert.ToDouble(reader.GetValue(6)),
                                SellPrice = reader.IsDBNull(7) ? 0.0 : Convert.ToDouble(reader.GetValue(7)),
                                AccessoryCost = reader.IsDBNull(8) ? 0.0 : Convert.ToDouble(reader.GetValue(8)),
                                AccessoryPrice = reader.IsDBNull(9) ? 0.0 : Convert.ToDouble(reader.GetValue(9)),
                                GSTRate = reader.IsDBNull(10) ? 0.0 : Convert.ToDouble(reader.GetValue(10)),
                                PSTRate = reader.IsDBNull(11) ? 0.0 : Convert.ToDouble(reader.GetValue(11)),
                                TaxFlag1 = reader.IsDBNull(12) ? "" : reader.GetString(12),
                                TaxFlag2 = reader.IsDBNull(13) ? "" : reader.GetString(13)
                            });
                        }
                    }
                }

                foreach (var r in records)
                {
                    bool isNewTaxes = false;
                    if ((r.Invoice.CompareTo("0006356745") >= 0 && string.IsNullOrEmpty(r.OriginalInvoice)) ||
                        (r.Invoice.CompareTo("0006356745") >= 0 && r.OriginalInvoice.CompareTo("0006356745") > 0))
                    {
                        isNewTaxes = true;
                    }

                    string rOrD = DiscoverOrRogers(r.CustTerritory);
                    double topUpOwing = r.CostPrice - r.SellPrice;
                    if (topUpOwing < 0) topUpOwing = 0;

                    double topUpAcc = 0;
                    double topUpTotal = topUpOwing;
                    double arAmount = 0;
                    double salesBeforeTax = r.Group == "HDW" ? r.SellPrice : r.AccessoryPrice;
                    double gstHst = 0;
                    double pstQst = 0;
                    double sellPrice = r.SellPrice;
                    double accPrice = r.AccessoryPrice;

                    if (isNewTaxes)
                    {
                        if (rOrD == "Rogers")
                        {
                            if (r.PaymentMethod == "Invoice")
                            {
                                if (r.Group == "HDW")
                                {
                                    topUpOwing = r.CostPrice;
                                    topUpTotal = r.CostPrice;
                                    double gstRate = r.TaxFlag1 == "Y" ? r.GSTRate : 0;
                                    double pstRate = r.TaxFlag2 == "Y" ? r.PSTRate : 0;
                                    arAmount = Math.Round(r.SellPrice * gstRate / 100.0, 2) + Math.Round(r.SellPrice * pstRate / 100.0, 2);
                                    salesBeforeTax = r.SellPrice;
                                    gstHst = Math.Round(r.SellPrice * gstRate / 100.0, 2);
                                    pstQst = Math.Round(r.SellPrice * pstRate / 100.0, 2);
                                    sellPrice = 0;
                                }
                                else
                                {
                                    topUpAcc = r.AccessoryCost;
                                    topUpTotal = r.AccessoryCost;
                                    double gstRate = r.TaxFlag1 == "Y" ? r.GSTRate : 0;
                                    double pstRate = r.TaxFlag2 == "Y" ? r.PSTRate : 0;
                                    arAmount = Math.Round(r.AccessoryPrice * gstRate / 100.0, 2) + Math.Round(r.AccessoryPrice * pstRate / 100.0, 2);
                                    salesBeforeTax = r.AccessoryPrice;
                                    gstHst = Math.Round(r.AccessoryPrice * gstRate / 100.0, 2);
                                    pstQst = Math.Round(r.AccessoryPrice * pstRate / 100.0, 2);
                                    accPrice = 0;
                                }
                            }
                            else if (r.PaymentMethod == "CREDIT CARD")
                            {
                                if (r.Group == "HDW")
                                {
                                    topUpOwing = r.CostPrice;
                                    topUpTotal = r.CostPrice;
                                    arAmount = -1.0 * r.SellPrice;
                                    salesBeforeTax = r.SellPrice;
                                    double gstRate = r.TaxFlag1 == "Y" ? r.GSTRate : 0;
                                    double pstRate = r.TaxFlag2 == "Y" ? r.PSTRate : 0;
                                    gstHst = Math.Round(r.SellPrice * gstRate / 100.0, 2);
                                    pstQst = Math.Round(r.SellPrice * pstRate / 100.0, 2);
                                    sellPrice = 0;
                                }
                                else
                                {
                                    topUpAcc = r.AccessoryCost;
                                    topUpTotal = r.AccessoryCost;
                                    arAmount = -1.0 * r.AccessoryPrice;
                                    salesBeforeTax = r.AccessoryPrice;
                                    double gstRate = r.TaxFlag1 == "Y" ? r.GSTRate : 0;
                                    double pstRate = r.TaxFlag2 == "Y" ? r.PSTRate : 0;
                                    gstHst = Math.Round(r.AccessoryPrice * gstRate / 100.0, 2);
                                    pstQst = Math.Round(r.AccessoryPrice * pstRate / 100.0, 2);
                                    accPrice = 0;
                                }
                            }
                        }
                        else // Discover
                        {
                            if (r.PaymentMethod == "Invoice" || r.PaymentMethod == "CREDIT CARD")
                            {
                                if (r.Group == "HDW")
                                {
                                    salesBeforeTax = r.SellPrice;
                                    double gstRate = r.TaxFlag1 == "Y" ? r.GSTRate : 0;
                                    double pstRate = r.TaxFlag2 == "Y" ? r.PSTRate : 0;
                                    gstHst = Math.Round(r.SellPrice * gstRate / 100.0, 2);
                                    pstQst = Math.Round(r.SellPrice * pstRate / 100.0, 2);
                                }
                                else
                                {
                                    salesBeforeTax = r.AccessoryPrice;
                                    double gstRate = r.TaxFlag1 == "Y" ? r.GSTRate : 0;
                                    double pstRate = r.TaxFlag2 == "Y" ? r.PSTRate : 0;
                                    gstHst = Math.Round(r.AccessoryPrice * gstRate / 100.0, 2);
                                    pstQst = Math.Round(r.AccessoryPrice * pstRate / 100.0, 2);
                                }
                            }
                        }
                    }
                    else // not new taxes
                    {
                        if (r.Group == "HDW")
                        {
                            salesBeforeTax = r.SellPrice;
                            double gstRate = r.TaxFlag1 == "Y" ? r.GSTRate : 0;
                            double pstRate = r.TaxFlag2 == "Y" ? r.PSTRate : 0;
                            gstHst = Math.Round(r.SellPrice * gstRate / 100.0, 2);
                            pstQst = Math.Round(r.SellPrice * pstRate / 100.0, 2);
                        }
                        else
                        {
                            salesBeforeTax = r.AccessoryPrice;
                            double gstRate = r.TaxFlag1 == "Y" ? r.GSTRate : 0;
                            double pstRate = r.TaxFlag2 == "Y" ? r.PSTRate : 0;
                            gstHst = Math.Round(r.AccessoryPrice * gstRate / 100.0, 2);
                            pstQst = Math.Round(r.AccessoryPrice * pstRate / 100.0, 2);
                        }
                    }

                    string updSql = @"
                        UPDATE tblRogersInvoiceAcquisitionOutput 
                        SET TopUpOwing = @TopUpOwing, 
                            [TopUp Acc] = @TopUpAcc, 
                            TopUpTotal = @TopUpTotal, 
                            ARAmount = @ARAmount, 
                            SalesBeforeTax = @SalesBeforeTax, 
                            [GST-HST] = @GstHst, 
                            [PST-QST] = @PstQst, 
                            SellPrice = @SellPrice, 
                            AccessoryPrice = @AccPrice 
                        WHERE Id = @Id";
                    
                    using (var updCmd = new SqlCommand(updSql, sqlConn))
                    {
                        updCmd.CommandTimeout = 600;
                        updCmd.Parameters.AddWithValue("@TopUpOwing", topUpOwing);
                        updCmd.Parameters.AddWithValue("@TopUpAcc", topUpAcc);
                        updCmd.Parameters.AddWithValue("@TopUpTotal", topUpTotal);
                        updCmd.Parameters.AddWithValue("@ARAmount", arAmount);
                        updCmd.Parameters.AddWithValue("@SalesBeforeTax", salesBeforeTax);
                        updCmd.Parameters.AddWithValue("@GstHst", gstHst);
                        updCmd.Parameters.AddWithValue("@PstQst", pstQst);
                        updCmd.Parameters.AddWithValue("@SellPrice", sellPrice);
                        updCmd.Parameters.AddWithValue("@AccPrice", accPrice);
                        updCmd.Parameters.AddWithValue("@Id", r.Id);
                        await updCmd.ExecuteNonQueryAsync();
                    }
                }

                // 2. RecalcFDDAmounts logic
                var fddRecords = new List<dynamic>();
                string getFdd = @"
                    SELECT Id, SalesBeforeTax, [GST-HST], [PST-QST], GSTRate, PSTRate, Fee, ARAmount
                    FROM tblRogersInvoiceAcquisitionOutput
                    WHERE UserId = @UserId AND ChannelName = 'FDDealer' AND Fee <> 0";
                
                using (var cmd = new SqlCommand(getFdd, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            fddRecords.Add(new {
                                Id = Convert.ToInt32(reader.GetValue(0)),
                                SalesBeforeTax = reader.IsDBNull(1) ? 0.0 : Convert.ToDouble(reader.GetValue(1)),
                                GstHst = reader.IsDBNull(2) ? 0.0 : Convert.ToDouble(reader.GetValue(2)),
                                PstQst = reader.IsDBNull(3) ? 0.0 : Convert.ToDouble(reader.GetValue(3)),
                                Gstrate = reader.IsDBNull(4) ? 0.0 : Convert.ToDouble(reader.GetValue(4)),
                                Pstrate = reader.IsDBNull(5) ? 0.0 : Convert.ToDouble(reader.GetValue(5)),
                                Fee = reader.IsDBNull(6) ? 0.0 : Convert.ToDouble(reader.GetValue(6)),
                                ArAmount = reader.IsDBNull(7) ? 0.0 : Convert.ToDouble(reader.GetValue(7))
                            });
                        }
                    }
                }

                foreach (var f in fddRecords)
                {
                    double salesBeforeTaxNew = f.SalesBeforeTax + f.Fee;
                    double gstNew = 0;
                    double pstNew = 0;

                    if (f.GstHst != 0)
                    {
                        gstNew = Math.Round(salesBeforeTaxNew * (f.Gstrate / 100.0), 2);
                    }
                    if (f.PstQst != 0)
                    {
                        pstNew = Math.Round(salesBeforeTaxNew * (f.Pstrate / 100.0), 2);
                    }

                    double totalNew = salesBeforeTaxNew + gstNew + pstNew;
                    double arAmountNew = f.ArAmount != 0 ? totalNew : 0;

                    string updFdd = @"
                        UPDATE tblRogersInvoiceAcquisitionOutput
                        SET SalesBeforeTax = @SalesBeforeTax,
                            [GST-HST] = @GstHst,
                            [PST-QST] = @PstQst,
                            ARAmount = @ArAmount
                        WHERE Id = @Id";
                    
                    using (var cmd = new SqlCommand(updFdd, sqlConn))
                    {
                        cmd.CommandTimeout = 600;
                        cmd.Parameters.AddWithValue("@SalesBeforeTax", salesBeforeTaxNew);
                        cmd.Parameters.AddWithValue("@GstHst", gstNew);
                        cmd.Parameters.AddWithValue("@PstQst", pstNew);
                        cmd.Parameters.AddWithValue("@ArAmount", arAmountNew);
                        cmd.Parameters.AddWithValue("@Id", f.Id);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        private async Task MoveToUserStagingAsync(int userId)
        {
            using (var sqlConn = new SqlConnection(_sqlConnStr))
            {
                await sqlConn.OpenAsync();

                // Move tblRogersInvoiceAcquisitionAR -> tblRogersInvoiceAcquisitionARUSER
                string copyAR = @"
                    INSERT INTO tblRogersInvoiceAcquisitionARUSER (
                        UserId, ChannelName, PaymentMethod, Type, Invoice, InvoiceDate, OrderDate, CustName, 
                        CustTerritory, DealerCode, MSD, UserName, CellPhoneNo, PortedCTN, VoicePlan, DataPlan, 
                        WebOrderID, Type2, Qty, PartNumber, PartNumberDescription, IMEIESN, HdwCost, HdwSellPrice, 
                        TopUpHdw, [Topup Acc], TopUpTotal, AccessoryPrice, Fee, FeeCount, GST, PST, HST, QST, 
                        ARAmount, HDWChargeToCustomer, [True HDW TopUp], SCOA, ShipToProvince,
                        WHSE
                    )
                    SELECT 
                        UserId, ChannelName, PaymentMethod, Type, Invoice, InvoiceDate, OrderDate, CustName, 
                        CustTerritory, DealerCode, MSD, UserName, CellPhoneNo, PortedCTN, VoicePlan, DataPlan, 
                        WebOrderID, Type2, Qty, PartNumber, FreeAccessory, IMEIESN, CostPrice, SellPrice, 
                        TopUpOwing, [Topup Acc], TopUpTotal, AccessoryPrice, Fee, FeeCount, GST, PST, HST, QST, 
                        ARAmount, HDWChargeToCustomer, [True HDW TopUp], SCOA, ShipToProvince,
                        WHSE
                    FROM tblRogersInvoiceAcquisitionAR
                    WHERE UserId = @UserId";
                using (var cmd = new SqlCommand(copyAR, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Move tblRogersInvoiceAcquisitionDetail -> tblRogersInvoiceAcquisitionDetailUSER
                string copyDetail = @"
                    INSERT INTO tblRogersInvoiceAcquisitionDetailUSER (
                        UserId, ChannelName, PaymentMethod, Type, Invoice, BVLineNo, RecNoDetail, InvoiceDate, 
                        OrderDate, CustName, CustTerritory, DealerCode, CellPhoneNo, WebOrderID, Type2, 
                        FreeAccessory, [Topup Acc], AccessoryCost, AccessoryPrice, AccCurrentCostTotal, 
                        AccSRPTotal, AccSRP17Total, AccSRP25Total, AccSRP50Total, ACCSellingPrice, 
                        CODE, Description, ProdCode, Qty, BVCost, BVCostExt, BVPrice, BVPriceExt, 
                        TopUpAmt, TopUpOrig, TopUpModify, CustPayExt, AccGroup, Margin, AccSRP, AccSRP17, 
                        AccSRP25, AccSRP50, TopUpRecalc, MSD, UserName, VoicePlan, DataPlan, GSTRate, PSTRate, 
                        Fee, FeePayback, M2MOrderID, ReturnClassification, GSTFlag, PSTFlag, AdjustmentType, 
                        BVReceiptNo, BVReceiptNoInt, DeviceOfferTypeID, ShipToProvince, OriginalInvoice, 
                        AccessoryType, AccountNumber, RogersACCMarginShare, AgentName, AgentEmail, AgentContactNumber, RDType,
                        WHSE
                    )
                    SELECT 
                        UserId, ChannelName, PaymentMethod, Type, Invoice, BVLineNo, RecNoDetail, InvoiceDate, 
                        OrderDate, CustName, CustTerritory, DealerCode, CellPhoneNo, WebOrderID, Type2, 
                        FreeAccessory, [Topup Acc], AccessoryCost, AccessoryPrice, AccCurrentCostTotal, 
                        AccSRPTotal, AccSRP17Total, AccSRP25Total, AccSRP50Total, ACCSellingPrice, 
                        CODE, Description, ProdCode, Qty, BVCost, BVCostExt, BVPrice, BVPriceExt, 
                        TopUpAmt, TopUpOrig, TopUpModify, CustPayExt, AccGroup, Margin, AccSRP, AccSRP17, 
                        AccSRP25, AccSRP50, TopUpRecalc, MSD, UserName, VoicePlan, DataPlan, GSTRate, PSTRate, 
                        Fee, FeePayback, M2MOrderID, ReturnClassification, GSTFlag, PSTFlag, AdjustmentType, 
                        BVReceiptNo, BVReceiptNoInt, DeviceOfferTypeID, ShipToProvince, OriginalInvoice, 
                        AccessoryType, AccountNumber, RogersACCMarginShare, AgentName, AgentEmail, AgentContactNumber, RDType,
                        WHSE
                    FROM tblRogersInvoiceAcquisitionDetail
                    WHERE UserId = @UserId";
                using (var cmd = new SqlCommand(copyDetail, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Move tblRogersInvoiceAcquisitionOutput -> tblRogersInvoiceAcquisitionOutputUSER
                string copyOutput = @"
                    INSERT INTO tblRogersInvoiceAcquisitionOutputUSER (
                        UserId, ChannelName, PaymentMethod, Type, Invoice, InvoiceDate, OrderDate, CustName, 
                        CustTerritory, DealerCode, MSD, UserName, CellPhoneNo, PortedCTN, VoicePlan, DataPlan, 
                        WebOrderID, Type2, Qty, PartNumber, PartNumberDescription, IMEIESN, HdwCost, HdwSellPrice, 
                        TopUpHdw, AccessoryCost, AccessoryPrice, [TopUp Acc], TopUpTotal, Fee, FeePayback, 
                        FeeCount, ARAmount, [RV-UEValue], HDWChargeToCustomer, [HDWRV-UEValue], [True HDW TopUp], 
                        ACCChargeToCx, AccMargin, HDWMargin, [Group], ShipToProvince, SalesBeforeTax, 
                        [GST-HST], [PST-QST], Total, R4BOrderID, ReturnClassification, RateTierTransactionCount, 
                        Comments, AccSeq, RDAccUnitCost, RDAccExtendedCost, AccessoryType, BAN, AgentName, 
                        AgentEmail, AgentContactNumber, RogersHWMarginShare, RogersACCMarginShare, Term, 
                        BVReceiptCost, IMEIReceiveAppCost, NetPriceProtection, NetIMEIReceiveAppCost, 
                        ReceiptDate1, Invoice1, Qty1, Cost1, ReceiptDate2, Invoice2, Qty2, Cost2, 
                        ReceiptDate3, Invoice3, Qty3, Cost3, BVReceiptNo, BVReceiptNoInt, BVReceiptQty, 
                        BVReceiptCostAcc, BVReceiptDate, RogersInvoiceCost, DeviceOfferTypeID, 
                        DealerHDWMargin, DealerACCMargin, RDType, PPOverpayment,
                        WHSE
                    )
                    SELECT 
                        UserId, ChannelName, PaymentMethod, Type, Invoice, InvoiceDate, OrderDate, CustName, 
                        CustTerritory, DealerCode, MSD, UserName, CellPhoneNo, PortedCTN, VoicePlan, DataPlan, 
                        WebOrderID, Type2, Qty, PartNumber, FreeAccessory, IMEIESN, CostPrice, SellPrice, 
                        TopUpOwing, AccessoryCost, AccessoryPrice, [TopUp Acc], TopUpTotal, Fee, FeePayback, 
                        FeeCount, ARAmount, [RV-UEValue], HDWChargeToCustomer, [HDWRV-UEValue], [True HDW TopUp], 
                        ACCChargeToCx, AccMargin, HDWMargin, [Group], ShipToProvince, SalesBeforeTax, 
                        [GST-HST], [PST-QST], (SalesBeforeTax + [GST-HST] + [PST-QST]) AS Total, M2MOrderID, 
                        ReturnClassification, RateTierTransactionCount, Comments, AccSeq, RDAccUnitCost, 
                        RDAccExtendedCost, AccessoryType, BAN, AgentName, AgentEmail, AgentContactNumber, 
                        RogersHWMarginShare, RogersACCMarginShare, Term, BVReceiptCost, IMEIReceiveAppCost, 
                        NetPriceProtection, NetIMEIReceiveAppCost, ReceiptDate1, Invoice1, Qty1, Cost1, 
                        ReceiptDate2, Invoice2, Qty2, Cost2, ReceiptDate3, Invoice3, Qty3, Cost3, 
                        BVReceiptNo, BVReceiptNoInt, BVReceiptQty, BVReceiptCostAcc, BVReceiptDate, 
                        RogersInvoiceCost, DeviceOfferTypeID, DealerHDWMargin, DealerACCMargin, RDType, PPOverpayment,
                        WHSE
                    FROM tblRogersInvoiceAcquisitionOutput
                    WHERE UserId = @UserId";
                using (var cmd = new SqlCommand(copyOutput, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task UpdateExcelReportsAsync(int userId, string startDateStr, string endDateStr)
        {
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            string targetDir = @"C:\RogersInvoice";
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            string templateDir = ResolveTemplateFolder();

            // Format date range string for cover sheets
            string dateRangeStr = "Mar 01-31, 2024";
            if (DateTime.TryParse(startDateStr, out DateTime startDt) && DateTime.TryParse(endDateStr, out DateTime endDt))
            {
                if (startDt.Year == endDt.Year && startDt.Month == endDt.Month)
                {
                    dateRangeStr = $"{startDt:MMM dd}-{endDt:dd, yyyy}"; // e.g. "Mar 01-31, 2024"
                }
                else
                {
                    dateRangeStr = $"{startDt:MMM dd, yyyy} - {endDt:MMM dd, yyyy}";
                }
            }

            // 1. RogersInvoice-Spire.xlsm
            await ProcessSingleExcelReportAsync(
                string.IsNullOrEmpty(templateDir) ? "" : Path.Combine(templateDir, "RogersInvoice-Spire.xlsm"),
                Path.Combine(targetDir, "RogersInvoice-Spire.xlsm"),
                userId,
                new (string SheetName, string Query)[]
                {
                    ("Details", "SELECT * FROM tblRogersInvoiceAcquisitionOutputUSER WHERE UserId = @UserId AND ChannelName <> 'RCT'"),
                    ("RIL AR", "SELECT * FROM tblRogersInvoiceAcquisitionARUSER WHERE UserId = @UserId AND ChannelName = 'RIL'")
                },
                dateRangeStr
            );

            // 2. RogersInvoiceRCT-Spire.xlsm
            await ProcessSingleExcelReportAsync(
                string.IsNullOrEmpty(templateDir) ? "" : Path.Combine(templateDir, "RogersInvoiceRCT-Spire.xlsm"),
                Path.Combine(targetDir, "RogersInvoiceRCT-Spire.xlsm"),
                userId,
                new (string SheetName, string Query)[]
                {
                    ("Details", "SELECT * FROM tblRogersInvoiceAcquisitionOutputUSER WHERE UserId = @UserId AND ChannelName = 'RCT'"),
                    ("AccDetail", "SELECT * FROM tblRogersInvoiceAcquisitionDetailUSER WHERE UserId = @UserId AND ChannelName = 'RCT'")
                },
                dateRangeStr
            );

            // 3. RogersInvoiceUPSLost-spire.xlsm
            await ProcessSingleExcelReportAsync(
                string.IsNullOrEmpty(templateDir) ? "" : Path.Combine(templateDir, "RogersInvoiceUPSLost-spire.xlsm"),
                Path.Combine(targetDir, "RogersInvoiceUPSLost-spire.xlsm"),
                userId,
                new (string SheetName, string Query)[]
                {
                    ("Claim", "SELECT * FROM tblRogersInvoiceUPSLostUSER WHERE UserId = @UserId")
                },
                null
            );
        }

        private string ResolveTemplateFolder()
        {
            // 1. Try relative to Current Directory parents for wwwroot
            string currentDir = Directory.GetCurrentDirectory();
            while (!string.IsNullOrEmpty(currentDir))
            {
                string testPath = Path.Combine(currentDir, "wwwroot", "Templates", "RogersInvoice");
                if (Directory.Exists(testPath)) return testPath;

                string? parent = Directory.GetParent(currentDir)?.FullName;
                if (parent == currentDir || string.IsNullOrEmpty(parent)) break;
                currentDir = parent;
            }

            // 2. Try relative to Base Directory parents for wwwroot
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            while (!string.IsNullOrEmpty(baseDir))
            {
                string testPath = Path.Combine(baseDir, "wwwroot", "Templates", "RogersInvoice");
                if (Directory.Exists(testPath)) return testPath;

                string? parent = Directory.GetParent(baseDir)?.FullName;
                if (parent == baseDir || string.IsNullOrEmpty(parent)) break;
                baseDir = parent;
            }

            // 3. Try standard workspace path first
            string hardcoded = @"c:\Users\DELL\Downloads\My Code\Excel Sample\RogersInvoice";
            if (Directory.Exists(hardcoded)) return hardcoded;

            // 4. Try relative to Current Directory parents for Excel Sample
            currentDir = Directory.GetCurrentDirectory();
            while (!string.IsNullOrEmpty(currentDir))
            {
                string testPath = Path.Combine(currentDir, "Excel Sample", "RogersInvoice");
                if (Directory.Exists(testPath)) return testPath;

                string? parent = Directory.GetParent(currentDir)?.FullName;
                if (parent == currentDir || string.IsNullOrEmpty(parent)) break;
                currentDir = parent;
            }

            // 5. Try relative to Base Directory parents for Excel Sample
            string current = AppDomain.CurrentDomain.BaseDirectory;
            while (!string.IsNullOrEmpty(current))
            {
                string testPath = Path.Combine(current, "Excel Sample", "RogersInvoice");
                if (Directory.Exists(testPath)) return testPath;

                string? parent = Directory.GetParent(current)?.FullName;
                if (parent == current || string.IsNullOrEmpty(parent)) break;
                current = parent;
            }

            // 6. Check local template folder under C:\RogersInvoice\Templates
            string localFallback = @"C:\RogersInvoice\Templates";
            if (Directory.Exists(localFallback)) return localFallback;

            return "";
        }

        private async Task ProcessSingleExcelReportAsync(
            string templatePath,
            string destPath,
            int userId,
            (string SheetName, string Query)[] mappings,
            string? dateRangeStr)
        {
            if (!File.Exists(destPath))
            {
                // Destination file does not exist, so copy it from templatePath if available
                if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
                {
                    throw new FileNotFoundException(
                        $"Destination file '{Path.GetFileName(destPath)}' does not exist in C:\\RogersInvoice, " +
                        "and could not find a template file in your workspace or templates folders to create it. " +
                        "Please ensure 'Excel Sample\\RogersInvoice' exists in your workspace or place the templates in 'C:\\RogersInvoice'.");
                }

                // Copy template to destination (first time initialization)
                File.Copy(templatePath, destPath, true);
            }

            var fileInfo = new FileInfo(destPath);
            using (var package = new ExcelPackage(fileInfo))
            {
                // Write date range to cover sheet if it exists
                var invoiceSheet = package.Workbook.Worksheets["Invoice"];
                if (invoiceSheet != null && !string.IsNullOrEmpty(dateRangeStr))
                {
                    if (Path.GetFileNameWithoutExtension(destPath).Contains("RCT"))
                    {
                        invoiceSheet.Cells["C7"].Value = dateRangeStr;
                    }
                    else
                    {
                        invoiceSheet.Cells["C6"].Value = dateRangeStr;
                    }
                }

                foreach (var map in mappings)
                {
                    var sheet = package.Workbook.Worksheets[map.SheetName];
                    if (sheet == null) continue;

                    var dt = new DataTable();
                    using (var conn = new SqlConnection(_sqlConnStr))
                    {
                        using (var cmd = new SqlCommand(map.Query, conn))
                        {
                            cmd.CommandTimeout = 600;
                            cmd.Parameters.AddWithValue("@UserId", userId);
                            using (var da = new SqlDataAdapter(cmd))
                            {
                                da.Fill(dt);
                            }
                        }
                    }

                    PopulateWorksheetFromDb(sheet, dt);
                }

                // Force Excel to recalculate formulas when opened
                package.Workbook.CalcMode = ExcelCalcMode.Automatic;
                await package.SaveAsync();
            }
        }

        private void PopulateWorksheetFromDb(ExcelWorksheet sheet, DataTable dt)
        {
            int headerRow = 1;
            int colCount = sheet.Dimension?.Columns ?? 0;
            if (colCount == 0) return;

            // Read headers
            var headerMap = new Dictionary<int, string>();
            for (int col = 1; col <= colCount; col++)
            {
                string headerName = sheet.Cells[headerRow, col].Text?.Trim() ?? "";
                if (!string.IsNullOrEmpty(headerName))
                {
                    headerMap[col] = headerName;
                }
            }

            // Write new data cells starting from row 2
            int currentRow = headerRow + 1;
            foreach (DataRow row in dt.Rows)
            {
                foreach (var kvp in headerMap)
                {
                    int col = kvp.Key;
                    string excelHeader = kvp.Value;

                    string? matchedCol = FindMatchingColumn(dt, excelHeader);
                    if (matchedCol != null)
                    {
                        object val = row[matchedCol];
                        if (val == DBNull.Value)
                        {
                            sheet.Cells[currentRow, col].Value = null;
                        }
                        else
                        {
                            if (val is DateTime dtVal)
                            {
                                sheet.Cells[currentRow, col].Value = dtVal;
                                if (string.IsNullOrEmpty(sheet.Cells[currentRow, col].Style.Numberformat.Format))
                                {
                                    sheet.Cells[currentRow, col].Style.Numberformat.Format = "yyyy-mm-dd";
                                }
                            }
                            else
                            {
                                sheet.Cells[currentRow, col].Value = val;
                            }
                        }
                    }
                    else
                    {
                        sheet.Cells[currentRow, col].Value = null;
                    }
                }
                currentRow++;
            }

            // Clear any leftover rows from previous runs
            int lastRow = sheet.Dimension?.Rows ?? 1;
            if (lastRow >= currentRow)
            {
                var range = sheet.Cells[currentRow, 1, lastRow, colCount];
                range.Clear();
            }

            // Set/enable AutoFilter on the exact active data range (row 1 down to the last written row)
            int finalDataRow = currentRow - 1;
            if (finalDataRow >= 1)
            {
                sheet.Cells[1, 1, finalDataRow, colCount].AutoFilter = true;
            }
        }

        private string? FindMatchingColumn(DataTable dt, string excelHeader)
        {
            foreach (DataColumn col in dt.Columns)
            {
                if (string.Equals(col.ColumnName, excelHeader, StringComparison.OrdinalIgnoreCase))
                    return col.ColumnName;
            }

            var normalized = excelHeader.Trim();
            if (string.Equals(normalized, "ARAmount-ARTaxes-CCGrossSale", StringComparison.OrdinalIgnoreCase))
            {
                if (dt.Columns.Contains("ARAmount")) return "ARAmount";
            }

            var cleanExcel = normalized.Replace(" ", "").Replace("-", "").Replace("_", "");
            foreach (DataColumn col in dt.Columns)
            {
                var cleanCol = col.ColumnName.Replace(" ", "").Replace("-", "").Replace("_", "");
                if (string.Equals(cleanCol, cleanExcel, StringComparison.OrdinalIgnoreCase))
                    return col.ColumnName;
            }

            return null;
        }

        public async Task<List<CostVerificationRow>> GetCostVerificationReportAsync(string startDate, string endDate)
        {
            var list = new List<CostVerificationRow>();
            using (var sqlConn = new SqlConnection(_sqlConnStr))
            {
                await sqlConn.OpenAsync();

                // Fetch NetIMEIReceiveCost from Postgres inventory_receipts
                var pgDt = new System.Data.DataTable();
                using (var pgConn = new NpgsqlConnection(_pgConnStr))
                {
                    await pgConn.OpenAsync();
                    string pgSql = @"
                        SELECT bvreceiptno, SUM(CASE WHEN transtype = 'C' THEN perunitamount * -1 ELSE perunitamount END) AS NetIMEIReceiveCost
                        FROM inventory_receipts
                        GROUP BY bvreceiptno";
                    using (var pgCmd = new NpgsqlCommand(pgSql, pgConn))
                    {
                        pgCmd.CommandTimeout = 600;
                        using (var pgReader = await pgCmd.ExecuteReaderAsync())
                        {
                            pgDt.Load(pgReader);
                        }
                    }
                }

                string createTemp = @"
                    CREATE TABLE #TempNetIMEIReceiveCost (
                        bvreceiptno VARCHAR(255) COLLATE DATABASE_DEFAULT,
                        NetIMEIReceiveCost FLOAT
                    )";
                using (var cmd = new SqlCommand(createTemp, sqlConn)) await cmd.ExecuteNonQueryAsync();

                using (var bulkCopy = new SqlBulkCopy(sqlConn))
                {
                    bulkCopy.BulkCopyTimeout = 600;
                    bulkCopy.DestinationTableName = "#TempNetIMEIReceiveCost";
                    await bulkCopy.WriteToServerAsync(pgDt);
                }

                // Select from CostVerification output query matching date range
                string sql = @"
                    WITH qryPriceProtectionSummary AS (
                        SELECT ""ReceiptNo"", ""SKU"", ""IMEI"", SUM(""ClaimAmount"") AS ""SumOfClaimAmount"", COUNT(""ID"") AS ""PPClaimCount""
                        FROM tblPriceProtection
                        GROUP BY ""ReceiptNo"", ""SKU"", ""IMEI""
                    )
                    SELECT 
                        sa.""TransactionNo"", sa.""Invoice"", sa.""InvoiceDate"", sa.""CustName"", sa.""CustTerritory"", 
                        sa.""Whse"", sa.""PartNumber"", sa.""Description"" AS ""FreeAccessory"", sa.""Qty"", sa.""IMEIESN"", 
                        CASE WHEN sa.""Qty"" = 0 THEN sa.""ItemCost"" ELSE sa.""ItemCost"" * sa.""Qty"" END AS ""CostPrice"", 
                        CASE WHEN sa.""Qty"" = 0 THEN sa.""ItemSellPrice"" ELSE sa.""ItemSellPrice"" * sa.""Qty"" END AS ""SellPrice"", 
                        CASE WHEN ABS(sa.""ItemSellPrice"" * sa.""Qty"") > ABS(sa.""ItemCost"" * sa.""Qty"") THEN 0 ELSE (sa.""ItemCost"" * sa.""Qty"") - (sa.""ItemSellPrice"" * sa.""Qty"") END AS ""TopUpOwing"", 
                        ni.""NetIMEIReceiveCost"" * sa.""Qty"" AS ""NetIMEI ReceiveCost"",
                        pp.""SumOfClaimAmount"" AS ""NetPriceProtection"",
                        sa.""BVReceipt"", sa.""BVReceiptNo""
                    FROM SalesActivations sa
                    LEFT JOIN #TempNetIMEIReceiveCost ni ON sa.""BVReceipt"" = ni.""bvreceiptno""
                    LEFT JOIN qryPriceProtectionSummary pp ON sa.""IMEIESN"" = pp.""IMEI"" AND sa.""PartNumber"" = pp.""SKU"" AND sa.""BVReceipt"" = pp.""ReceiptNo""
                    WHERE sa.""InvoiceDate"" BETWEEN @StartDate AND @EndDate
                    ORDER BY sa.""InvoiceDate"", sa.""Invoice"";";

                using (var cmd = new SqlCommand(sql, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@StartDate", DateTime.Parse(startDate));
                    cmd.Parameters.AddWithValue("@EndDate", DateTime.Parse(endDate));

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new CostVerificationRow
                            {
                                TransactionNo = reader.IsDBNull(0) ? "" : reader.GetString(0),
                                Invoice = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                InvoiceDate = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2),
                                CustName = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                CustTerritory = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                Whse = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                PartNumber = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                FreeAccessory = reader.IsDBNull(7) ? "" : reader.GetString(7),
                                Qty = reader.IsDBNull(8) ? 0.0 : Convert.ToDouble(reader.GetValue(8)),
                                IMEIESN = reader.IsDBNull(9) ? "" : reader.GetString(9),
                                CostPrice = reader.IsDBNull(10) ? 0.0 : Convert.ToDouble(reader.GetValue(10)),
                                SellPrice = reader.IsDBNull(11) ? 0.0 : Convert.ToDouble(reader.GetValue(11)),
                                TopUpOwing = reader.IsDBNull(12) ? 0.0 : Convert.ToDouble(reader.GetValue(12)),
                                NetIMEIReceiveCost = reader.IsDBNull(13) ? 0.0 : Convert.ToDouble(reader.GetValue(13)),
                                NetPriceProtection = reader.IsDBNull(14) ? 0.0 : Convert.ToDouble(reader.GetValue(14)),
                                BVReceipt = reader.IsDBNull(15) ? "" : reader.GetString(15)
                            };
                            
                            // Save BVReceiptNo temporarily in PONumber for Postgres lookup later since PONumber is unused yet
                            row.PONumber = reader.IsDBNull(16) ? "" : reader.GetValue(16).ToString();

                            list.Add(row);
                        }
                    }
                }
            }

            if (list.Any())
            {
                // Fill pr.cost and pr.link_no from Postgres inventory_receipts
                // Use the temporary BVReceiptNo we stored in PONumber
                var receiptNos = list.Select(l => l.PONumber ?? "").Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
                var prData = new Dictionary<string, (decimal Cost, string LinkNo)>();

                if (receiptNos.Any())
                {
                    using (var pgConn = new NpgsqlConnection(_pgConnStr))
                    {
                        await pgConn.OpenAsync();
                        // cast id to string for comparison and return
                        string pgSql = "SELECT id::varchar, cost, link_no FROM inventory_receipts WHERE id::varchar = ANY(@IDs)";
                        using (var cmd = new NpgsqlCommand(pgSql, pgConn))
                        {
                            cmd.Parameters.AddWithValue("IDs", receiptNos);
                            cmd.CommandTimeout = 600;

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    string id = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                    decimal cost = reader.IsDBNull(1) ? 0m : Convert.ToDecimal(reader.GetValue(1));
                                    string linkNo = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                    prData[id] = (cost, linkNo);
                                }
                            }
                        }
                    }
                }

                var skus = list.Select(l => l.PartNumber ?? "").Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
                var miscData = new Dictionary<string, string>();

                if (skus.Any())
                {
                    using (var pgConn = new NpgsqlConnection(_pgConnStr))
                    {
                        await pgConn.OpenAsync();
                        string pgSql = "SELECT part_no, misc_1 FROM inventory WHERE part_no = ANY(@SKUs) LIMIT 100";
                        using (var cmd = new NpgsqlCommand(pgSql, pgConn))
                        {
                            cmd.Parameters.AddWithValue("SKUs", skus);
                            cmd.CommandTimeout = 600;

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    miscData[reader.GetString(0)] = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                }
                            }
                        }
                    }
                }

                foreach (var row in list)
                {
                    // Update from miscData
                    if (row.PartNumber != null && miscData.TryGetValue(row.PartNumber, out var misc))
                    {
                        row.MISC_1 = misc;
                    }
                    
                    // Update from prData
                    if (row.PONumber != null && prData.TryGetValue(row.PONumber, out var pr))
                    {
                        row.BVReceiptCost = (row.Qty < 0) ? (-1 * pr.Cost) : pr.Cost;
                        row.PONumber = pr.LinkNo;
                    }
                    else
                    {
                        row.PONumber = ""; // Clear temporary BVReceiptNo
                        row.BVReceiptCost = 0m;
                    }
                }
            }

            return list;
        }

        public async Task<List<DailySalesRow>> GetSalesSummaryByPaymentMethodAsync(string startDate, string endDate)
        {
            var list = new List<DailySalesRow>();
            using (var pgConn = new NpgsqlConnection(_pgConnStr))
            {
                await pgConn.OpenAsync();

                // Select from Postgres sales_history joined with payments, customers, payment methods
                string sql = @"
                    SELECT 
                        sh.invoice_no, sh.invoice_date, pm.description AS pay_method, 
                        sh.trans_no, sh.cust_no, sh.cust_name, sh.total, 
                        sh.territory_code, ad.sales_terr
                    FROM sales_history sh
                    INNER JOIN sales_history_payments shp ON sh.invoice_no = shp.invoice_no
                    INNER JOIN payment_methods pm ON shp.payment_method = pm.id
                    INNER JOIN customers cu ON sh.cust_no = cu.cust_no
                    INNER JOIN addresses ad ON cu.cust_no = ad.link_no AND ad.link_table = 'CUST' AND ad.addr_type = 'B'
                    WHERE sh.invoice_date BETWEEN @StartDate::date AND @EndDate::date
                    ORDER BY sh.invoice_date, sh.invoice_no";

                using (var cmd = new NpgsqlCommand(sql, pgConn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("StartDate", NpgsqlTypes.NpgsqlDbType.Date, DateTime.Parse(startDate));
                    cmd.Parameters.AddWithValue("EndDate", NpgsqlTypes.NpgsqlDbType.Date, DateTime.Parse(endDate));

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new DailySalesRow
                            {
                                InvoiceNo = reader.IsDBNull(0) ? "" : reader.GetString(0),
                                Date = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1),
                                PaymentMethod = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                TransNo = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                CustNo = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                CustName = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                Total = reader.IsDBNull(6) ? 0m : Convert.ToDecimal(reader.GetValue(6)),
                                InvTerr = reader.IsDBNull(7) ? "" : reader.GetString(7),
                                CustTerr = reader.IsDBNull(8) ? "" : reader.GetString(8)
                            });
                        }
                    }
                }
            }

            // Fill WebOrderID from SQL Server
            if (list.Any())
            {
                var invoices = list.Select(l => l.InvoiceNo ?? "").Distinct().ToList();
                var webOrderIds = new Dictionary<string, string>();

                using (var sqlConn = new SqlConnection(_sqlConnStr))
                {
                    await sqlConn.OpenAsync();
                    string sql = "SELECT Invoice, WebOrderID FROM SalesActivations WHERE Invoice IN (SELECT value FROM string_split(@Invoices, ','))";
                    using (var cmd = new SqlCommand("SELECT Invoice, MAX(WebOrderID) FROM SalesActivations WHERE Invoice = @Invoice GROUP BY Invoice", sqlConn))
                    {
                        cmd.CommandTimeout = 600;
                        foreach (var inv in invoices)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@Invoice", inv);
                            var val = await cmd.ExecuteScalarAsync();
                            if (val != null)
                            {
                                webOrderIds[inv] = val.ToString() ?? "";
                            }
                        }
                    }
                }

                foreach (var row in list)
                {
                    if (row.InvoiceNo != null && webOrderIds.TryGetValue(row.InvoiceNo, out var webId))
                    {
                        row.WebOrderID = webId;
                    }
                }
            }

            return list;
        }

        public async Task<List<ReturnsVerificationRow>> GetReturnsVerificationReportAsync(string startDate, string endDate, string returnsStart, string returnsEnd, int userId)
        {
            await EnsureTablesExistAsync();
            using (var sqlConn = new SqlConnection(_sqlConnStr))
            {
                await sqlConn.OpenAsync();

                // 1. Delete from temp returns validation table
                string delSql = "DELETE FROM tblRogersInvoiceTempReturnsValidation WHERE UserId = @UserId";
                using (var cmd = new SqlCommand(delSql, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 2. Insert into temp table from tblRogersInvoiceAcquisitionOutputUSER returns in date range
                string insSql = @"
                    INSERT INTO tblRogersInvoiceTempReturnsValidation (
                        UserId, ChannelName, PaymentMethod, Type, Invoice, InvoiceDate, CustTerritory, 
                        CellPhoneNo, WebOrderID, Qty, PartNumber, FreeAccessory, IMEIESN, CostPrice, 
                        SellPrice, TopUpOwing, AccessoryCost, AccessoryPrice, [TopUp Acc], TopUpTotal, 
                        ARAmount, HDWChargeToCustomer, [True HDW TopUp], ACCChargeToCx, AccMargin, [Group], Source
                    )
                    SELECT 
                        @UserId, ChannelName, PaymentMethod, Type, Invoice, InvoiceDate, CustTerritory, 
                        CellPhoneNo, WebOrderID, Qty, PartNumber, PartNumberDescription, IMEIESN, HdwCost, 
                        HdwSellPrice, TopUpHdw, AccessoryCost, AccessoryPrice, [TopUp Acc], TopUpTotal, 
                        ARAmount, HDWChargeToCustomer, [True HDW TopUp], ACCChargeToCx, AccMargin, [Group], 'AcquisitionOutput'
                    FROM tblRogersInvoiceAcquisitionOutputUSER
                    WHERE UserId = @UserId 
                      AND Type LIKE 'Return%' 
                      AND InvoiceDate BETWEEN @RetStart AND @RetEnd 
                      AND Qty < 0";

                using (var cmd = new SqlCommand(insSql, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@RetStart", returnsStart.Trim());
                    cmd.Parameters.AddWithValue("@RetEnd", returnsEnd.Trim());
                    await cmd.ExecuteNonQueryAsync();
                }

                // 3. For each return, search for matching original sale and populate matching columns
                var returns = new List<dynamic>();
                string selSql = "SELECT Id, WebOrderID, PartNumber, IMEIESN FROM tblRogersInvoiceTempReturnsValidation WHERE UserId = @UserId";
                using (var cmd = new SqlCommand(selSql, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            returns.Add(new {
                                Id = Convert.ToInt32(reader.GetValue(0)),
                                WebOrderID = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                PartNumber = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                IMEIESN = reader.IsDBNull(3) ? "" : reader.GetString(3)
                            });
                        }
                    }
                }

                foreach (var r in returns)
                {
                    // Find original sale in tblRogersInvoiceAcquisitionOutputUSER or FFFClaimsMaster
                    string saleSql = @"
                        SELECT TOP 1 
                            ChannelName, PaymentMethod, Type, Invoice, InvoiceDate, CustTerritory, 
                            CellPhoneNo, WebOrderID, Qty, PartNumber, PartNumberDescription, IMEIESN, 
                            HdwCost, HdwSellPrice, TopUpHdw, AccessoryCost, AccessoryPrice, [TopUp Acc], 
                            TopUpTotal, ARAmount, HDWChargeToCustomer, [True HDW TopUp], ACCChargeToCx, 
                            AccMargin, [Group]
                        FROM tblRogersInvoiceAcquisitionOutputUSER
                        WHERE UserId = @UserId 
                          AND Qty > 0 
                          AND WebOrderID = @WebID 
                          AND PartNumber = @Part 
                          AND IMEIESN = @Imei
                        ORDER BY InvoiceDate DESC";
                    
                    var sale = new Dictionary<string, object>();
                    using (var cmd = new SqlCommand(saleSql, sqlConn))
                    {
                        cmd.CommandTimeout = 600;
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@WebID", r.WebOrderID);
                        cmd.Parameters.AddWithValue("@Part", r.PartNumber);
                        cmd.Parameters.AddWithValue("@Imei", r.IMEIESN);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (reader.Read())
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    sale[reader.GetName(i)] = reader.GetValue(i);
                                }
                            }
                        }
                    }

                    if (sale.Any())
                    {
                        string updSql = @"
                            UPDATE tblRogersInvoiceTempReturnsValidation
                            SET ChannelName2 = @ChannelName2, PaymentMethod2 = @PaymentMethod2, Type2 = @Type2, 
                                Invoice2 = @Invoice2, InvoiceDate2 = @InvoiceDate2, CustTerritory2 = @CustTerritory2, 
                                CellPhoneNo2 = @CellPhoneNo2, WebOrderID2 = @WebOrderID2, Qty2 = @Qty2, 
                                PartNumber2 = @PartNumber2, FreeAccessory2 = @FreeAccessory2, IMEIESN2 = @IMEIESN2, 
                                CostPrice2 = @CostPrice2, SellPrice2 = @SellPrice2, TopUpOwing2 = @TopUpOwing2, 
                                AccessoryCost2 = @AccessoryCost2, AccessoryPrice2 = @AccessoryPrice2, 
                                [TopUp Acc2] = @TopUpAcc2, TopUpTotal2 = @TopUpTotal2, ARAmount2 = @ARAmount2, 
                                HDWChargeToCustomer2 = @HDWChargeToCustomer2, [True HDW TopUp2] = @TrueHDWTopUp2, 
                                ACCChargeToCx2 = @ACCChargeToCx2, AccMargin2 = @AccMargin2, Group2 = @Group2
                            WHERE Id = @Id";
                        
                        using (var cmd = new SqlCommand(updSql, sqlConn))
                        {
                            cmd.CommandTimeout = 600;
                            cmd.Parameters.AddWithValue("@ChannelName2", sale["ChannelName"]);
                            cmd.Parameters.AddWithValue("@PaymentMethod2", sale["PaymentMethod"]);
                            cmd.Parameters.AddWithValue("@Type2", sale["Type"]);
                            cmd.Parameters.AddWithValue("@Invoice2", sale["Invoice"]);
                            cmd.Parameters.AddWithValue("@InvoiceDate2", sale["InvoiceDate"]);
                            cmd.Parameters.AddWithValue("@CustTerritory2", sale["CustTerritory"]);
                            cmd.Parameters.AddWithValue("@CellPhoneNo2", sale["CellPhoneNo"]);
                            cmd.Parameters.AddWithValue("@WebOrderID2", sale["WebOrderID"]);
                            cmd.Parameters.AddWithValue("@Qty2", sale["Qty"]);
                            cmd.Parameters.AddWithValue("@PartNumber2", sale["PartNumber"]);
                            cmd.Parameters.AddWithValue("@FreeAccessory2", sale["PartNumberDescription"]);
                            cmd.Parameters.AddWithValue("@IMEIESN2", sale["IMEIESN"]);
                            cmd.Parameters.AddWithValue("@CostPrice2", sale["HdwCost"]);
                            cmd.Parameters.AddWithValue("@SellPrice2", sale["HdwSellPrice"]);
                            cmd.Parameters.AddWithValue("@TopUpOwing2", sale["TopUpHdw"]);
                            cmd.Parameters.AddWithValue("@AccessoryCost2", sale["AccessoryCost"]);
                            cmd.Parameters.AddWithValue("@AccessoryPrice2", sale["AccessoryPrice"]);
                            cmd.Parameters.AddWithValue("@TopUpAcc2", sale["TopUp Acc"]);
                            cmd.Parameters.AddWithValue("@TopUpTotal2", sale["TopUpTotal"]);
                            cmd.Parameters.AddWithValue("@ARAmount2", sale["ARAmount"]);
                            cmd.Parameters.AddWithValue("@HDWChargeToCustomer2", sale["HDWChargeToCustomer"]);
                            cmd.Parameters.AddWithValue("@TrueHDWTopUp2", sale["True HDW TopUp"]);
                            cmd.Parameters.AddWithValue("@ACCChargeToCx2", sale["ACCChargeToCx"]);
                            cmd.Parameters.AddWithValue("@AccMargin2", sale["AccMargin"]);
                            cmd.Parameters.AddWithValue("@Group2", sale["Group"]);
                            cmd.Parameters.AddWithValue("@Id", r.Id);

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                // Return all returns verification data
                var report = new List<ReturnsVerificationRow>();
                string getReport = "SELECT * FROM tblRogersInvoiceTempReturnsValidation WHERE UserId = @UserId ORDER BY InvoiceDate, Invoice";
                using (var cmd = new SqlCommand(getReport, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            report.Add(new ReturnsVerificationRow {
                                Id = Convert.ToInt32(reader.GetValue(0)),
                                UserId = Convert.ToInt32(reader.GetValue(1)),
                                ChannelName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                PaymentMethod = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                Type = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                Invoice = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                InvoiceDate = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6),
                                CustTerritory = reader.IsDBNull(7) ? "" : reader.GetString(7),
                                CellPhoneNo = reader.IsDBNull(8) ? "" : reader.GetString(8),
                                WebOrderID = reader.IsDBNull(9) ? "" : reader.GetString(9),
                                Qty = reader.IsDBNull(10) ? 0.0 : Convert.ToDouble(reader.GetValue(10)),
                                PartNumber = reader.IsDBNull(11) ? "" : reader.GetString(11),
                                FreeAccessory = reader.IsDBNull(12) ? "" : reader.GetString(12),
                                IMEIESN = reader.IsDBNull(13) ? "" : reader.GetString(13),
                                CostPrice = reader.IsDBNull(14) ? 0.0 : Convert.ToDouble(reader.GetValue(14)),
                                SellPrice = reader.IsDBNull(15) ? 0.0 : Convert.ToDouble(reader.GetValue(15)),
                                TopUpOwing = reader.IsDBNull(16) ? 0.0 : Convert.ToDouble(reader.GetValue(16)),
                                AccessoryCost = reader.IsDBNull(17) ? 0.0 : Convert.ToDouble(reader.GetValue(17)),
                                AccessoryPrice = reader.IsDBNull(18) ? 0.0 : Convert.ToDouble(reader.GetValue(18)),
                                TopUpAcc = reader.IsDBNull(19) ? 0.0 : Convert.ToDouble(reader.GetValue(19)),
                                TopUpTotal = reader.IsDBNull(20) ? 0.0 : Convert.ToDouble(reader.GetValue(20)),
                                ARAmount = reader.IsDBNull(21) ? 0.0 : Convert.ToDouble(reader.GetValue(21)),
                                HDWChargeToCustomer = reader.IsDBNull(22) ? 0.0 : Convert.ToDouble(reader.GetValue(22)),
                                TrueHDWTopUp = reader.IsDBNull(23) ? 0.0 : Convert.ToDouble(reader.GetValue(23)),
                                ACCChargeToCx = reader.IsDBNull(24) ? 0.0 : Convert.ToDouble(reader.GetValue(24)),
                                AccMargin = reader.IsDBNull(25) ? 0.0 : Convert.ToDouble(reader.GetValue(25)),
                                Group = reader.IsDBNull(26) ? "" : reader.GetString(26),
                                Source = reader.IsDBNull(27) ? "" : reader.GetString(27),
                                
                                ChannelName2 = reader.IsDBNull(28) ? "" : reader.GetString(28),
                                PaymentMethod2 = reader.IsDBNull(29) ? "" : reader.GetString(29),
                                Type2 = reader.IsDBNull(30) ? "" : reader.GetString(30),
                                Invoice2 = reader.IsDBNull(31) ? "" : reader.GetString(31),
                                InvoiceDate2 = reader.IsDBNull(32) ? (DateTime?)null : reader.GetDateTime(32),
                                CustTerritory2 = reader.IsDBNull(33) ? "" : reader.GetString(33),
                                CellPhoneNo2 = reader.IsDBNull(34) ? "" : reader.GetString(34),
                                WebOrderID2 = reader.IsDBNull(35) ? "" : reader.GetString(35),
                                Qty2 = reader.IsDBNull(36) ? 0.0 : Convert.ToDouble(reader.GetValue(36)),
                                PartNumber2 = reader.IsDBNull(37) ? "" : reader.GetString(37),
                                FreeAccessory2 = reader.IsDBNull(38) ? "" : reader.GetString(38),
                                IMEIESN2 = reader.IsDBNull(39) ? "" : reader.GetString(39),
                                CostPrice2 = reader.IsDBNull(40) ? 0.0 : Convert.ToDouble(reader.GetValue(40)),
                                SellPrice2 = reader.IsDBNull(41) ? 0.0 : Convert.ToDouble(reader.GetValue(41)),
                                TopUpOwing2 = reader.IsDBNull(42) ? 0.0 : Convert.ToDouble(reader.GetValue(42)),
                                AccessoryCost2 = reader.IsDBNull(43) ? 0.0 : Convert.ToDouble(reader.GetValue(43)),
                                AccessoryPrice2 = reader.IsDBNull(44) ? 0.0 : Convert.ToDouble(reader.GetValue(44)),
                                TopUpAcc2 = reader.IsDBNull(45) ? 0.0 : Convert.ToDouble(reader.GetValue(45)),
                                TopUpTotal2 = reader.IsDBNull(46) ? 0.0 : Convert.ToDouble(reader.GetValue(46)),
                                ARAmount2 = reader.IsDBNull(47) ? 0.0 : Convert.ToDouble(reader.GetValue(47)),
                                HDWChargeToCustomer2 = reader.IsDBNull(48) ? 0.0 : Convert.ToDouble(reader.GetValue(48)),
                                TrueHDWTopUp2 = reader.IsDBNull(49) ? 0.0 : Convert.ToDouble(reader.GetValue(49)),
                                ACCChargeToCx2 = reader.IsDBNull(50) ? 0.0 : Convert.ToDouble(reader.GetValue(50)),
                                AccMargin2 = reader.IsDBNull(51) ? 0.0 : Convert.ToDouble(reader.GetValue(51)),
                                Group2 = reader.IsDBNull(52) ? "" : reader.GetString(52)
                            });
                        }
                    }
                }

                return report;
            }
        }

        // Translation of helper functions in Module1.bas
        private string DetermineChannel(string territory, string channel, string feeType, string controlCentre)
        {
            if (territory == "HMS") return "UPS Lost";
            if (territory.Contains("CYG") || territory.Contains("CHY")) return "Ontario Government (MGS)";
            if (channel == "Government") return "Government of Canada";
            if (feeType.Contains("GOV")) return "Government of Canada";
            if (feeType.Contains("MFLEET") || territory == "MFL") return "M-Fleet";
            if (feeType.Contains("MDA") || territory == "MDA") return "MDA";

            if (territory.StartsWith("C"))
            {
                return controlCentre == "RCC" ? "RCC" : "Corporate";
            }

            if (territory.Length == 4 && territory.StartsWith("RD")) return "RDDealer";
            if (territory.Length == 4 && territory.StartsWith("FD")) return "FDDealer";

            return territory;
        }

        private string DeterminePayMethod(string terms)
        {
            if (terms == "CREDIT CARD") return "CREDIT CARD";
            if (terms == "V21 Account") return "V21 Account";
            return "Invoice";
        }

        private string DetermineType(string capHardware, string voice, string data, string prodCode, string description, string recordType, string adjustmentType, string orderNumber, string webOrderID, string recType2)
        {
            if (recordType == "Sale") return "Acc";
            if (adjustmentType.ToUpper().Contains("RETURN") || adjustmentType.ToUpper().Contains("ACC RET")) return "Return";

            if (recType2.ToUpper().StartsWith("COAM")) return "Acquisition";
            if (recType2 == "HUP") return "HUP";
            if (recType2.ToUpper() == "VOICE" || recType2.ToUpper() == "DATA") return "Acquisition";

            if (!string.IsNullOrEmpty(capHardware)) return "HUP";

            string descUpper = description.ToUpper();
            if (descUpper.Contains("LICENCE") || descUpper.Contains("LICENSE") || prodCode == "HLC" || prodCode == "ACC") return "Acc";

            if ((prodCode == "HCC" || prodCode == "HCL") && string.IsNullOrEmpty(voice) && string.IsNullOrEmpty(data)) return "HUP";

            if (!string.IsNullOrEmpty(voice) || !string.IsNullOrEmpty(data)) return "Acquisition";

            return "Acc";
        }

        private string DetermineTypeOld2(string recType2, string prodCode, string partNumber, string termVal)
        {
            if (termVal == "0") return "No Term";
            if (recType2 == "HUP") return "HUP";
            if (recType2.ToUpper().Contains("VOICE")) return "Voice";
            if (recType2.ToUpper().Contains("DATA")) return "Data";

            if (string.IsNullOrEmpty(recType2) && (partNumber == "RETURNONMGS" || partNumber == "RETURNGOV")) return "Exchange Only";

            if (prodCode == "ACC" || prodCode == "OBA" || recType2.ToUpper().Contains("ACC")) return "Acc";

            return recType2; // Return default
        }

        private string DiscoverOrRogers(string territory)
        {
            if (territory == "RIL" || territory == "RCT" || territory == "RCR" || territory == "RRT" ||
                territory.StartsWith("V") || territory.StartsWith("D") || territory.StartsWith("H") || territory.StartsWith("FD"))
            {
                return "Discover";
            }
            return "Rogers";
        }

        private double AccTopUpCalculation(string territory, string partNumber, double custPayAmount, double qty, double bvPrice, double srp, string channel, string accGroup, double cost)
        {
            bool blnCredit = qty < 0;
            qty = Math.Abs(qty);
            double topUp = 0;

            if (!territory.StartsWith("D"))
            {
                if (partNumber != "SHIPPING")
                {
                    double dblAccCost = qty * cost;
                    double dblAccSRP = qty * srp;

                    double dblCustomerPaysDiscover = bvPrice * qty;
                    double dblCustomerPaysTotal = custPayAmount * qty;

                    double dblDiscountRate = 0.17;
                    if (channel == "Government of Canada") dblDiscountRate = 0.25;
                    else if (channel == "Ontario Government (MGS)") dblDiscountRate = 0.5;

                    if (accGroup == "LICENSE")
                    {
                        topUp = dblAccSRP - dblCustomerPaysDiscover;
                    }
                    else
                    {
                        if (dblCustomerPaysTotal == 0)
                        {
                            topUp = dblAccCost;
                        }
                        else
                        {
                            double targetVal = dblAccSRP - (dblAccSRP * dblDiscountRate) - dblCustomerPaysDiscover;
                            topUp = Math.Round(targetVal, 2, MidpointRounding.AwayFromZero);

                            if (topUp > 0 && topUp < (dblAccCost - dblCustomerPaysDiscover))
                            {
                                topUp = dblAccCost - dblCustomerPaysDiscover;
                            }

                            if ((topUp < 0 && qty > 0) || (topUp > 0 && qty < 0))
                            {
                                topUp = 0;
                            }
                        }
                    }

                    if (partNumber == "M2MSIM2FF" || partNumber == "M2MSIM3FF")
                    {
                        topUp = dblAccCost - dblCustomerPaysDiscover;
                    }

                    if (partNumber == "CNMISCUPGRADE" || partNumber == "CNMISCUPGRADE1")
                    {
                        topUp = 13.0 * qty;
                    }
                    else if (partNumber == "CNMISCUPGRADE2")
                    {
                        topUp = 5.0 * qty;
                    }
                    else if (partNumber == "CIBCMISCUPGRADE")
                    {
                        topUp = 13.0 * qty;
                    }
                }
            }

            if (territory.Trim() == "RIL")
            {
                double dblAccCost = qty * cost;
                double dblCustomerPaysDiscover = bvPrice * qty;
                topUp = dblAccCost - dblCustomerPaysDiscover;
            }

            if (blnCredit) topUp = topUp * -1;

            return topUp;
        }

        public async Task<List<CostVerificationRow>> GetHdwFeeReportAsync(int userId)
        {
            await EnsureTablesExistAsync();
            using (var sqlConn = new SqlConnection(_sqlConnStr))
            {
                await sqlConn.OpenAsync();
                var list = new List<CostVerificationRow>();
                string sql = @"
                    SELECT 
                        0 AS TransactionNo,
                        Invoice,
                        InvoiceDate,
                        CustName,
                        CustTerritory,
                        WHSE AS Whse,
                        PartNumber,
                        '' AS FreeAccessory,
                        Qty,
                        IMEIESN,
                        HdwCost AS CostPrice,
                        HdwSellPrice AS SellPrice,
                        TopUpHdw AS TopUpOwing,
                        0 AS BVReceiptCost,
                        0 AS NetIMEIReceiveCost,
                        0 AS NetPriceProtection,
                        '' AS PONumber,
                        '' AS BVReceipt,
                        '' AS MISC_1
                    FROM tblRogersInvoiceAcquisitionOutputUSER
                    WHERE UserId = @UserId 
                      AND (
                        ([Group] = 'HDW' AND Fee <> 1 AND Fee <> 5 AND CustTerritory NOT IN ('HOF', 'RRT', 'HMS', 'RCT'))
                        OR
                        ([Group] <> 'HDW' AND CustTerritory NOT IN ('HOF', 'RRT', 'HMS', 'RCT'))
                      )";

                using (var cmd = new SqlCommand(sql, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new CostVerificationRow {
                                TransactionNo = Convert.ToInt32(reader.GetValue(0)).ToString(),
                                Invoice = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                InvoiceDate = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2),
                                CustName = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                CustTerritory = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                Whse = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                PartNumber = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                FreeAccessory = reader.IsDBNull(7) ? "" : reader.GetString(7),
                                Qty = reader.IsDBNull(8) ? 0 : Convert.ToInt32(Convert.ToDouble(reader.GetValue(8))),
                                IMEIESN = reader.IsDBNull(9) ? "" : reader.GetString(9),
                                CostPrice = reader.IsDBNull(10) ? 0.0 : Convert.ToDouble(reader.GetValue(10)),
                                SellPrice = reader.IsDBNull(11) ? 0.0 : Convert.ToDouble(reader.GetValue(11)),
                                TopUpOwing = reader.IsDBNull(12) ? 0.0 : Convert.ToDouble(reader.GetValue(12)),
                                BVReceiptCost = reader.IsDBNull(13) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(13)),
                                NetIMEIReceiveCost = reader.IsDBNull(14) ? 0.0 : Convert.ToDouble(reader.GetValue(14)),
                                NetPriceProtection = reader.IsDBNull(15) ? 0.0 : Convert.ToDouble(reader.GetValue(15)),
                                PONumber = reader.IsDBNull(16) ? "" : reader.GetString(16),
                                BVReceipt = reader.IsDBNull(17) ? "" : reader.GetString(17),
                                MISC_1 = reader.IsDBNull(18) ? "" : reader.GetString(18)
                            });
                        }
                    }
                }
                return list;
            }
        }

        public async Task<byte[]> GetRogersEstimateCsvAsync(int userId)
        {
            await EnsureTablesExistAsync();
            var dt = new System.Data.DataTable();
            using (var conn = new SqlConnection(_sqlConnStr))
            {
                await conn.OpenAsync();
                string sql = @"
                    SELECT * 
                    FROM tblRogersInvoiceAcquisitionOutputUSER 
                    WHERE UserId = @UserId 
                    ORDER BY ChannelName, Invoice, Id";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        dt.Load(reader);
                    }
                }
            }
            
            // Remove the internal columns from the export
            if (dt.Columns.Contains("UserId"))
            {
                dt.Columns.Remove("UserId");
            }
            if (dt.Columns.Contains("Id"))
            {
                dt.Columns.Remove("Id");
            }

            var csvBuilder = new System.Text.StringBuilder();
            
            // Headers
            var headers = new List<string>();
            foreach (System.Data.DataColumn col in dt.Columns)
            {
                headers.Add($@"""{col.ColumnName.Replace("\"", "\"\"")}""");
            }
            csvBuilder.AppendLine(string.Join(",", headers));

            // Rows
            foreach (System.Data.DataRow row in dt.Rows)
            {
                var fields = new List<string>();
                foreach (var item in row.ItemArray)
                {
                    string fieldStr = item == DBNull.Value || item == null ? "" : item.ToString();
                    fields.Add($@"""{fieldStr.Replace("\"", "\"\"")}""");
                }
                csvBuilder.AppendLine(string.Join(",", fields));
            }

            // Write BOM for Excel UTF-8 compatibility
            byte[] preamble = System.Text.Encoding.UTF8.GetPreamble();
            byte[] csvBytes = System.Text.Encoding.UTF8.GetBytes(csvBuilder.ToString());
            
            byte[] result = new byte[preamble.Length + csvBytes.Length];
            Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
            Buffer.BlockCopy(csvBytes, 0, result, preamble.Length, csvBytes.Length);

            return result;
        }
    }
}
