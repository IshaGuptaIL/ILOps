using DAL.Common.Login;
using DAL.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.CustomSearch
{
    public class CustomSearchDA : ICustomSearch
    {

        private readonly AppDBContext _dbContext;
        private readonly string _pgConn;
        private readonly string _sqlConn;

        public CustomSearchDA(AppDBContext context, IConfiguration config)
        {
            _dbContext = context;
            _pgConn = config.GetConnectionString("spire_Connection");      // Postgres
            _sqlConn = config.GetConnectionString("bvactivation_Connection"); // SQL Server
        }

        public async Task<ApiResposne> GetSalesActivationHeaders(string fieldName, string value)
        {
            var response = new ApiResposne();
            try
            {
                using var sqlConn = new SqlConnection(_sqlConn);
                await sqlConn.OpenAsync();

                string whereClause = "";
                var parameters = new List<SqlParameter>();

                switch (fieldName)
                {
                    case "WebOrderID":
                        whereClause = "WebOrderID = @val";
                        parameters.Add(new SqlParameter("@val", value));
                        break;
                    case "IMEIESN":
                        whereClause = "IMEIESN = @val";
                        parameters.Add(new SqlParameter("@val", value));
                        break;
                    case "CellPhoneNo":
                        whereClause = "CellPhoneNo = @val";
                        parameters.Add(new SqlParameter("@val", value));
                        break;
                    case "Invoice":
                        whereClause = "Invoice = @val";
                        parameters.Add(new SqlParameter("@val", value));
                        break;
                    case "CustomerName":
                        whereClause = "CustName LIKE @val + '%'";
                        parameters.Add(new SqlParameter("@val", value));
                        break;
                    case "CHTRChaseID":
                        whereClause = "CHTRChaseID = @val";
                        parameters.Add(new SqlParameter("@val", value));
                        break;
                    case "CustomerPO":
                        whereClause = "CustomerPONo = @val";
                        parameters.Add(new SqlParameter("@val", value));
                        break;
                    case "SimCardNo":
                        whereClause = "SIMCardNo = @val";
                        parameters.Add(new SqlParameter("@val", value));
                        break;
                    case "PortedCTN":
                        whereClause = "PortedCTN = @val";
                        parameters.Add(new SqlParameter("@val", value));
                        break;
                    case "OriginalInvoice":
                        whereClause = "OriginalInvoice = @val";
                        parameters.Add(new SqlParameter("@val", value));
                        break;
                    case "UserName":
                        whereClause = "UserName = @val";
                        parameters.Add(new SqlParameter("@val", value));
                        break;
                    case "CHTRWebID":
                        whereClause = "CHTRWebID = @val";
                        parameters.Add(new SqlParameter("@val", value));
                        break;
                    default:
                        whereClause = "1 = 0";
                        break;
                }

                string sqlBatch = $@"
                    DELETE FROM tblSalesActivations;

                    INSERT INTO tblSalesActivations (
                        Invoice, Invoice10, OrderNo, Customer, CustName, CustTerritory, MSD,
                        InvoiceDate, OrderDate, RecordType, RecordTypeExtended, VoicePlan,
                        VoicePlanDescription, CommissionVoice, DataPlan, DataPlanDescription,
                        CommissionData, CAPHardware, CapCost, W00Code, BVType, BVInvoiceLine,
                        BVRecNo, Whse, PartNumber, Description, ProductCode, CellPhoneNo,
                        IMEIESN, Qty, ItemCost, ItemSellPrice, CommissionSubsidy,
                        CommissionSubsidyCost, CommissionSPIF, CommissionSPIFCost, TopUpSDF,
                        TopUpSDFCost, TopUpSDFAcc, TopUpSDFAccCost, TopUpSDFLic,
                        TopUpSDFLicCost, REBATE, FreeAccessory, AccessoryCost, AccessoryPrice,
                        UserInitials, Salesperson, CustomerPONo, SIMCardNo, WebOrderID,
                        UserName, OriginalInvoice, AdjustmentType, Fee, Supress, PinNo,
                        CostBudgetCode, Department, Comments, FeeType, GSTRate, PSTRate,
                        GSTFlag, PSTFlag, PayMeth, CustomerPostal, CustomerPostalFirstDigit,
                        Channel, ImportLineID, InvoiceNet, InvoiceShipping, InvoiceTaxes,
                        InvoiceTotal, RodID, PortedCTN, Terms, TermsText, CAPCostHUP,
                        ShipToPostal, FreeAccessoryPart, AccessorySRP, SCOA, M2MOrderID,
                        ControlCentre, TransactionNo, AccountCode, AuthorizedDepartment,
                        CommissionCable, CablePlan, CablePlanDescription, RMANumber, PCCPID,
                        PCCPAmount, Tax1Code, Tax2Code, BVReceipt, BVReceiptNo,
                        OriginalSKUBVPartNumber, OriginalWebOrderID, OriginalHardware,
                        OriginalIMEI, CHTRWebID, CHTRChaseID, UpFrontEdgePrice,
                        InvoiceNetBeforeRVUE, ClaimCarrier, ClaimNumber, DeviceOfferTypeID,
                        POLine, ShipToPostalFirstDigit, ShipToProvince, R4BOrderID,
                        V21DealerCode, CustPayAmount, CustPayAmountOriginal, AccessoryType,
                        AccountNumber, AgentName, AgentEmail, AgentContactNumber,
                        RogersHWMarginShare, Term, bulk, SpireCount, CreatedDate
                    )
                    SELECT 
                        Invoice, Invoice10, OrderNo, Customer, CustName, CustTerritory, MSD,
                        InvoiceDate, OrderDate, RecordType, RecordTypeExtended, VoicePlan,
                        VoicePlanDescription, CAST(CommissionVoice AS INT), DataPlan, DataPlanDescription,
                        CAST(CommissionData AS INT), CAPHardware, CAST(CapCost AS INT), W00Code, BVType, BVInvoiceLine,
                        BVRecNo, Whse, PartNumber, Description, ProductCode, CellPhoneNo,
                        IMEIESN, CAST(Qty AS INT), CAST(ItemCost AS INT), CAST(ItemSellPrice AS INT), CommissionSubsidy,
                        CAST(CommissionSubsidyCost AS INT), CommissionSPIF, CAST(CommissionSPIFCost AS INT), TopUpSDF,
                        CAST(TopUpSDFCost AS INT), TopUpSDFAcc, CAST(TopUpSDFAccCost AS INT), TopUpSDFLic,
                        CAST(TopUpSDFLicCost AS INT), CAST(REBATE AS INT), FreeAccessory, CAST(AccessoryCost AS INT), CAST(AccessoryPrice AS INT),
                        UserInitials, Salesperson, CustomerPONo, SIMCardNo, WebOrderID,
                        UserName, OriginalInvoice, AdjustmentType, CAST(Fee AS VARCHAR), Supress, PinNo,
                        CostBudgetCode, Department, Comments, FeeType, GSTRate, PSTRate,
                        GSTFlag, PSTFlag, PayMeth, CustomerPostal, CustomerPostalFirstDigit,
                        Channel, ImportLineID, InvoiceNet, InvoiceShipping, InvoiceTaxes,
                        InvoiceTotal, RodID, PortedCTN, Terms, TermsText, CAPCostHUP,
                        ShipToPostal, FreeAccessoryPart, AccessorySRP, SCOA, M2MOrderID,
                        ControlCentre, TransactionNo, AccountCode, AuthorizedDepartment,
                        CommissionCable, CablePlan, CablePlanDescription, RMANumber, PCCPID,
                        PCCPAmount, Tax1Code, Tax2Code, BVReceipt, BVReceiptNo,
                        OriginalSKUBVPartNumber, OriginalWebOrderID, OriginalHardware,
                        OriginalIMEI, CHTRWebID, CHTRChaseID, UpFrontEdgePrice,
                        InvoiceNetBeforeRVUE, ClaimCarrier, ClaimNumber, DeviceOfferTypeID,
                        POLine, ShipToPostalFirstDigit, ShipToProvince, R4BOrderID,
                        V21DealerCode, CustPayAmount, CustPayAmountOriginal, AccessoryType,
                        AccountNumber, AgentName, AgentEmail, AgentContactNumber,
                        RogersHWMarginShare, Term, bulk, SpireCount, CreatedDate
                    FROM SalesActivations
                    WHERE {whereClause};    

                    WITH CTE AS (
                        SELECT seq, ROW_NUMBER() OVER (ORDER BY invoice10) as row_num
                        FROM tblSalesActivations
                    )
                    UPDATE CTE SET seq = row_num;
                ";

                using (var cmd = new SqlCommand(sqlBatch, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.Add(param);
                    }
                    await cmd.ExecuteNonQueryAsync();
                }

                string selectSql = @"
                    SELECT seq, Invoice, InvoiceDate, Customer, CustName, InvoiceTotal, 
                           CustTerritory, WebOrderID, OriginalInvoice, UpFrontEdgePrice, 
                           AdjustmentType, PayMeth, TransactionNo
                    FROM tblSalesActivations
                    ORDER BY seq;
                ";

                var list = new List<SalesActivationHeaderBO>();
                using (var cmd = new SqlCommand(selectSql, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        list.Add(new SalesActivationHeaderBO
                        {
                            Seq = reader["seq"] != DBNull.Value ? Convert.ToInt32(reader["seq"]) : 0,
                            Invoice = reader["Invoice"]?.ToString() ?? "",
                            InvoiceDate = reader["InvoiceDate"] != DBNull.Value ? Convert.ToDateTime(reader["InvoiceDate"]) : (DateTime?)null,
                            CustomerNo = reader["Customer"]?.ToString() ?? "",
                            CustomerName = reader["CustName"]?.ToString() ?? "",
                            InvoiceTotal = reader["InvoiceTotal"] != DBNull.Value ? Convert.ToDecimal(reader["InvoiceTotal"]) : 0,
                            CustTerritory = reader["CustTerritory"]?.ToString() ?? "",
                            WebOrderId = reader["WebOrderID"]?.ToString() ?? "",
                            OriginalInvoice = reader["OriginalInvoice"]?.ToString() ?? "",
                            UpfrontEdge = reader["UpFrontEdgePrice"] != DBNull.Value ? Convert.ToDecimal(reader["UpFrontEdgePrice"]) : (decimal?)null,
                            PaymentMethod = MapPaymentMethod(reader["PayMeth"]?.ToString()),
                            TransactionNumber = reader["TransactionNo"]?.ToString() ?? ""
                        });
                    }
                }

                response.Success = true;
                response.Result = list;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<ApiResposne> GetSalesActivationDetails(string invoiceNo)
        {
            var response = new ApiResposne();

            try
            {
                var list = new List<SalesActivationDetailBO>();

                await using var pgConn = new NpgsqlConnection(_pgConn);
                await pgConn.OpenAsync();

                var sql = @"
                    SELECT 
                        item.whse,
                        item.part_no,
                        item.description,
                        ist.number AS serial_no,
                        item.comment,
                        item.committed_qty,
                        item.unit_price
                    FROM ""sales_history_items"" item
                    LEFT JOIN ""inventory_serial_transactions"" ist 
                        ON item.invoice_no = ist.link_no 
                        AND item.guid = ist.link_guid
                    WHERE item.invoice_no = @invoiceNo
                    ORDER BY item.sequence;
                ";

                await using var cmd = new NpgsqlCommand(sql, pgConn);
                cmd.CommandTimeout = 600;
                cmd.Parameters.AddWithValue("@invoiceNo", invoiceNo);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new SalesActivationDetailBO
                    {
                        Whse = reader["whse"]?.ToString() ?? "",
                        PartNo = reader["part_no"]?.ToString() ?? "",
                        Description = reader["description"]?.ToString() ?? "",
                        SerialNo = reader["serial_no"]?.ToString() ?? "",
                        Comment = reader["comment"]?.ToString() ?? "",
                        Committed = reader["committed_qty"] != DBNull.Value ? Convert.ToInt32(reader["committed_qty"]) : 0,
                        UnitPrice = reader["unit_price"] != DBNull.Value ? Convert.ToDecimal(reader["unit_price"]) : 0
                    });
                }

                response.Success = true;
                response.Result = list;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<List<tblSpireInvoice>> GenerateInvoiceAsync(string invoiceNo, int seq)
        {
            var result = new List<tblSpireInvoice>();
            if (string.IsNullOrEmpty(invoiceNo))
                return result;

            await using var pgConn = new NpgsqlConnection(_pgConn);
            await pgConn.OpenAsync();

            var sqlFetch = @"
                SELECT 
                    (item.sequence + 1) AS seq1,
                    sh.invoice_no, sh.cust_no, sh.invoice_date, sh.territory_code, sh.terms_description,
                    item.whse, item.part_no, item.description, item.comment, item.committed_qty, item.unit_price, item.current_cost,
                    sh.subtotal, sh.freight, sh.total_discount, sh.total,
                    sh.sales_tax_total[1] AS sales_tax_total1,
                    sh.sales_tax_total[2] AS sales_tax_total2,
                    item.guid, item.serialized_qty,
                    (SELECT number FROM ""inventory_serial_transactions"" ist WHERE ist.link_guid = item.guid LIMIT 1) AS serial_no,
                    addr_b.name AS b_name,
                    addr_b.address[1] AS b_address1,
                    addr_b.address[2] AS b_address2,
                    addr_b.city AS b_city,
                    addr_b.prov_state AS b_prov_state,
                    addr_b.postal_zip AS b_postal_zip,
                    addr_s.name AS s_name,
                    addr_s.address[1] AS s_address1,
                    addr_s.address[2] AS s_address2,
                    addr_s.city AS s_city,
                    addr_s.prov_state AS s_prov_state,
                    addr_s.postal_zip AS s_postal_zip
                FROM ""sales_history"" sh
                INNER JOIN ""sales_history_items"" item ON sh.invoice_no = item.invoice_no
                LEFT JOIN ""addresses"" addr_b ON sh.invoice_no = addr_b.link_no AND addr_b.link_table = 'SHIS' AND addr_b.addr_type = 'B'
                LEFT JOIN ""addresses"" addr_s ON sh.invoice_no = addr_s.link_no AND addr_s.link_table = 'SHIS' AND addr_s.addr_type = 'S'
                WHERE sh.invoice_no = @invoiceNo
                ORDER BY item.sequence;
            ";

            await using var fetchCmd = new NpgsqlCommand(sqlFetch, pgConn);
            fetchCmd.CommandTimeout = 600;
            fetchCmd.Parameters.AddWithValue("@invoiceNo", invoiceNo);

            await using var reader = await fetchCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string comment = reader["comment"] != DBNull.Value ? reader["comment"].ToString() : string.Empty;
                string desc = reader["description"] != DBNull.Value ? reader["description"].ToString() : string.Empty;
                string finalDescription = desc;
                if (!string.IsNullOrEmpty(comment))
                {
                    finalDescription = comment.Replace("\r\n", ";").Replace("\n", ";");
                }

                var row = new tblSpireInvoice
                {
                    Seq1 = reader["seq1"] != DBNull.Value ? Convert.ToInt32(reader["seq1"]) : 0,
                    invoice_no = reader["invoice_no"]?.ToString() ?? string.Empty,
                    cust_no = reader["cust_no"]?.ToString() ?? string.Empty,
                    invoice_date = reader["invoice_date"] != DBNull.Value
                        ? reader["invoice_date"] is DateTime dt
                            ? dt
                            : ((DateOnly)reader["invoice_date"]).ToDateTime(TimeOnly.MinValue)
                        : DateTime.MinValue,
                    territory_code = reader["territory_code"]?.ToString() ?? string.Empty,
                    terms_description = reader["terms_description"]?.ToString() ?? string.Empty,
                    whse = reader["whse"]?.ToString() ?? string.Empty,
                    part_no = reader["part_no"]?.ToString() ?? string.Empty,
                    description = finalDescription,
                    committed_qty = reader["committed_qty"] != DBNull.Value ? Convert.ToInt32(reader["committed_qty"]) : 0,
                    unit_price = reader["unit_price"] != DBNull.Value ? (int?)Convert.ToInt32(Convert.ToDecimal(reader["unit_price"])) : 0,
                    current_cost = reader["current_cost"] != DBNull.Value ? (int?)Convert.ToInt32(Convert.ToDecimal(reader["current_cost"])) : 0,
                    subtotal = reader["subtotal"] != DBNull.Value ? (int?)Convert.ToInt32(Convert.ToDecimal(reader["subtotal"])) : 0,
                    freight = reader["freight"] != DBNull.Value ? (int?)Convert.ToInt32(Convert.ToDecimal(reader["freight"])) : 0,
                    total_discount = reader["total_discount"] != DBNull.Value ? (int?)Convert.ToInt32(Convert.ToDecimal(reader["total_discount"])) : 0,
                    total = reader["total"] != DBNull.Value ? (int?)Convert.ToInt32(Convert.ToDecimal(reader["total"])) : 0,
                    sales_tax_total1 = reader["sales_tax_total1"] != DBNull.Value ? (int?)Convert.ToInt32(Convert.ToDecimal(reader["sales_tax_total1"])) : 0,
                    sales_tax_total2 = reader["sales_tax_total2"] != DBNull.Value ? (int?)Convert.ToInt32(Convert.ToDecimal(reader["sales_tax_total2"])) : 0,
                    strGUID = reader["guid"]?.ToString() ?? string.Empty,
                    serialized_qty = reader["serialized_qty"] != DBNull.Value ? Convert.ToInt32(reader["serialized_qty"]) : 0,
                    number = reader["serial_no"]?.ToString() ?? string.Empty,
                    
                    CUSTOM_AddressesWB_link_table = "SHIS",
                    CUSTOM_AddressesWB_1_link_table = "SHIS",
                    CUSTOM_AddressesWB_addr_type = "B",
                    CUSTOM_AddressesWB_1_addr_type = "S",
                    
                    CUSTOM_AddressesWB_name = reader["b_name"]?.ToString() ?? string.Empty,
                    CUSTOM_AddressesWB_address1 = reader["b_address1"]?.ToString() ?? string.Empty,
                    CUSTOM_AddressesWB_address2 = reader["b_address2"]?.ToString() ?? string.Empty,
                    CUSTOM_AddressesWB_city = reader["b_city"]?.ToString() ?? string.Empty,
                    CUSTOM_AddressesWB_prov_state = reader["b_prov_state"]?.ToString() ?? string.Empty,
                    CUSTOM_AddressesWB_postal_zip = reader["b_postal_zip"]?.ToString() ?? string.Empty,
                    
                    CUSTOM_AddressesWB_1_name = reader["s_name"]?.ToString() ?? string.Empty,
                    CUSTOM_AddressesWB_1_address1 = reader["s_address1"]?.ToString() ?? string.Empty,
                    CUSTOM_AddressesWB_1_address2 = reader["s_address2"]?.ToString() ?? string.Empty,
                    CUSTOM_AddressesWB_1_city = reader["s_city"]?.ToString() ?? string.Empty,
                    CUSTOM_AddressesWB_1_prov_state = reader["s_prov_state"]?.ToString() ?? string.Empty,
                    CUSTOM_AddressesWB_1_postal_zip = reader["s_postal_zip"]?.ToString() ?? string.Empty
                };

                result.Add(row);
            }

            await reader.CloseAsync();
            await pgConn.CloseAsync();

            using var sqlConn = new SqlConnection(_sqlConn);
            await sqlConn.OpenAsync();

            using (var delCmd = new SqlCommand("DELETE FROM tblSpireInvoice;", sqlConn))
            {
                delCmd.CommandTimeout = 600;
                await delCmd.ExecuteNonQueryAsync();
            }

            foreach (var inv in result)
            {
                var insertCmd = new SqlCommand(@"
                    DECLARE @HasIdentity INT = OBJECTPROPERTY(OBJECT_ID('tblSpireInvoice'), 'TableHasIdentity');
                    IF @HasIdentity = 1
                    BEGIN
                        SET IDENTITY_INSERT tblSpireInvoice ON;
                    END

                    INSERT INTO tblSpireInvoice (
                        Seq1, invoice_no, cust_no, invoice_date, territory_code, terms_description,
                        whse, part_no, description, committed_qty, unit_price, current_cost,
                        subtotal, freight, total_discount, total, sales_tax_total1, sales_tax_total2,
                        strGUID, serialized_qty, number,
                        CUSTOM_AddressesWB_link_table, CUSTOM_AddressesWB_1_link_table,
                        CUSTOM_AddressesWB_addr_type, CUSTOM_AddressesWB_1_addr_type,
                        CUSTOM_AddressesWB_name, CUSTOM_AddressesWB_address1, CUSTOM_AddressesWB_address2,
                        CUSTOM_AddressesWB_city, CUSTOM_AddressesWB_prov_state, CUSTOM_AddressesWB_postal_zip,
                        CUSTOM_AddressesWB_1_name, CUSTOM_AddressesWB_1_address1, CUSTOM_AddressesWB_1_address2,
                        CUSTOM_AddressesWB_1_city, CUSTOM_AddressesWB_1_prov_state, CUSTOM_AddressesWB_1_postal_zip
                    ) VALUES (
                        @Seq1, @invoice_no, @cust_no, @invoice_date, @territory_code, @terms_description,
                        @whse, @part_no, @description, @committed_qty, @unit_price, @current_cost,
                        @subtotal, @freight, @total_discount, @total, @sales_tax_total1, @sales_tax_total2,
                        @strGUID, @serialized_qty, @number,
                        'SHIS', 'SHIS', 'B', 'S',
                        @b_name, @b_address1, @b_address2,
                        @b_city, @b_prov_state, @b_postal_zip,
                        @s_name, @s_address1, @s_address2,
                        @s_city, @s_prov_state, @s_postal_zip
                    );

                    IF @HasIdentity = 1
                    BEGIN
                        SET IDENTITY_INSERT tblSpireInvoice OFF;
                    END
                ", sqlConn);

                insertCmd.CommandTimeout = 600;
                insertCmd.Parameters.AddWithValue("@Seq1", inv.Seq1);
                insertCmd.Parameters.AddWithValue("@invoice_no", inv.invoice_no);
                insertCmd.Parameters.AddWithValue("@cust_no", inv.cust_no);
                insertCmd.Parameters.AddWithValue("@invoice_date", inv.invoice_date);
                insertCmd.Parameters.AddWithValue("@territory_code", inv.territory_code);
                insertCmd.Parameters.AddWithValue("@terms_description", inv.terms_description);
                insertCmd.Parameters.AddWithValue("@whse", inv.whse);
                insertCmd.Parameters.AddWithValue("@part_no", inv.part_no);
                insertCmd.Parameters.AddWithValue("@description", inv.description);
                insertCmd.Parameters.AddWithValue("@committed_qty", inv.committed_qty);
                insertCmd.Parameters.AddWithValue("@unit_price", inv.unit_price ?? 0);
                insertCmd.Parameters.AddWithValue("@current_cost", inv.current_cost ?? 0);
                insertCmd.Parameters.AddWithValue("@subtotal", inv.subtotal ?? 0);
                insertCmd.Parameters.AddWithValue("@freight", inv.freight ?? 0);
                insertCmd.Parameters.AddWithValue("@total_discount", inv.total_discount ?? 0);
                insertCmd.Parameters.AddWithValue("@total", inv.total ?? 0);
                insertCmd.Parameters.AddWithValue("@sales_tax_total1", inv.sales_tax_total1 ?? 0);
                insertCmd.Parameters.AddWithValue("@sales_tax_total2", inv.sales_tax_total2 ?? 0);
                insertCmd.Parameters.AddWithValue("@strGUID", inv.strGUID);
                insertCmd.Parameters.AddWithValue("@serialized_qty", inv.serialized_qty);
                insertCmd.Parameters.AddWithValue("@number", inv.number);

                insertCmd.Parameters.AddWithValue("@b_name", inv.CUSTOM_AddressesWB_name);
                insertCmd.Parameters.AddWithValue("@b_address1", inv.CUSTOM_AddressesWB_address1);
                insertCmd.Parameters.AddWithValue("@b_address2", inv.CUSTOM_AddressesWB_address2);
                insertCmd.Parameters.AddWithValue("@b_city", inv.CUSTOM_AddressesWB_city);
                insertCmd.Parameters.AddWithValue("@b_prov_state", inv.CUSTOM_AddressesWB_prov_state);
                insertCmd.Parameters.AddWithValue("@b_postal_zip", inv.CUSTOM_AddressesWB_postal_zip);

                insertCmd.Parameters.AddWithValue("@s_name", inv.CUSTOM_AddressesWB_1_name);
                insertCmd.Parameters.AddWithValue("@s_address1", inv.CUSTOM_AddressesWB_1_address1);
                insertCmd.Parameters.AddWithValue("@s_address2", inv.CUSTOM_AddressesWB_1_address2);
                insertCmd.Parameters.AddWithValue("@s_city", inv.CUSTOM_AddressesWB_1_city);
                insertCmd.Parameters.AddWithValue("@s_prov_state", inv.CUSTOM_AddressesWB_1_prov_state);
                insertCmd.Parameters.AddWithValue("@s_postal_zip", inv.CUSTOM_AddressesWB_1_postal_zip);

                await insertCmd.ExecuteNonQueryAsync();
            }

            return result;
        }

        public async Task<ApiResposne> GetTransactionData(string invoiceNo)
        {
            var response = new ApiResposne();

            try
            {
                await using var pgConn = new NpgsqlConnection(_pgConn);
                await pgConn.OpenAsync();

                var sql = @"
                    SELECT 
                        t.account_no,
                        a.name,
                        t.debit_amt,
                        t.credit_amt,
                        t.trans_no,
                        s.invoice_no,
                        t.date
                    FROM sales_history s
                    INNER JOIN gl_transactions t 
                        ON s.trans_no = t.trans_no
                    INNER JOIN gl_accounts a 
                        ON t.account_no = a.account_no
                        AND t.division = a.division
                        AND t.currency = a.currency
                    WHERE s.invoice_no = @invoiceNo
                ";

                var list = new List<object>();

                await using var cmd = new NpgsqlCommand(sql, pgConn);
                cmd.CommandTimeout = 600;
                cmd.Parameters.AddWithValue("@invoiceNo", invoiceNo);

                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        AccountNo = reader["account_no"]?.ToString(),
                        Name = reader["name"]?.ToString(),
                        Debit = reader["debit_amt"] != DBNull.Value ? Convert.ToDecimal(reader["debit_amt"]) : 0,
                        Credit = reader["credit_amt"] != DBNull.Value ? Convert.ToDecimal(reader["credit_amt"]) : 0,
                        TransNo = reader["trans_no"]?.ToString(),
                        Invoice = reader["invoice_no"]?.ToString(),
                        Date = reader["date"] != DBNull.Value
                            ? reader["date"] is DateTime dt
                                ? dt
                                : ((DateOnly)reader["date"]).ToDateTime(TimeOnly.MinValue)
                            : DateTime.MinValue
                    });
                }

                response.Success = true;
                response.Result = list;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        private string MapPaymentMethod(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return "CC";

            if (double.TryParse(code, out double num))
            {
                int val = (int)num;
                if (val == 0 || val == 1)
                    return "AR";
                if (val == 5)
                    return "V21";
            }
            return "CC";
        }

    }
}