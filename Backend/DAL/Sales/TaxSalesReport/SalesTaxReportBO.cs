using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Sales.TaxSalesReport
{
    public class SalesTaxReportBO
    {
    }

    public class SalesTaxReportRequest
    {
        public System.DateTime StartDate { get; set; }
        public System.DateTime EndDate { get; set; }
    }

    public class SalesTaxReportRow
    {
        public int Trans { get; set; }
        public System.DateTime? Invdate { get; set; }
        public string Invoice { get; set; }
        public string WebOrderID { get; set; }
        public string Source { get; set; } = "OE";
        public string CustNo { get; set; }
        public string CustName { get; set; }
        public string Territory { get; set; }
        public string ShipToProvince { get; set; }
        public string PostalDigit { get; set; }
        public string OneIMEI { get; set; }
        public int? Tax1Code { get; set; }
        public string Tax1Name { get; set; }
        public string Tax1GL { get; set; }
        public int? Tax2Code { get; set; }
        public string Tax2Name { get; set; }
        public string Tax2GL { get; set; }
        public decimal InvoiceNet { get; set; }
        public decimal Tax1Total { get; set; }
        public decimal Tax2Total { get; set; }
        public decimal ShippingAmt { get; set; }
        public decimal InvoiceTotalBeforeUERVValue { get; set; }
        public decimal UERVValue { get; set; }
        public decimal InvoiceTotal { get; set; }
        public decimal TotalOfExtendedSell { get; set; }
        public Dictionary<string, decimal> DepartmentSales { get; set; } = new Dictionary<string, decimal>();
    }

    public class SalesTaxReportResponse
    {
        public List<SalesTaxReportRow> Data { get; set; }
        public List<string> DepartmentNames { get; set; }
    }

    public class GLTaxTransactionRow
    {
        public string TransNo { get; set; }
        public System.DateTime TransDate { get; set; }
        public System.DateTime PostDate { get; set; }
        public string GLAcct { get; set; }
        public string GLAcctName { get; set; }
        public string Module { get; set; }
        public string User { get; set; }
        public string Memo { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }

    public class VendorBO
    {
        public string VendorNo { get; set; }
        public string Name { get; set; }
    }

    public class VendorActivityRequest
    {
        public string Vendor { get; set; }
        public System.DateTime StartDate { get; set; }
        public System.DateTime EndDate { get; set; }
    }

}