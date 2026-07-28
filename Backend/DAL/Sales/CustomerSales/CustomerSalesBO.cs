using System;
using System.Collections.Generic;

namespace DAL.Sales.CustomerSales
{
    public class CustomerSalesRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? CustGroup { get; set; }
        public string? MSDCode { get; set; }
        public string? TerritoryCode { get; set; }
    }

    public class CreateGroupRequest
    {
        public string CustGroup { get; set; }
        public string GroupName { get; set; }
        public string BVCustNo { get; set; }
        public bool IncludeFrench { get; set; }
    }

    public class CustomerGroupBO
    {
        public string CustGroup { get; set; }
        public string GroupName { get; set; }
        public int BVCustCount { get; set; }
    }

    public class BVCustomerBO
    {
        public string BVCustNo { get; set; }
        public string BVName { get; set; }
    }

    public class CustomerSalesRow
    {
        public string? WebOrderID { get; set; }
        public string? Invoice { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string? VoicePlanDescription { get; set; }
        public string? DataPlanDescription { get; set; }
        public string? CellPhoneNo { get; set; }
        public string? UserName { get; set; }
        public string? PONo { get; set; }
        public string? CostBudgetCode { get; set; }
        public string? PartNumber { get; set; }
        public string? HardwareDescription { get; set; }
        public int? HDWQty { get; set; }
        public string? IMEIESN { get; set; }
        public string? AccParts { get; set; }
        public string? AccessoryDescription { get; set; }
        public string? AccQtys { get; set; }
        public string? ShipToProvince { get; set; }
        public decimal? InvoiceNet { get; set; }
        public decimal? InvoiceShipping { get; set; }
        public decimal? InvoiceTaxes { get; set; }
        public decimal? InvoiceTotal { get; set; }
        public string? CustGroup { get; set; }
        public string? CustNO { get; set; }
        public string? TypeOfService { get; set; }
        public string? PinNumber { get; set; }
        public decimal? HSTGST { get; set; }
        public decimal? PSTQST { get; set; }
        public string? MSDCode { get; set; }
        public string? CustomerName { get; set; }
        public string? Territory { get; set; }
        public string? AccountCode { get; set; }
        public string? AuthorizedDepartment { get; set; }
        public string? ShipToAddress { get; set; }
        public string? ShipToStreetAddress { get; set; }
        public string? ShipToCity { get; set; }
        public string? ShipToPostal { get; set; }
        public decimal? GSTRate { get; set; }
        public decimal? PSTRate { get; set; }
        public string? GSTFlag { get; set; }
        public string? PSTFlag { get; set; }
        public int? Tax1Code { get; set; }
        public int? Tax2Code { get; set; }
        public string? PortedCTN { get; set; }
        public string? BulkOrderID { get; set; }
        public decimal? HardwareCharge { get; set; }
        public decimal? AccessoryCharge { get; set; }
        public string? ARStatus { get; set; }
        public decimal? UserPayAmount { get; set; }
        public string? UserPayMethod { get; set; }
        public decimal? Balance { get; set; }
    }

    public class CustomerFieldBO
    {
        public int Id { get; set; }
        public string CustomerGroup { get; set; }
        public string FieldName { get; set; }
        public string Label { get; set; }
        public bool Include { get; set; }
        public int Sequence { get; set; }
        public string? SummaryType { get; set; }
        public string? FormatString { get; set; }
        public int? Level { get; set; }
    }
}
