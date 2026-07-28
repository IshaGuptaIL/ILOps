using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace LegacyApp.DAL.Sales.RogersSalesReporting
{
    public class RogerSalesReportingDAL : IRogerSalesReportingDAL
    {
        private readonly string _connectionString;

        public RogerSalesReportingDAL(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("bvactivation_Connection");
        }

        public async Task<DataTable> ExecuteActionAsync(string endpoint, string actionType, string startDate, string endDate, string criteria, string territory, string userCreatedBy)
        {
            DataTable dt = new DataTable();

            // Build query based on endpoint and logic from VBA
            string sqlQuery = GetSqlQueryForEndpoint(endpoint, criteria, territory);

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(sqlQuery, conn))
                {
                    // 10 MINUTES TIMEOUT EXPLICITLY SET (600 seconds)
                    cmd.CommandTimeout = 600;

                    // Pass the raw yyyy-MM-dd string from Angular directly. 
                    // This ensures proper string comparison if the column is VARCHAR storing '2025-12-19 00:00:00'.
                    // And if the column is DATETIME, SQL Server implicitly converts '2025-12-01' perfectly.
                    cmd.Parameters.AddWithValue("@StartDate", startDate.Trim());
                    cmd.Parameters.AddWithValue("@EndDate", endDate.Trim());

                    if (criteria == "Specific Territory" && !string.IsNullOrEmpty(territory))
                    {
                        cmd.Parameters.AddWithValue("@Territory", territory);
                    }

                    await conn.OpenAsync();

                    // Standard ADO.NET execution (No Dapper)
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        dt.Load(reader);
                    }
                }
            }

            // Post-process DataTable to calculate 'Type' using exact VBA logic
            if (dt.Columns.Contains("VoicePlan") && dt.Columns.Contains("DataPlan"))
            {
                if (!dt.Columns.Contains("Type"))
                {
                    dt.Columns.Add("Type", typeof(string));
                }

                int idxCapHardware = dt.Columns.IndexOf("CAPHardware");
                int idxVoicePlan = dt.Columns.IndexOf("VoicePlan");
                int idxDataPlan = dt.Columns.IndexOf("DataPlan");
                int idxProductCode = dt.Columns.IndexOf("ProductCode");
                int idxDescription = dt.Columns.IndexOf("Description");
                int idxRecordType = dt.Columns.IndexOf("RecordType");
                int idxAdjustmentType = dt.Columns.IndexOf("AdjustmentType");
                int idxOrderNo = dt.Columns.IndexOf("OrderNo");
                int idxWebOrderID = dt.Columns.IndexOf("WebOrderID");
                int idxCapCost = dt.Columns.IndexOf("capcost");
                int idxCommissionVoice = dt.Columns.IndexOf("commissionvoice");
                int idxCommissionData = dt.Columns.IndexOf("commissiondata");
                int idxRecordTypeExtended = dt.Columns.IndexOf("RecordTypeExtended");

                foreach (DataRow row in dt.Rows)
                {
                    string capHardware = idxCapHardware >= 0 && row[idxCapHardware] != DBNull.Value ? row[idxCapHardware].ToString() : "";
                    string voice = idxVoicePlan >= 0 && row[idxVoicePlan] != DBNull.Value ? row[idxVoicePlan].ToString() : "";
                    string data = idxDataPlan >= 0 && row[idxDataPlan] != DBNull.Value ? row[idxDataPlan].ToString() : "";
                    string prodCode = idxProductCode >= 0 && row[idxProductCode] != DBNull.Value ? row[idxProductCode].ToString() : "";
                    string desc = idxDescription >= 0 && row[idxDescription] != DBNull.Value ? row[idxDescription].ToString() : "";
                    string recordType = idxRecordType >= 0 && row[idxRecordType] != DBNull.Value ? row[idxRecordType].ToString() : "";
                    string adjustmentType = idxAdjustmentType >= 0 && row[idxAdjustmentType] != DBNull.Value ? row[idxAdjustmentType].ToString() : "";
                    string orderNo = idxOrderNo >= 0 && row[idxOrderNo] != DBNull.Value ? row[idxOrderNo].ToString() : "";
                    string webOrderId = idxWebOrderID >= 0 && row[idxWebOrderID] != DBNull.Value ? row[idxWebOrderID].ToString() : "";

                    double capcost = idxCapCost >= 0 && row[idxCapCost] != DBNull.Value ? Convert.ToDouble(row[idxCapCost]) : 0;
                    double commissionvoice = idxCommissionVoice >= 0 && row[idxCommissionVoice] != DBNull.Value ? Convert.ToDouble(row[idxCommissionVoice]) : 0;
                    double commissiondata = idxCommissionData >= 0 && row[idxCommissionData] != DBNull.Value ? Convert.ToDouble(row[idxCommissionData]) : 0;

                    double hupFee = capcost * -1;
                    double voiceComm = commissionvoice * -1;
                    double dataComm = commissiondata * -1;

                    string recTypeExtended = idxRecordTypeExtended >= 0 && row[idxRecordTypeExtended] != DBNull.Value ? row[idxRecordTypeExtended].ToString() : "";

                    row["Type"] = DetermineType(capHardware, voice, data, prodCode, desc, recordType, adjustmentType, orderNo, webOrderId, hupFee, voiceComm, dataComm, recTypeExtended);
                }
            }

            return dt;
        }

        private string DetermineType(string capHardware, string voice, string data, string prodCode, string description, string recordType, string adjustmentType, string orderNumber, string webOrderId, double hupFee, double voiceComm, double dataComm, string recType2 = "")
        {
            string type = "";

            if (recType2 == "HUP") return "HUP";

            if (data.StartsWith("BB") || data.StartsWith("BV") || voice.StartsWith("BB") || voice.StartsWith("BV") || (voice.EndsWith("-V") && data.EndsWith("-D")))
            {
                return "Bundled";
            }

            if (!string.IsNullOrEmpty(capHardware)) return "HUP";

            if (voice.Contains("V4D")) return "Data";

            if (!string.IsNullOrEmpty(voice) || !string.IsNullOrEmpty(data))
            {
                if (!string.IsNullOrEmpty(voice) && string.IsNullOrEmpty(data)) type = "Voice";
                else if (string.IsNullOrEmpty(voice) && !string.IsNullOrEmpty(data)) type = "Data";
                else type = "Voice and Data";
            }
            else
            {
                if (!string.IsNullOrEmpty(capHardware)) type = "HUP";
                else
                {
                    string upperDesc = description.ToUpper();
                    if (upperDesc.Contains("LICENCE") || upperDesc.Contains("LICENSE") || prodCode.StartsWith("HLC")) type = "Licence";
                    else
                    {
                        if (!string.IsNullOrEmpty(recType2)) return recType2;
                        if (prodCode == "HCC") type = "Hardware";
                        else type = "Acc";
                    }
                }
            }

            if (type == "Hardware")
            {
                if (dataComm != 0 && voiceComm != 0) return "Voice and Data";
                if (voiceComm != 0) return "Voice";
                if (dataComm != 0) return "Data";
                if (hupFee != 0) return "HUP";
            }

            return type;
        }


        private string GetSqlQueryForEndpoint(string endpoint, string criteria, string territory)
        {
            // Base criteria logic replicating VBA exactly:
            string strCriteria = "";

            switch (criteria)
            {
                case "Begins with D":
                    strCriteria = " AND SalesActivations.CustTerritory LIKE 'D%'";
                    break;
                case "Begins with C":
                    strCriteria = " AND SalesActivations.CustTerritory LIKE 'C%'";
                    break;
                case "All Territories":
                    strCriteria = "";
                    break;
                case "Specific Territory":
                    strCriteria = " AND SalesActivations.CustTerritory = @Territory";
                    break;
                case "Missing or Invalid Territory":
                    strCriteria = " AND (SalesActivations.CustTerritory IS NULL OR LTRIM(RTRIM(ISNULL(SalesActivations.CustTerritory, ''))) = '')";
                    break;
                case "BCC-BSS-RDL-RDC-NIS-RIL":
                    strCriteria = " AND SalesActivations.CustTerritory IN ('BCC','BSS','RDL','RDC','NIS','RIL')";
                    break;
            }

            switch (endpoint)
            {
                case "any-territory-edit":
                    return $@"
                        SELECT 
                            SalesActivations.Invoice10, 
                            SalesActivations.TransactionNo, 
                            SalesActivations.InvoiceDate, 
                            SalesActivations.OrderDate, 
                            SalesActivations.CustName, 
                            SalesActivations.CustTerritory, 
                            SalesActivations.UserName, 
                            SalesActivations.CellPhoneNo, 
                            SalesActivations.VoicePlan, 
                            SalesActivations.DataPlan, 
                            SalesActivations.WebOrderID, 
                            -- Type column will be calculated in C# post-processing
                            SalesActivations.AdjustmentType, 
                            SalesActivations.Supress, 
                            SalesActivations.Fee, 
                            CASE WHEN Fee>0 THEN 1 WHEN fee<0 THEN -1 ELSE 0 END AS FeeCount, 
                            CASE WHEN ABS(itemsellprice*Qty)>ABS(itemcost*Qty) THEN 0 ELSE (itemcost*Qty)-(itemsellprice*Qty) END AS TopUpOwing, 
                            SalesActivations.TopUpSDFAcc, 
                            TopUpSDFAccCost*-1 AS TopUpSDFAccCostAdjusted, 
                            SalesActivations.TopUpSDF, 
                            TopUpSDFCost*-1 AS TopUpSDFCostAdjusted, 
                            SalesActivations.TopUpSDFLic, 
                            TopUpSDFLicCost*-1 AS TopUpSDFLicCostAdjusted, 
                            SalesActivations.OriginalInvoice, 
                            SalesActivations.Qty, 
                            SalesActivations.whse, 
                            SalesActivations.PartNumber, 
                            SalesActivations.ProductCode, 
                            SalesActivations.FreeAccessory, 
                            SalesActivations.FreeAccessoryPart, 
                            SalesActivations.IMEIESN, 
                            itemcost*qty AS CostPrice, 
                            itemsellprice*Qty AS SellPrice, 
                            SalesActivations.AccessoryCost, 
                            SalesActivations.AccessoryPrice, 
                            SalesActivations.CAPHardware, 
                            SalesActivations.BVInvoiceLine, 
                            SalesActivations.InvoiceNet, 
                            SalesActivations.InvoiceShipping, 
                            SalesActivations.InvoiceTaxes, 
                            SalesActivations.InvoiceTotal, 
                            SalesActivations.PayMeth, 
                            SalesActivations.TermsText, 
                            SalesActivations.Channel, 
                            SalesActivations.SCOA, 
                            SalesActivations.Customer, 
                            SalesActivations.SIMCardNo,
                            SalesActivations.capcost,
                            SalesActivations.commissionvoice,
                            SalesActivations.commissiondata,
                            SalesActivations.RecordTypeExtended,
                            SalesActivations.RecordType,
                            SalesActivations.Description,
                            SalesActivations.OrderNo,
                            -- Department columns (add these with default 0 values for now)
                            0.00 AS CoOpAdvertisingHO,
                            0.00 AS MiscellaneousGBMNDSIncExp,
                            0.00 AS OtherRevenueHO,
                            0.00 AS OtherRevenueCO,
                            0.00 AS ReceivableUpfrontEdgeRV,
                            0.00 AS SalesAccessoriesCO,
                            0.00 AS SalesHardwareCO,
                            0.00 AS StagingAndDeployment,
                            0.00 AS UnallocatedSales,
                            0.00 AS WebHosting
                        FROM SalesActivations
                        WHERE (SalesActivations.InvoiceDate BETWEEN @StartDate AND @EndDate)
                        {strCriteria}
                        ORDER BY SalesActivations.Invoice10";

                case "any-territory-output":
                    return $@"
                        SELECT 
                            SalesActivations.Invoice10, 
                            SalesActivations.TransactionNo, 
                            SalesActivations.InvoiceDate,
                            SalesActivations.CustName, 
                            SalesActivations.CustTerritory,
                            SalesActivations.Fee,
                            SalesActivations.InvoiceTotal,
                            -- Department columns
                            0.00 AS CoOpAdvertisingHO,
                            0.00 AS MiscellaneousGBMNDSIncExp,
                            0.00 AS OtherRevenueHO,
                            0.00 AS OtherRevenueCO,
                            0.00 AS ReceivableUpfrontEdgeRV,
                            0.00 AS SalesAccessoriesCO,
                            0.00 AS SalesHardwareCO,
                            0.00 AS StagingAndDeployment,
                            0.00 AS UnallocatedSales,
                            0.00 AS WebHosting
                        FROM SalesActivations
                        WHERE (SalesActivations.InvoiceDate BETWEEN @StartDate AND @EndDate)
                        AND (SalesActivations.Fee <> 0 OR SalesActivations.Supress = 0)
                        {strCriteria}
                        ORDER BY SalesActivations.Invoice10";

                case "all-except-corporate-edit":
                    return $@"
                        SELECT 
                            SalesActivations.Invoice10, 
                            SalesActivations.TransactionNo, 
                            SalesActivations.InvoiceDate, 
                            SalesActivations.OrderDate, 
                            SalesActivations.CustName, 
                            SalesActivations.CustTerritory, 
                            SalesActivations.UserName, 
                            SalesActivations.CellPhoneNo, 
                            SalesActivations.VoicePlan, 
                            SalesActivations.DataPlan, 
                            SalesActivations.WebOrderID, 
                            -- Type column will be calculated in C# post-processing
                            SalesActivations.AdjustmentType, 
                            SalesActivations.Supress, 
                            SalesActivations.Fee, 
                            CASE WHEN Fee>0 THEN 1 WHEN fee<0 THEN -1 ELSE 0 END AS FeeCount, 
                            CASE WHEN ABS(itemsellprice*Qty)>ABS(itemcost*Qty) THEN 0 ELSE (itemcost*Qty)-(itemsellprice*Qty) END AS TopUpOwing, 
                            SalesActivations.TopUpSDFAcc, 
                            TopUpSDFAccCost*-1 AS TopUpSDFAccCostAdjusted, 
                            SalesActivations.TopUpSDF, 
                            TopUpSDFCost*-1 AS TopUpSDFCostAdjusted, 
                            SalesActivations.TopUpSDFLic, 
                            TopUpSDFLicCost*-1 AS TopUpSDFLicCostAdjusted, 
                            SalesActivations.OriginalInvoice, 
                            SalesActivations.Qty, 
                            SalesActivations.whse, 
                            SalesActivations.PartNumber, 
                            SalesActivations.ProductCode, 
                            SalesActivations.FreeAccessory, 
                            SalesActivations.FreeAccessoryPart, 
                            SalesActivations.IMEIESN, 
                            itemcost*qty AS CostPrice, 
                            itemsellprice*Qty AS SellPrice, 
                            SalesActivations.AccessoryCost, 
                            SalesActivations.AccessoryPrice, 
                            SalesActivations.CAPHardware, 
                            SalesActivations.BVInvoiceLine, 
                            SalesActivations.InvoiceNet, 
                            SalesActivations.InvoiceShipping, 
                            SalesActivations.InvoiceTaxes, 
                            SalesActivations.InvoiceTotal, 
                            SalesActivations.PayMeth, 
                            SalesActivations.TermsText, 
                            SalesActivations.Channel, 
                            SalesActivations.SCOA, 
                            SalesActivations.Customer, 
                            SalesActivations.SIMCardNo,
                            SalesActivations.capcost,
                            SalesActivations.commissionvoice,
                            SalesActivations.commissiondata,
                            SalesActivations.RecordTypeExtended,
                            SalesActivations.RecordType,
                            SalesActivations.Description,
                            SalesActivations.OrderNo,
                            -- Department columns (add these with default 0 values for now)
                            0.00 AS CoOpAdvertisingHO,
                            0.00 AS MiscellaneousGBMNDSIncExp,
                            0.00 AS OtherRevenueHO,
                            0.00 AS OtherRevenueCO,
                            0.00 AS ReceivableUpfrontEdgeRV,
                            0.00 AS SalesAccessoriesCO,
                            0.00 AS SalesHardwareCO,
                            0.00 AS StagingAndDeployment,
                            0.00 AS UnallocatedSales,
                            0.00 AS WebHosting
                        FROM SalesActivations
                        WHERE (SalesActivations.InvoiceDate BETWEEN @StartDate AND @EndDate)
                        AND SalesActivations.CustTerritory NOT LIKE 'D%'
                        ORDER BY SalesActivations.Invoice10";

                case "dump-all":
                    return $@"
                        SELECT 
                            SalesActivations.*,
                            -- Department columns
                            0.00 AS CoOpAdvertisingHO,
                            0.00 AS MiscellaneousGBMNDSIncExp,
                            0.00 AS OtherRevenueHO,
                            0.00 AS OtherRevenueCO,
                            0.00 AS ReceivableUpfrontEdgeRV,
                            0.00 AS SalesAccessoriesCO,
                            0.00 AS SalesHardwareCO,
                            0.00 AS StagingAndDeployment,
                            0.00 AS UnallocatedSales,
                            0.00 AS WebHosting
                        FROM SalesActivations
                        WHERE (SalesActivations.InvoiceDate BETWEEN @StartDate AND @EndDate)
                        {strCriteria}
                        ORDER BY SalesActivations.Invoice10";

                case "invoices-missing-summary":
                    return $@"
                        SELECT 
                            HISTORYFULL.H_INV_PO_NO AS Invoice10, 
                            HISTORYFULL.H_CUST_SUPP_NO AS CustNo, 
                            HISTORYFULL.HI_SALES_PERSON AS SalesPerson, 
                            HISTORYFULL.H_REC_NO AS RecNo, 
                            HISTORYFULL.H_INV_PO_DATE AS InvoiceDate, 
                            HISTORYFULL.HI_PMT_METHOD AS PayMethod, 
                            HISTORYFULL.H_TOTAL_NET AS InvoiceNet, 
                            HISTORYFULL.H_TOTAL_TAX01 AS Tax1, 
                            HISTORYFULL.H_TOTAL_TAX02 AS Tax2, 
                            HISTORYFULL.H_TOTAL_AMOUNT AS InvoiceTotal,
                            -- Department columns
                            0.00 AS CoOpAdvertisingHO,
                            0.00 AS MiscellaneousGBMNDSIncExp,
                            0.00 AS OtherRevenueHO,
                            0.00 AS OtherRevenueCO,
                            0.00 AS ReceivableUpfrontEdgeRV,
                            0.00 AS SalesAccessoriesCO,
                            0.00 AS SalesHardwareCO,
                            0.00 AS StagingAndDeployment,
                            0.00 AS UnallocatedSales,
                            0.00 AS WebHosting
                        FROM HISTORYFULL 
                        LEFT JOIN SalesActivations ON HISTORYFULL.H_INV_PO_NO = SalesActivations.Invoice 
                        WHERE HISTORYFULL.HI_SALES_PERSON LIKE 'C%' 
                        AND HISTORYFULL.H_REC_NO = '000' 
                        AND HISTORYFULL.H_INV_PO_DATE BETWEEN FORMAT(@StartDate, 'yyyyMMdd') AND FORMAT(@EndDate, 'yyyyMMdd')
                        AND HISTORYFULL.HI_PMT_METHOD = '0' 
                        AND HISTORYFULL.H_TYPE_KEY LIKE '%I%' 
                        AND SalesActivations.Invoice IS NULL";
                case "i-nv-oi-ce-sn-ot-in-re-po-rt-in-gd-et-ai-l":
                    return $@"
                        SELECT HISTORYFULL.H_INV_PO_NO, HISTORYFULL.H_CUST_SUPP_NO, HISTORYFULL.HI_SALES_PERSON, HISTORYFULL.H_REC_NO, HISTORYFULL_1.H_REC_NO, HISTORYFULL.H_INV_PO_DATE, HISTORYFULL.HI_PMT_METHOD, HISTORYFULL.H_TOTAL_NET, HISTORYFULL.H_TOTAL_TAX01, HISTORYFULL.H_TOTAL_TAX02, HISTORYFULL.H_TOTAL_AMOUNT, HISTORYFULL_1.H_PART_NO, HISTORYFULL_1.H_DESCRIPTION, HISTORYFULL_1.H_PROD_CODE, HISTORYFULL_1.HI_SHIPPED_QTY, HISTORYFULL_1.HI_UNIT_PRICE, HISTORYFULL_1.HI_COST_PRICE
FROM (HISTORYFULL INNER JOIN HISTORYFULL AS HISTORYFULL_1 ON (HISTORYFULL.H_INV_PO_NO=HISTORYFULL_1.H_INV_PO_NO) AND (HISTORYFULL.H_TYPE_KEY=HISTORYFULL_1.H_TYPE_KEY)) LEFT JOIN SalesActivations ON HISTORYFULL_1.H_INV_PO_NO=SalesActivations.Invoice
WHERE (((HISTORYFULL.HI_SALES_PERSON) Like 'C%') And ((HISTORYFULL.H_REC_NO)='000') And ((HISTORYFULL.H_INV_PO_DATE) Between '20110509' And '20110515') And ((HISTORYFULL.HI_PMT_METHOD)='0') And ((HISTORYFULL.H_TYPE_KEY) LIKE '%I%') And ((SalesActivations.Invoice) Is Null));";

                case "q-ry-ex-ce-pt-io-nr-ep-or-t":
                    return $@"
                        SELECT SalesActivations.InvoiceDate, SalesActivations.Invoice, SalesActivations.Customer, SalesActivations.CustName, SalesActivations.CustTerritory, SalesActivations.UserInitials, SalesActivations.TermsText, CASE WHEN [PayMeth]='0' AND invoicetotal<>0 THEN invoicetotal ELSE NULL END AS ARAmount, dbo_t_orderimport.hardware_payment_methodID, tblPaymetnTerms.PaymentTermsText, SalesActivations.AdjustmentType, SalesActivations.OriginalInvoice, SalesActivations.InvoiceTotal, CASE WHEN ISNULL(custterritory, '') = '' THEN 'ERROR' ELSE '' END AS MissingTerritory, CASE WHEN ISNULL(TermsText, '') = '' THEN 'ERROR' ELSE '' END AS MissingTerms, CASE WHEN (TermsText='CREDIT CARD' OR TermsText='V21 Account') AND (CASE WHEN PayMeth='0' AND invoicetotal<>0 THEN invoicetotal ELSE 0 END)<>0 THEN 'ERROR' ELSE '' END AS ConflictTermsAndTender, SalesActivations.Supress
FROM (SalesActivations LEFT JOIN dbo_t_orderimport ON SalesActivations.OrderNo=dbo_t_orderimport.imported) LEFT JOIN tblPaymetnTerms ON dbo_t_orderimport.hardware_payment_methodID=tblPaymetnTerms.code
WHERE (((SalesActivations.InvoiceDate) Between @StartDate And @EndDate) And ((SalesActivations.CustTerritory) Not Like 'D%' And ((SalesActivations.CustTerritory)='' Or (SalesActivations.CustTerritory) Is Null))) Or (((SalesActivations.InvoiceDate) Between @StartDate And @EndDate) And ((SalesActivations.CustTerritory) Not Like 'D%') And ((SalesActivations.TermsText)='' Or (SalesActivations.TermsText) Is Null) And ((dbo_t_orderimport.hardware_payment_methodID)<>'0')) Or (((SalesActivations.InvoiceDate) Between @StartDate And @EndDate) And ((SalesActivations.CustTerritory) Not Like 'D%') And ((SalesActivations.TermsText)='CREDIT CARD' Or (SalesActivations.TermsText)='V21 Account') And ((CASE WHEN [PayMeth]='0' AND invoicetotal<>0 THEN invoicetotal ELSE NULL END)<>0)) Or (((SalesActivations.InvoiceDate) Between @StartDate And @EndDate) And ((SalesActivations.CustTerritory) Not Like 'D%') And ((dbo_t_orderimport.hardware_payment_methodID)='0') And ((SalesActivations.InvoiceTotal)<>0))
ORDER BY SalesActivations.InvoiceDate;";

                case "l-os-so-nh-ar-dw-ar-e":
                    return $@"
                        SELECT SalesActivations.InvoiceDate, SalesActivations.CustTerritory, SalesActivations.Invoice, SalesActivations.OriginalInvoice, SalesActivations.AdjustmentType, SalesActivations.OrderNo, SalesActivations.RecordType, SalesActivations.RecordTypeExtended, SalesActivations.PartNumber, SalesActivations.Qty, SalesActivations.ItemCost, SalesActivations.ItemSellPrice, SalesActivations.TopUpSDF, SalesActivations.TopUpSDFCost, Round(ISNULL(((qty*itemcost)-(qty*itemsellprice)+topupsdfcost)*qty,0),2) AS Loss, SalesActivations.Fee, SalesActivations.Supress
FROM SalesActivations
WHERE (((SalesActivations.InvoiceDate) Between @StartDate And @EndDate) AND ((SalesActivations.CustTerritory) Like 'C%') AND ((Round(ISNULL(((qty*itemcost)-(qty*itemsellprice)+topupsdfcost)*qty,0),2))<>0 And (Round(ISNULL(((qty*itemcost)-(qty*itemsellprice)+topupsdfcost)*qty,0),2)) Is Not Null) AND ((Abs(Round(ISNULL(((qty*itemcost)-(qty*itemsellprice)+topupsdfcost)*qty,0),2)))>0.01))
ORDER BY SalesActivations.InvoiceDate, SalesActivations.Invoice;";

                case "r-og-er-sd-ai-ly2e-di-ta-ll":
                    return $@"
                        SELECT SalesActivations.Invoice, SalesActivations.TransactionNo, SalesActivations.InvoiceDate, SalesActivations.OrderDate, SalesActivations.CustName, SalesActivations.CustTerritory, SalesActivations.UserName, SalesActivations.CellPhoneNo, SalesActivations.VoicePlan, SalesActivations.DataPlan, SalesActivations.WebOrderID, '' AS Type, SalesActivations.AdjustmentType, SalesActivations.Supress, SalesActivations.Fee, CASE WHEN [Fee]>0 THEN 1 WHEN [fee]<0 THEN -1 ELSE 0 END AS FeeCount, CASE WHEN ABS(sellprice)>ABS(costprice) THEN 0 ELSE costprice-sellprice END AS TopUpOwing, SalesActivations.TopUpSDFAcc, SalesActivations.TopUpSDF, [TopUpSDFCost]*-1 AS TopUpSDFCostAdjusted, SalesActivations.TopUpSDFLic, [TopUpSDFLicCost]*-1 AS TopUpSDFLicCostAdjusted, SalesActivations.OriginalInvoice, SalesActivations.Qty, SalesActivations.PartNumber, SalesActivations.ProductCode, SalesActivations.FreeAccessory, SalesActivations.FreeAccessoryPart, SalesActivations.IMEIESN, [itemcost]*[qty] AS CostPrice, [itemsellprice]*[Qty] AS SellPrice, SalesActivations.AccessoryCost, SalesActivations.AccessoryPrice, [TopUpSDFAccCost]*-1 AS TopUpSDFAccCostAdjusted, SalesActivations.CAPHardware, SalesActivations.BVInvoiceLine, SalesActivations.InvoiceNet, SalesActivations.InvoiceShipping, SalesActivations.InvoiceTaxes, SalesActivations.InvoiceTotal, SalesActivations.PayMeth, SalesActivations.TermsText, SalesActivations.Channel, SalesActivations.SCOA, SalesActivations.Customer, SalesActivations.SIMCardNo, SalesActivations.RMANumber
FROM SalesActivations
WHERE (((SalesActivations.InvoiceDate)>=@StartDate And (SalesActivations.InvoiceDate)<=@EndDate) AND ((SalesActivations.CustTerritory) Like 'C%'))
ORDER BY SalesActivations.Invoice;";

                case "r-og-er-sd-ai-ly2e-di-ta-ll-w-it-hp-ro-vi-nc-e":
                    return $@"
                        SELECT SalesActivations.Invoice, SalesActivations.InvoiceDate, SalesActivations.OrderDate, SalesActivations.CustName, SalesActivations.CustTerritory, SalesActivations.UserName, SalesActivations.CellPhoneNo, SalesActivations.VoicePlan, SalesActivations.DataPlan, SalesActivations.WebOrderID, '' AS Type, SalesActivations.AdjustmentType, SalesActivations.Supress, SalesActivations.Fee, CASE WHEN [Fee]>0 THEN 1 WHEN [fee]<0 THEN -1 ELSE 0 END AS FeeCount, CASE WHEN ABS(sellprice)>ABS(costprice) THEN 0 ELSE costprice-sellprice END AS TopUpOwing, SalesActivations.TopUpSDFAcc, SalesActivations.TopUpSDFLic, SalesActivations.TopUpSDFLicCost, SalesActivations.OriginalInvoice, SalesActivations.Qty, SalesActivations.PartNumber, SalesActivations.ProductCode, SalesActivations.FreeAccessory, SalesActivations.FreeAccessoryPart, SalesActivations.IMEIESN, [itemcost]*[qty] AS CostPrice, [itemsellprice]*[Qty] AS SellPrice, SalesActivations.AccessoryCost, SalesActivations.AccessoryPrice, [TopUpSDFAccCost]*-1 AS TopUpSDFAccCostAdjusted, SalesActivations.CAPHardware, SalesActivations.BVInvoiceLine, SalesActivations.InvoiceNet, SalesActivations.InvoiceShipping, SalesActivations.InvoiceTaxes, SalesActivations.InvoiceTotal, SalesActivations.PayMeth, SalesActivations.TermsText, SalesActivations.Channel, SalesActivations.SCOA, DLookUp('[Province]','[PostalProvince]','[firstdigit] = '' & Left([shiptopostal],1) & ''') AS ShipToProvince
FROM SalesActivations
WHERE (((SalesActivations.InvoiceDate)>=@StartDate And (SalesActivations.InvoiceDate)<=@EndDate) AND ((SalesActivations.CustTerritory) Like 'C%'))
ORDER BY SalesActivations.Invoice;";

                case "r-og-er-sd-ai-ly-gb-mn-ds-ed-it":
                    return $@"
                        SELECT SalesActivations.Invoice, SalesActivations.InvoiceDate, SalesActivations.CustName, SalesActivations.CustTerritory, SalesActivations.UserName, SalesActivations.CellPhoneNo, SalesActivations.VoicePlan, SalesActivations.DataPlan, SalesActivations.WebOrderID, '' AS Type, SalesActivations.AdjustmentType, SalesActivations.Supress, SalesActivations.OriginalInvoice, SalesActivations.Qty, SalesActivations.PartNumber, SalesActivations.Description, SalesActivations.ProductCode, SalesActivations.FreeAccessory, SalesActivations.IMEIESN, [itemcost]*[qty] AS CostPrice, [itemsellprice]*[Qty] AS SellPrice, SalesActivations.AccessoryCost, SalesActivations.Fee, SalesActivations.FeeType, SalesActivations.TopUpSDFAcc, [TopUpSDFAccCost]*-1 AS TopUpSDFAccCostAdjusted, SalesActivations.TopUpSDFLic, SalesActivations.TopUpSDFLicCost, SalesActivations.CAPHardware, SalesActivations.GSTRate, SalesActivations.PSTRate, SalesActivations.GSTFlag, SalesActivations.PSTFlag, SalesActivations.PayMeth, SalesActivations.TermsText
FROM SalesActivations
WHERE (((SalesActivations.InvoiceDate)>=@StartDate And (SalesActivations.InvoiceDate)<=@EndDate) And ((SalesActivations.CustTerritory)=[Enter Territory GBM or NDS]));";

                case "g-bm-ed-it":
                    return $@"
                        SELECT SalesActivations.Invoice, SalesActivations.InvoiceDate, SalesActivations.OrderDate, SalesActivations.CustName, SalesActivations.CustTerritory, SalesActivations.UserName, SalesActivations.CellPhoneNo, SalesActivations.VoicePlan, SalesActivations.DataPlan, SalesActivations.WebOrderID, '' AS Type, SalesActivations.AdjustmentType, SalesActivations.Supress, SalesActivations.Fee, CASE WHEN ABS(sellprice)>ABS(costprice) THEN 0 ELSE costprice-sellprice END AS TopUpOwing, SalesActivations.TopUpSDFAcc, SalesActivations.TopUpSDFLic, SalesActivations.TopUpSDFLicCost, SalesActivations.OriginalInvoice, SalesActivations.Qty, SalesActivations.PartNumber, SalesActivations.ProductCode, SalesActivations.FreeAccessory, SalesActivations.IMEIESN, [itemcost]*[qty] AS CostPrice, [itemsellprice]*[Qty] AS SellPrice, SalesActivations.AccessoryCost, [TopUpSDFAccCost]*-1 AS TopUpSDFAccCostAdjusted, SalesActivations.CAPHardware, SalesActivations.BVInvoiceLine, SalesActivations.PayMeth, SalesActivations.TermsText, SalesActivations.InvoiceTotal
FROM SalesActivations
WHERE (((SalesActivations.InvoiceDate)>=@StartDate And (SalesActivations.InvoiceDate)<=@EndDate) AND ((SalesActivations.CustTerritory)='GBM'))
ORDER BY SalesActivations.Invoice;";

                case "n-ds-ed-it":
                    return $@"
                        SELECT SalesActivations.Invoice, SalesActivations.TransactionNo, SalesActivations.InvoiceDate, SalesActivations.OrderDate, SalesActivations.CustName, SalesActivations.CustTerritory, SalesActivations.UserName, SalesActivations.CellPhoneNo, SalesActivations.VoicePlan, SalesActivations.DataPlan, SalesActivations.WebOrderID, '' AS Type, SalesActivations.AdjustmentType, SalesActivations.Supress, SalesActivations.Fee, CASE WHEN ABS(sellprice)>ABS(costprice) THEN 0 ELSE costprice-sellprice END AS TopUpOwing, SalesActivations.TopUpSDFAcc, [TopUpSDFAccCost]*-1 AS TopUpSDFAccCostAdjusted, SalesActivations.TopUpSDF, [TopUpSDFCost]*-1 AS TopUpSDFCostAdjusted, SalesActivations.TopUpSDFLic, [TopUpSDFLicCost]*-1 AS TopUpSDFLicCostAdjusted, SalesActivations.OriginalInvoice, SalesActivations.Qty, SalesActivations.PartNumber, SalesActivations.ProductCode, SalesActivations.FreeAccessory, SalesActivations.IMEIESN, [itemcost]*[qty] AS CostPrice, [itemsellprice]*[Qty] AS SellPrice, SalesActivations.AccessoryCost, SalesActivations.CAPHardware, SalesActivations.BVInvoiceLine, SalesActivations.Channel, SalesActivations.PayMeth, SalesActivations.TermsText, SalesActivations.Customer, SalesActivations.SIMCardNo
FROM SalesActivations
WHERE (((SalesActivations.InvoiceDate)>=@StartDate And (SalesActivations.InvoiceDate)<=@EndDate) AND ((SalesActivations.CustTerritory)='NDS'))
ORDER BY SalesActivations.Invoice;";

                case "r-dl-ed-it":
                    return $@"
                        SELECT SalesActivations.Invoice, SalesActivations.TransactionNo, SalesActivations.InvoiceDate, SalesActivations.OrderDate, SalesActivations.Customer, SalesActivations.CustName, SalesActivations.CustTerritory, SalesActivations.UserName, SalesActivations.CellPhoneNo, SalesActivations.VoicePlan, SalesActivations.DataPlan, SalesActivations.WebOrderID, '' AS Type, SalesActivations.AdjustmentType, SalesActivations.Supress, SalesActivations.Fee, CASE WHEN ABS(sellprice)>ABS(costprice) THEN 0 ELSE costprice-sellprice END AS TopUpOwing, SalesActivations.TopUpSDFAcc, [TopUpSDFAccCost]*-1 AS TopUpSDFAccCostAdjusted, SalesActivations.TopUpSDF, [TopUpSDFCost]*-1 AS TopUpSDFCostAdjusted, SalesActivations.TopUpSDFLic, [TopUpSDFLicCost]*-1 AS TopUpSDFLicCostAdjusted, SalesActivations.OriginalInvoice, SalesActivations.Qty, SalesActivations.PartNumber, SalesActivations.ProductCode, SalesActivations.FreeAccessory, SalesActivations.IMEIESN, [itemcost]*[qty] AS CostPrice, [itemsellprice]*[Qty] AS SellPrice, SalesActivations.AccessoryCost, SalesActivations.CAPHardware, SalesActivations.BVInvoiceLine, SalesActivations.PayMeth, SalesActivations.TermsText, SalesActivations.InvoiceTotal, SalesActivations.SIMCardNo
FROM SalesActivations
WHERE (((SalesActivations.InvoiceDate)>=@StartDate And (SalesActivations.InvoiceDate)<=@EndDate) AND ((SalesActivations.CustTerritory)='RDL'))
ORDER BY SalesActivations.Invoice;";

                case "r-og-er-sd-ai-ly2h-up-an-dr-es-t-d-ir-ec-t-o-ut-pu-t":
                    return $@"
                        SELECT [RogersDaily2HUPandREST-Direct].Invoice, [RogersDaily2HUPandREST-Direct].InvoiceDate, [RogersDaily2HUPandREST-Direct].OrderDate, [RogersDaily2HUPandREST-Direct].OrderNo, [RogersDaily2HUPandREST-Direct].CustName, [RogersDaily2HUPandREST-Direct].CustTerritory, [RogersDaily2HUPandREST-Direct].MSD, [RogersDaily2HUPandREST-Direct].UserName, [RogersDaily2HUPandREST-Direct].CellPhoneNo, [RogersDaily2HUPandREST-Direct].VoicePlan, [RogersDaily2HUPandREST-Direct].DataPlan, [RogersDaily2HUPandREST-Direct].WebOrderID, [RogersDaily2HUPandREST-Direct].Type, UPPER(ISNULL([AdjustmentType],'')) AS AdjustType, [RogersDaily2HUPandREST-Direct].Supress, [RogersDaily2HUPandREST-Direct].Fee, CASE WHEN [Fee]>0 THEN 1 WHEN [fee]<0 THEN -1 ELSE 0 END AS FeeCount, [RogersDaily2HUPandREST-Direct].TopUpSDFAccCostAdjusted, [RogersDaily2HUPandREST-Direct].TopUpSDFLicCost, [RogersDaily2HUPandREST-Direct].OriginalInvoice, [RogersDaily2HUPandREST-Direct].Qty, [RogersDaily2HUPandREST-Direct].PartNumber, [RogersDaily2HUPandREST-Direct].ProductCode, [RogersDaily2HUPandREST-Direct].FreeAccessory, [RogersDaily2HUPandREST-Direct].IMEIESN, [RogersDaily2HUPandREST-Direct].CostPrice, [RogersDaily2HUPandREST-Direct].SellPrice, [RogersDaily2HUPandREST-Direct].TopUpOwing, [RogersDaily2HUPandREST-Direct].AccessoryCost, [RogersDaily2HUPandREST-Direct].AccessoryPrice, [RogersDaily2HUPandREST-Direct].TopUpSDFAcc, [RogersDaily2HUPandREST-Direct].TopUpSDFLic, [RogersDaily2HUPandREST-Direct].CAPHardware, [RogersDaily2HUPandREST-Direct].BVInvoiceLine, [RogersDaily2HUPandREST-Direct].Channel, [RogersDaily2HUPandREST-Direct].PayMeth, [RogersDaily2HUPandREST-Direct].CustomerPostalFirstDigit, [RogersDaily2HUPandREST-Direct].ARAmount
FROM [RogersDaily2HUPandREST-Direct]
WHERE (((UPPER(ISNULL([AdjustmentType],''))) Not LIKE '%RETURN%') AND (([RogersDaily2HUPandREST-Direct].Supress)=No) AND (([RogersDaily2HUPandREST-Direct].Fee)<>0)) OR (((UPPER(ISNULL([AdjustmentType],''))) Not LIKE '%RETURN%') AND (([RogersDaily2HUPandREST-Direct].Supress)=No) AND (([RogersDaily2HUPandREST-Direct].TopUpSDFAccCostAdjusted)<>0)) OR (((UPPER(ISNULL([AdjustmentType],''))) Not LIKE '%RETURN%') AND (([RogersDaily2HUPandREST-Direct].Supress)=No) AND (([RogersDaily2HUPandREST-Direct].TopUpSDFLicCost)<>0)) OR (((UPPER(ISNULL([AdjustmentType],''))) Not LIKE '%RETURN%') AND (([RogersDaily2HUPandREST-Direct].Supress)=No) AND (([RogersDaily2HUPandREST-Direct].ARAmount)<>0))
ORDER BY [RogersDaily2HUPandREST-Direct].Invoice;";

                case "r-og-er-sd-ai-ly2a-ct-iv-at-io-ns-d-ir-ec-t-o-ut-pu-t":
                    return $@"
                        SELECT [RogersDaily2Activations-Direct].Invoice, [RogersDaily2Activations-Direct].InvoiceDate, [RogersDaily2Activations-Direct].OrderDate, [RogersDaily2Activations-Direct].OrderNo, [RogersDaily2Activations-Direct].CustName, [RogersDaily2Activations-Direct].CustTerritory, [RogersDaily2Activations-Direct].MSD, [RogersDaily2Activations-Direct].UserName, [RogersDaily2Activations-Direct].CellPhoneNo, [RogersDaily2Activations-Direct].VoicePlan, [RogersDaily2Activations-Direct].DataPlan, [RogersDaily2Activations-Direct].WebOrderID, [RogersDaily2Activations-Direct].Type, UPPER(ISNULL([AdjustmentType],'')) AS AdjustType, [RogersDaily2Activations-Direct].Supress, [RogersDaily2Activations-Direct].OriginalInvoice, [RogersDaily2Activations-Direct].Qty, [RogersDaily2Activations-Direct].PartNumber, [RogersDaily2Activations-Direct].ProductCode, [RogersDaily2Activations-Direct].FreeAccessory, [RogersDaily2Activations-Direct].IMEIESN, [RogersDaily2Activations-Direct].CostPrice, [RogersDaily2Activations-Direct].SellPrice, [RogersDaily2Activations-Direct].AccessoryCost, [RogersDaily2Activations-Direct].AccessoryPrice, [RogersDaily2Activations-Direct].Fee, CASE WHEN [Fee]>0 THEN 1 WHEN [fee]<0 THEN -1 ELSE 0 END AS FeeCount, [RogersDaily2Activations-Direct].TopUpSDFAccCostAdjusted, [RogersDaily2Activations-Direct].TopUpOwing, [RogersDaily2Activations-Direct].TopUpSDFLicCost, [RogersDaily2Activations-Direct].Channel, [RogersDaily2Activations-Direct].CustomerPostalFirstDigit, [RogersDaily2Activations-Direct].PayMeth, [RogersDaily2Activations-Direct].ARAmount
FROM [RogersDaily2Activations-Direct]
WHERE (((UPPER(ISNULL([AdjustmentType],''))) Not LIKE '%RETURN%') AND (([RogersDaily2Activations-Direct].Supress)=No) AND (([RogersDaily2Activations-Direct].Fee)<>0)) OR (((UPPER(ISNULL([AdjustmentType],''))) Not LIKE '%RETURN%') AND (([RogersDaily2Activations-Direct].Supress)=No) AND (([RogersDaily2Activations-Direct].TopUpSDFAccCostAdjusted)<>0)) OR (((UPPER(ISNULL([AdjustmentType],''))) Not LIKE '%RETURN%') AND (([RogersDaily2Activations-Direct].Supress)=No) AND (([RogersDaily2Activations-Direct].TopUpOwing)<>0)) OR (((UPPER(ISNULL([AdjustmentType],''))) Not LIKE '%RETURN%') AND (([RogersDaily2Activations-Direct].Supress)=No) AND (([RogersDaily2Activations-Direct].TopUpSDFLicCost)<>0)) OR (((UPPER(ISNULL([AdjustmentType],''))) Not LIKE '%RETURN%') AND (([RogersDaily2Activations-Direct].Supress)=No) AND (([RogersDaily2Activations-Direct].ARAmount)<>0))
ORDER BY [RogersDaily2Activations-Direct].Invoice;";

                case "r-og-er-sd-ai-ly2r-et-ur-ns-o-ut-pu-t":
                    return $@"
                        SELECT RogersDaily2RETURNS.Invoice AS Expr1, RogersDaily2RETURNS.InvoiceDate AS Expr2, RogersDaily2RETURNS.OrderDate AS Expr3, RogersDaily2RETURNS.OrderNo AS Expr4, RogersDaily2RETURNS.CustName AS Expr5, RogersDaily2RETURNS.CustTerritory AS Expr6, RogersDaily2RETURNS.MSD AS Expr7, RogersDaily2RETURNS.UserName AS Expr8, RogersDaily2RETURNS.CellPhoneNo AS Expr9, RogersDaily2RETURNS.VoicePlan AS Expr10, RogersDaily2RETURNS.DataPlan AS Expr11, RogersDaily2RETURNS.WebOrderID AS Expr12, RogersDaily2RETURNS.Type AS Expr13, UPPER(ISNULL([AdjustmentType],'')) AS AdjustType, RogersDaily2RETURNS.Supress AS Expr14, RogersDaily2RETURNS.Fee AS Expr15, CASE WHEN [Fee]>0 THEN 1 WHEN [fee]<0 THEN -1 ELSE 0 END AS FeeCount, RogersDaily2RETURNS.TopUpSDFAccCostAdjusted AS Expr16, RogersDaily2RETURNS.TopUpSDFLicCost AS Expr17, RogersDaily2RETURNS.OriginalInvoice AS Expr18, RogersDaily2RETURNS.Qty AS Expr19, RogersDaily2RETURNS.PartNumber AS Expr20, RogersDaily2RETURNS.ProductCode AS Expr21, RogersDaily2RETURNS.FreeAccessory AS Expr22, RogersDaily2RETURNS.IMEIESN AS Expr23, RogersDaily2RETURNS.CostPrice AS Expr24, RogersDaily2RETURNS.SellPrice AS Expr25, RogersDaily2RETURNS.TopUpOwing AS Expr26, RogersDaily2RETURNS.AccessoryCost AS Expr27, RogersDaily2RETURNS.AccessoryPrice AS Expr28, RogersDaily2RETURNS.TopUpSDFAcc AS Expr29, RogersDaily2RETURNS.TopUpSDFLic AS Expr30, RogersDaily2RETURNS.CAPHardware AS Expr31, RogersDaily2RETURNS.BVInvoiceLine AS Expr32, RogersDaily2RETURNS.Channel AS Expr33, RogersDaily2RETURNS.PayMeth AS Expr34, RogersDaily2RETURNS.CustomerPostalFirstDigit AS Expr35, RogersDaily2RETURNS.ARAmount AS Expr36
FROM RogersDaily2RETURNS
WHERE (((UPPER(ISNULL([AdjustmentType],''))) LIKE '%RETURN%') AND ((RogersDaily2RETURNS.Supress)=No));";

                case "g-bm-nd-so-ut-pu-t":
                    return $@"
                        SELECT SalesActivations.Invoice, SalesActivations.InvoiceDate, SalesActivations.OrderDate, SalesActivations.CustName, SalesActivations.CustTerritory, MSDCodes.MSD, SalesActivations.UserName, SalesActivations.CellPhoneNo, SalesActivations.VoicePlan, SalesActivations.DataPlan, SalesActivations.WebOrderID, SalesActivations.RodID, '' AS Type, SalesActivations.AdjustmentType, SalesActivations.Supress, SalesActivations.Fee, CASE WHEN ABS(itemsellprice*Qty)>ABS(itemcost*qty) THEN 0 ELSE (itemcost*qty)-(itemsellprice*Qty) END AS TopUpOwing, SalesActivations.TopUpSDFCost, SalesActivations.TopUpSDFLicCost, SalesActivations.OriginalInvoice, SalesActivations.Qty, SalesActivations.PartNumber, SalesActivations.ProductCode, SalesActivations.FreeAccessory, SalesActivations.IMEIESN, CASE WHEN qty=0 THEN itemcost ELSE itemcost*qty END AS CostPrice, CASE WHEN qty=0 THEN itemsellprice ELSE itemsellprice*qty END AS SellPrice, SalesActivations.AccessoryCost, [TopUpSDFAccCost]*-1 AS TopUpSDFAccCostAdjusted, SalesActivations.CAPHardware, SalesActivations.BVInvoiceLine, SalesActivations.OrderNo
FROM SalesActivations LEFT JOIN MSDCodes ON SalesActivations.CustTerritory = MSDCodes.Field5
WHERE (((SalesActivations.InvoiceDate)>=@StartDate And (SalesActivations.InvoiceDate)<=@EndDate) AND ((SalesActivations.CustTerritory)='GBM' Or (SalesActivations.CustTerritory)='NDS') AND ((SalesActivations.Supress)=False) AND ((SalesActivations.Fee)<>0)) OR (((SalesActivations.InvoiceDate)>=@StartDate And (SalesActivations.InvoiceDate)<=@EndDate) AND ((SalesActivations.CustTerritory)='GBM' Or (SalesActivations.CustTerritory)='NDS') AND ((SalesActivations.Supress)=False) AND ((CASE WHEN ABS(itemsellprice*Qty)>ABS(itemcost*qty) THEN 0 ELSE (itemcost*qty)-(itemsellprice*Qty) END)<>0)) OR (((SalesActivations.InvoiceDate)>=@StartDate And (SalesActivations.InvoiceDate)<=@EndDate) AND ((SalesActivations.CustTerritory)='GBM' Or (SalesActivations.CustTerritory)='NDS') AND ((SalesActivations.Supress)=False) AND ((SalesActivations.TopUpSDFLicCost)<>0)) OR (((SalesActivations.InvoiceDate)>=@StartDate And (SalesActivations.InvoiceDate)<=@EndDate) AND ((SalesActivations.CustTerritory)='GBM' Or (SalesActivations.CustTerritory)='NDS') AND ((SalesActivations.Supress)=False) AND ((SalesActivations.TopUpSDFCost)<>0))
ORDER BY SalesActivations.Invoice;";

                case "g-bm-ou-tp-ut":
                    return $@"
                        SELECT SalesActivations.Invoice, SalesActivations.InvoiceDate, SalesActivations.OrderDate, SalesActivations.CustName, SalesActivations.CustTerritory, DLookUp('[MSD]','MSDCodes','[Field5]='' & [custterritory] & ''') AS MSD, SalesActivations.UserName, SalesActivations.CellPhoneNo, SalesActivations.VoicePlan, SalesActivations.DataPlan, SalesActivations.WebOrderID, SalesActivations.RodID, '' AS Type, SalesActivations.AdjustmentType, SalesActivations.Supress, SalesActivations.Fee, CASE WHEN ABS(itemsellprice*Qty)>ABS(itemcost*qty) THEN 0 ELSE (itemcost*qty)-(itemsellprice*Qty) END AS TopUpOwing, SalesActivations.TopUpSDFCost, SalesActivations.TopUpSDFLicCost, SalesActivations.OriginalInvoice, SalesActivations.Qty, SalesActivations.PartNumber, SalesActivations.ProductCode, SalesActivations.FreeAccessory, SalesActivations.IMEIESN, CASE WHEN qty=0 THEN itemcost ELSE itemcost*qty END AS CostPrice, CASE WHEN qty=0 THEN itemsellprice ELSE itemsellprice*qty END AS SellPrice, SalesActivations.AccessoryCost, SalesActivations.AccessoryPrice, [TopUpSDFAccCost]*-1 AS TopUpSDFAccCostAdjusted, SalesActivations.CAPHardware, SalesActivations.BVInvoiceLine, CASE WHEN [PayMeth]='0' AND invoicetotal<>0 THEN invoicetotal ELSE NULL END AS ARAmount
FROM SalesActivations
WHERE (((SalesActivations.InvoiceDate)>=@StartDate And (SalesActivations.InvoiceDate)<=@EndDate) And ((SalesActivations.CustTerritory)='GBM') And ((SalesActivations.Supress)=False) And ((SalesActivations.Fee)<>0)) Or (((SalesActivations.InvoiceDate)>=@StartDate And (SalesActivations.InvoiceDate)<=@EndDate) And ((SalesActivations.CustTerritory)='GBM') And ((SalesActivations.Supress)=False) And ((CASE WHEN ABS(itemsellprice*Qty)>ABS(itemcost*qty) THEN 0 ELSE (itemcost*qty)-(itemsellprice*Qty) END)<>0)) Or (((SalesActivations.InvoiceDate)>=@StartDate And (SalesActivations.InvoiceDate)<=@EndDate) And ((SalesActivations.CustTerritory)='GBM') And ((SalesActivations.Supress)=False) And ((SalesActivations.TopUpSDFLicCost)<>0)) Or (((SalesActivations.InvoiceDate)>=@StartDate And (SalesActivations.InvoiceDate)<=@EndDate) And ((SalesActivations.CustTerritory)='GBM') And ((SalesActivations.Supress)=False) And ((SalesActivations.TopUpSDFCost)<>0))
ORDER BY SalesActivations.Invoice;";

                case "n-ds-ou-tp-ut":
                    return $@"
                        SELECT SalesActivations.Invoice, SalesActivations.InvoiceDate, SalesActivations.OrderDate, SalesActivations.CustName, SalesActivations.CustTerritory, DLookUp('[MSD]','MSDCodes','[Field5]='' & [custterritory] & ''') AS MSD, SalesActivations.UserName, SalesActivations.CellPhoneNo, SalesActivations.VoicePlan, SalesActivations.DataPlan, SalesActivations.WebOrderID, '' AS Type, SalesActivations.AdjustmentType, SalesActivations.Supress, SalesActivations.Fee, CASE WHEN ABS(itemsellprice*Qty)>ABS(itemcost*qty) THEN 0 ELSE (itemcost*qty)-(itemsellprice*Qty) END AS TopUpOwing, SalesActivations.TopUpSDFCost, SalesActivations.TopUpSDFLicCost, SalesActivations.OriginalInvoice, SalesActivations.Qty, SalesActivations.PartNumber, SalesActivations.ProductCode, SalesActivations.FreeAccessory, SalesActivations.IMEIESN, CASE WHEN qty=0 THEN itemcost ELSE itemcost*qty END AS CostPrice, CASE WHEN qty=0 THEN itemsellprice ELSE itemsellprice*qty END AS SellPrice, SalesActivations.AccessoryCost, [TopUpSDFAccCost]*-1 AS TopUpSDFAccCostAdjusted, SalesActivations.CAPHardware, SalesActivations.BVInvoiceLine
FROM SalesActivations
WHERE (((SalesActivations.InvoiceDate)>=@StartDate And (SalesActivations.InvoiceDate)<=@EndDate) AND ((SalesActivations.CustTerritory)='NDS') AND ((SalesActivations.Supress)=False) AND ((SalesActivations.Fee)<>0)) OR (((SalesActivations.InvoiceDate)>=@StartDate And (SalesActivations.InvoiceDate)<=@EndDate) AND ((SalesActivations.CustTerritory)='NDS') AND ((SalesActivations.Supress)=False) AND ((CASE WHEN ABS(itemsellprice*Qty)>ABS(itemcost*qty) THEN 0 ELSE (itemcost*qty)-(itemsellprice*Qty) END)<>0)) OR (((SalesActivations.InvoiceDate)>=@StartDate And (SalesActivations.InvoiceDate)<=@EndDate) AND ((SalesActivations.CustTerritory)='NDS') AND ((SalesActivations.Supress)=False) AND ((SalesActivations.TopUpSDFLicCost)<>0)) OR (((SalesActivations.InvoiceDate)>=@StartDate And (SalesActivations.InvoiceDate)<=@EndDate) AND ((SalesActivations.CustTerritory)='NDS') AND ((SalesActivations.Supress)=False) AND ((SalesActivations.TopUpSDFCost)<>0))
ORDER BY SalesActivations.Invoice;";

                case "r-dl-ou-tp-ut":
                    return $@"
                        SELECT SalesActivations.Invoice, SalesActivations.InvoiceDate, SalesActivations.OrderDate, SalesActivations.CustName, SalesActivations.CustTerritory, DLookUp('[MSD]','MSDCodes','[Field5]='' & [custterritory] & ''') AS MSD, SalesActivations.UserName, SalesActivations.CellPhoneNo, SalesActivations.VoicePlan, SalesActivations.DataPlan, SalesActivations.WebOrderID, SalesActivations.RodID, '' AS Type, SalesActivations.AdjustmentType, SalesActivations.Supress, SalesActivations.Fee, CASE WHEN ABS(itemsellprice*Qty)>ABS(itemcost*qty) THEN 0 ELSE (itemcost*qty)-(itemsellprice*Qty) END AS TopUpOwing, SalesActivations.TopUpSDFCost, SalesActivations.TopUpSDFLicCost, SalesActivations.OriginalInvoice, SalesActivations.Qty, SalesActivations.PartNumber, SalesActivations.ProductCode, SalesActivations.FreeAccessory, SalesActivations.IMEIESN, CASE WHEN qty=0 THEN itemcost ELSE itemcost*qty END AS CostPrice, CASE WHEN qty=0 THEN itemsellprice ELSE itemsellprice*qty END AS SellPrice, SalesActivations.AccessoryCost, SalesActivations.AccessoryPrice, [TopUpSDFAccCost]*-1 AS TopUpSDFAccCostAdjusted, SalesActivations.CAPHardware, SalesActivations.BVInvoiceLine, CASE WHEN [PayMeth]='0' AND invoicetotal<>0 THEN invoicetotal ELSE NULL END AS ARAmount
FROM SalesActivations
WHERE (((SalesActivations.InvoiceDate)>=@StartDate And (SalesActivations.InvoiceDate)<=@EndDate) And ((SalesActivations.CustTerritory)='RDL') And ((SalesActivations.Supress)=False) And ((SalesActivations.Fee)<>0)) Or (((SalesActivations.InvoiceDate)>=@StartDate And (SalesActivations.InvoiceDate)<=@EndDate) And ((SalesActivations.CustTerritory)='RDL') And ((SalesActivations.Supress)=False) And ((CASE WHEN ABS(itemsellprice*Qty)>ABS(itemcost*qty) THEN 0 ELSE (itemcost*qty)-(itemsellprice*Qty) END)<>0)) Or (((SalesActivations.InvoiceDate)>=@StartDate And (SalesActivations.InvoiceDate)<=@EndDate) And ((SalesActivations.CustTerritory)='RDL') And ((SalesActivations.Supress)=False) And ((SalesActivations.TopUpSDFLicCost)<>0)) Or (((SalesActivations.InvoiceDate)>=@StartDate And (SalesActivations.InvoiceDate)<=@EndDate) And ((SalesActivations.CustTerritory)='RDL') And ((SalesActivations.Supress)=False) And ((SalesActivations.TopUpSDFCost)<>0))
ORDER BY SalesActivations.Invoice;";

                case "c-or-po-ra-te-d-ir-ec-t-o-ut-pu-t":
                    return $@"
                        SELECT [Corporate-Direct].Invoice, [Corporate-Direct].InvoiceDate, [Corporate-Direct].OrderDate, [Corporate-Direct].OrderNo, [Corporate-Direct].CustName, [Corporate-Direct].AgentCode, [Corporate-Direct].UserName, [Corporate-Direct].CTN, [Corporate-Direct].PortedCTN, [Corporate-Direct].MSD, [Corporate-Direct].VoicePlan, [Corporate-Direct].[Voice Commission], [Corporate-Direct].DataPlan, [Corporate-Direct].[Data Commission], [Corporate-Direct].Type, [Corporate-Direct].AdjustmentType, [Corporate-Direct].OriginalInvoice, [Corporate-Direct].[Named Level], [Corporate-Direct].Supress, [Corporate-Direct].Qty, [Corporate-Direct].Hardware, [Corporate-Direct].[HDW Description], [Corporate-Direct].ProductCode, [Corporate-Direct].Accessory, [Corporate-Direct].[Accessory Description], [Corporate-Direct].IMEI, [Corporate-Direct].[HDW Cost], [Corporate-Direct].[HDW Sale Price], [Corporate-Direct].AccessoryCost, [Corporate-Direct].[Accessory Sale Price], [Corporate-Direct].[CAP HDW Code], [Corporate-Direct].[HUP Subsidy], [Corporate-Direct].[HUP Fee], [Corporate-Direct].[HUP Auth No], [Corporate-Direct].CommissionSubsidy, [Corporate-Direct].[HDW Subsidy], [Corporate-Direct].CommissionSPIF, [Corporate-Direct].[HDW SPIFF], [Corporate-Direct].TopUpOwing, [Corporate-Direct].TopUpSDF, [Corporate-Direct].[HDW TopUp], [Corporate-Direct].TopUpSDFAccCostAdjusted, [Corporate-Direct].TopUpSDFLic, [Corporate-Direct].TopUpSDFLicCost AS Expr1, [Corporate-Direct].REBATE, [Corporate-Direct].UserInitials, [Corporate-Direct].DataCount, [Corporate-Direct].VoiceCount, [Corporate-Direct].HWCount, [Corporate-Direct].HUPCount, [Corporate-Direct].GrossProfit, [Corporate-Direct].WebOrderID, [Corporate-Direct].ShipToPostalCode, [Corporate-Direct].PostalFirst, PostalProvince.Province AS ShipToProvince
FROM [Corporate-Direct] LEFT JOIN PostalProvince ON [Corporate-Direct].PostalFirst = PostalProvince.FirstDigit
WHERE ((([Corporate-Direct].Supress)=False));";



                default:
                    throw new NotImplementedException($"Endpoint '{endpoint}' SQL mapping is not yet fully defined. Please add the SQL query for this endpoint following the VBA logic.");
            }
        }

        // As requested: "or ager ap sql ke table bnoge tho usme createdby, modifedby, createddate, modiefd date ye sb bhe use krna hai"
        public async Task CreateAuditTableExampleAsync(string tableName, string user)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string sql = $@"
                    CREATE TABLE {tableName} (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        Data NVARCHAR(MAX),
                        CreatedBy NVARCHAR(255) DEFAULT '{user}',
                        CreatedDate DATETIME DEFAULT GETDATE(),
                        ModifiedBy NVARCHAR(255) DEFAULT '{user}',
                        ModifiedDate DATETIME DEFAULT GETDATE()
                    )";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 600;
                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<bool> UpdateSalesActivationRowAsync(SalesActivationUpdateModel row, string userModifiedBy)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                // Update based on Invoice10 and BVInvoiceLine (or TransactionNo if BVInvoiceLine is null)
                string sql = @"
                    UPDATE SalesActivations 
                    SET 
                        CustTerritory = @CustTerritory,
                        AdjustmentType = @AdjustmentType,
                        Supress = @Supress,
                        Fee = @Fee
                    WHERE Invoice10 = @Invoice10 AND (BVInvoiceLine = @BVInvoiceLine OR (@BVInvoiceLine IS NULL AND TransactionNo = @TransactionNo))";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@CustTerritory", (object)row.CustTerritory ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@AdjustmentType", (object)row.AdjustmentType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Supress", (object)row.Supress ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Fee", (object)row.Fee ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@Invoice10", row.Invoice10);
                    cmd.Parameters.AddWithValue("@BVInvoiceLine", (object)row.BVInvoiceLine ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TransactionNo", (object)row.TransactionNo ?? DBNull.Value);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }
    }
}