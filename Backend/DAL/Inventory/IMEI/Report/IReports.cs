using DAL.Common.Login;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.IMEI.Report
{
    public interface IReports
    {

        Task<List<InventoryStockBO>> GetInventoryStockStatus();

        Task<List<ReceivedReportBO>> GetReceivedReport(
            string itemType,
            string vendor,
            string part,
            DateTime? startDate,
            DateTime? endDate);

       


        Task<ApiResposne> GetParts(string itemType);
        Task<ApiResposne> GetVendors();

        Task<List<SpireReceiptBO>> GetSpireReceipts(DateOnly startDate, DateOnly endDate, string whse = "CO");
        Task<List<HardwareReceiptBO>> GetHardwareReceipts(string receiptNo, string poNumber);


    }
}
