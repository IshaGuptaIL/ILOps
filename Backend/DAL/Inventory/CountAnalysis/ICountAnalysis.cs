using DAL.Common.Login;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.CountAnalysis
{
    public interface ICountAnalysis
    {

        // 1 ROW
        Task<ApiResposne> LoadIMEICounts(Stream excelStream, string fileName);
        Task<ApiResposne> GetOnhandNotCounted();
        Task<ApiResposne> GetWarehouseAssignments(int pageNumber, int pageSize);
        Task<ApiResposne> GetAllImportedCounts();
        Task<ApiResposne> GetDuplicateIMEICounts(int pageNumber, int pageSize);
        Task<ApiResposne> GetSystemDuplicateSerials(int pageNumber, int pageSize);
        Task<ApiResposne> ProcessDuplicateCounts();
        Task<ApiResposne> GetDuplicateCleanupPreview();
        Task<ApiResposne> DeleteDuplicateCounts();
        Task<ApiResposne> GetInvalidSerialCounts();
        Task<ApiResposne> GetSystemSerialVerification();
        Task<ApiResposne> GetDiscrepancyReport();
        Task<ApiResposne> GetQuantityVsSerialComparison();
        Task<ApiResposne> GetMissingFromPhysicalCount();
        Task<ApiResposne> ProcessCountedNotOnhandDetails();


        // 2 ROW
         Task<ApiResposne> LoadSpireSalesAndReceipts(string type);
        Task<ApiResposne> GetAccessoryDiscrepancies();
        Task<ApiResposne> GetCountedNotInBV();
        Task<ApiResposne> GetOnhandNotCounteds();
        Task<ApiResposne> GetLoadedStockStatus();
     Task<ApiResposne> ImportBackorders(IFormFile file);
        Task<ApiResposne> ImportACCCounts(Stream excelStream, string fileName);
        Task<ACCEditResponse> GetACCCountsForEdit();
        Task<bool> UpdateACCCount(int id, double newQty);
        Task<ApiResposne> ImportBackOrders(Stream excelStream, string fileName);
        Task<List<string>> GetWarehouses();
        Task<List<object>> GetCountFiles(string type);
        Task<bool> AssignCountsToWarehouse(AssignWarehouseRequest request);
        Task<object> GetCountFileSummary(string fileName, string type);

        // 2 ROW



        //3 ROW

        Task<ApiResposne> GetItemReceiptsSummary(DateTime startDate, DateTime endDate);
        //Task<ApiResposne> GetAccessoryTotalsByTerritory(DateTime start, DateTime end);
        Task<ApiResposne> GetAccessorySalesByChannel(DateTime startDate, DateTime endDate);
        Task<ApiResposne> GetItemSalesSummary();
        Task<ApiResposne> GetAccessoryAnalysisReport(
       DateTime startDate,
       DateTime endDate);
    }
}
