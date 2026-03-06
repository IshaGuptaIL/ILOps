using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.OutputInvoice
{
    public interface IOutputInvoice
    {
        Task<PagedInvoiceResponse> GetInvoiceListPaged(int pageNumber, int pageSize);

        Task<List<InvoiceItem>> GetInvoiceList();

        Task<bool> ClearInvoiceList();
        Task<string> CheckSpireHistory(string invoiceNo);
         Task<bool> ProcessInvoiceOutput(string invoiceNo, string folder, string prefix, bool isSpire);
        Task<int> ProcessAllInvoices(string folder, string prefix, string invType);
    }
}