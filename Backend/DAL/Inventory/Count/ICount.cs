using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.Count
{
    public interface ICount
    {

        Task<bool> DeleteCounts(string fileName, bool isACC);
        Task<bool> DeleteAllCounts(bool isACC);
        Task<bool> LoadSnapshot(InventorySnapshotBO options);
        Task<object> GetFileStatus();
        // Return byte array, not IActionResult
        Task<byte[]> ExportHardwareCounts();
        Task<string> TestFileAccess();
       Task<byte[]> ExportAccessoryCounts();
        Task<List<string>> GetUniqueFileNames(bool isACC);
        Task<bool> SyncInventoryFiles();

    }
    }

