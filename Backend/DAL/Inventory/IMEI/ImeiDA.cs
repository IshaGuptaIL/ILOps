using DAL.Common.Login;
using DAL.Inventory.IMEI;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

public class ImeiDA : Iiemi
{
    private readonly string _sqlConn;

    public ImeiDA(IConfiguration config)
    {
        _sqlConn = config.GetConnectionString("bvactivation_Connection");
    }

    public async Task<ApiResposne> FindByImeiAsync(string imei)
    {
        var response = new ApiResposne();

        if (string.IsNullOrEmpty(imei))
        {
            response.Success = false;
            response.Message = "IMEI is required";
            return response;
        }

        HardwareReceivedVM receipt = null;

        string sql = @"
            SELECT TOP 1 *, Vendor
            FROM HardwareReceived
            WHERE IMEI = @IMEI";

        using var conn = new SqlConnection(_sqlConn);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@IMEI", imei);

        await conn.OpenAsync();
        using var r = await cmd.ExecuteReaderAsync();

        if (await r.ReadAsync())
        {
            receipt = new HardwareReceivedVM
            {
                VendorName = r["Vendor"]?.ToString(),
                BVReceiptNo = r["BVReceiptNo"]?.ToString(),
                PONumber = r["PO"]?.ToString(),
                PartNo = r["Part"]?.ToString(),
                QtyReceived = Convert.ToInt32(r["Qty"]),
                UnitCost = Convert.ToDecimal(r["ReceiptUnitCost"]),
                ReceiptDate = Convert.ToDateTime(r["BVReceiptDate"]),
                CMO = r["CMO"]?.ToString()
            };
        }

        if (receipt == null)
        {
            response.Success = false;
            response.Message = "IMEI not found";
            return response;
        }

        response.Success = true;
        response.Message = "IMEI found";
        response.Result = receipt;
        return response;
    }

    public async Task<ApiResposne> GetRogersInvoicesAsync(string bvReceiptNo)
    {
        var response = new ApiResposne();

        var list = new List<RogersInvoiceVM>();

        string sql = @"
            SELECT TransType, RefNo, TransDate, PerUnitAmount, Qty, Remarks
            FROM tblRogersInvoice
            WHERE BVReceiptNo = @BVReceiptNo";

        using var conn = new SqlConnection(_sqlConn);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@BVReceiptNo", bvReceiptNo);

        await conn.OpenAsync();
        using var r = await cmd.ExecuteReaderAsync();

        while (await r.ReadAsync())
        {
            list.Add(new RogersInvoiceVM
            {
                TransType = r["TransType"]?.ToString(),
                RefNo = r["RefNo"]?.ToString(),
                TransDate = (DateTime)(r["TransDate"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(r["TransDate"])),
                PerUnitAmount = r["PerUnitAmount"] == DBNull.Value
                    ? 0
                    : Convert.ToDecimal(r["PerUnitAmount"]),
                Qty = r["Qty"] == DBNull.Value
                    ? 0
                    : Convert.ToInt32(r["Qty"]),
                Remarks = r["Remarks"]?.ToString()
            });
        }

        response.Success = true;
        response.Message = "Invoices fetched";
        response.Result = list;
        response.Count = list.Count;

        return response;
    }
}
