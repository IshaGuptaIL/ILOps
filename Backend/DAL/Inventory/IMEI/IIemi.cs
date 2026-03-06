using DAL.Common.Login;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.IMEI
{
    public interface Iiemi
    {

        Task<ApiResposne> FindByImeiAsync(string imei);
        Task<ApiResposne> GetRogersInvoicesAsync(string bvReceiptNo);
    }
}
