using DAL.Common.Login;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.CustomSearch
{
    public interface ICustomSearch
    {
        Task<ApiResposne> GetSalesActivationHeaders(string fieldName, string value);
        Task<ApiResposne> GetSalesActivationDetails(string invoiceNo);
        Task<List<tblSpireInvoice>> GenerateInvoiceAsync(string invoiceNo, int seq);
        Task<ApiResposne> GetTransactionData(string invoiceNo);
    }
}
