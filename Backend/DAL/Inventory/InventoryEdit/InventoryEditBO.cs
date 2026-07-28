using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DAL.Inventory.InventoryEdit
{
    /// <summary>
    /// Business-layer request/response objects for InventoryEdit operations.
    /// These serve as DTOs between the Controller and the DA layer.
    /// </summary>

    public class UpdateTermsRequest
    {
        public string InvoiceNo { get; set; }
        public string TermsLabel { get; set; }
        public string ModifiedBy { get; set; }
    }

    public class UpdateBulkIdRequest
    {
        public string OldBulkId { get; set; }
        public string NewBulkId { get; set; }
        public string ModifiedBy { get; set; }
    }

    public class UpdateSingleInvoiceBulkIdRequest
    {
        public string InvoiceNo { get; set; }
        public string NewBulkId { get; set; }
        public string ModifiedBy { get; set; }
    }

    public class UpdateMultipleBulkIdsRequest
    {
        public List<string> InvoiceNos { get; set; }
        public string NewBulkId { get; set; }
        public string ModifiedBy { get; set; }
    }

    public class UpdateAddressRequest
    {
        public string InvoiceNo { get; set; }
        public AddressRecord BillTo { get; set; }
        public AddressRecord ShipTo { get; set; }
        public string ModifiedBy { get; set; }
    }

    /// <summary>
    /// Model representing the sales_history table in pgAdmin (PostgreSQL).
    /// Used for DTO and raw SQL mapping.
    /// </summary>
    public class sales_history
    {
        [Key]
        public string invoice_no { get; set; }
        public string terms_code { get; set; }
        public string terms_description { get; set; }
        public int terms_days_before_due { get; set; }
        public int terms_days_allowed { get; set; }
        public decimal terms_discount_rate { get; set; }
        public decimal total { get; set; }
        public string fob { get; set; }

        // Field updated by VBA in sales_history
        public string cust_name { get; set; }
    }

    /// <summary>
    /// Model for address edit operations, covering both Bill-To and Ship-To.
    /// Exact match for Spire 'addresses' table used in VBA.
    /// </summary>
    public class InvoiceAddressEditModel
    {
        public string InvoiceNo { get; set; }
        public AddressRecord BillTo { get; set; }
        public AddressRecord ShipTo { get; set; }
    }

    public class AddressRecord
    {
        public string Name { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string Address4 { get; set; }
        public string City { get; set; }
        public string ProvState { get; set; }
        public string PostalZip { get; set; }
        public string CountryCode { get; set; }
    }
}
