using DAL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Inventory.InventoryEdit
{
    public interface IInventoryEdit
    {
        // ─── Terms Edit ──────────────────────────────────────────────────────
        Task<sales_history> GetInvoiceTermsAsync(string invoiceNo);
        Task<bool> UpdateInvoiceTermsAsync(string invoiceNo, string termsLabel, string modifiedBy);

        // ─── Bulk ID Edit ─────────────────────────────────────────────────────
        Task<int> GetBulkIdCountAsync(string bulkId);
        Task<bool> UpdateBulkIdAsync(string oldBulkId, string newBulkId, string modifiedBy);
        Task<sales_history> GetSingleInvoiceBulkIdAsync(string invoiceNo);
        Task<bool> UpdateSingleInvoiceBulkIdAsync(string invoiceNo, string newBulkId, string modifiedBy);
        Task<bool> UpdateMultipleBulkIdsAsync(List<string> invoiceNos, string newBulkId, string modifiedBy);

        // ─── Address Edit ─────────────────────────────────────────────────────
        Task<InvoiceAddressEditModel> GetInvoiceAddressAsync(string invoiceNo);
        Task<bool> UpdateInvoiceAddressAsync(InvoiceAddressEditModel model, string modifiedBy);
    }
}
