using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;

namespace DAL.Models
{
    public class AppDBModal
    {
    }
    public class tblTransferListACC
    {
        [Key]
        public int ID { get; set; }

        public string? WarehouseCodeTransferFrom { get; set; }

        public string? WarehouseCodeTransferTo { get; set; }

        public string? PartNo { get; set; }

        public int? Quantity { get; set; }

        public string? ValidationResult { get; set; }

        public DateTime? TransferDateTime { get; set; }

        public int? RowNumber { get; set; }

        public bool? TransferCreated { get; set; }

        public bool? TransferPosted { get; set; }
    }
    public class tblTransferList
    {
        [Key]
        public int ID { get; set; }

        public string? WarehouseCodeTransferFrom { get; set; }

        public string? WarehouseCodeTransferTo { get; set; }

        public string? PartNo { get; set; }

        public string? IMEI { get; set; }

        public string? SimPartNo { get; set; }

        public string? SimNo { get; set; }

        public string? Pin { get; set; }

        public string? ValidationResult { get; set; }

        public DateTime? TransferDateTime { get; set; }

        public int? RowNumber { get; set; }

        public bool? TransferCreated { get; set; }

        public bool? TransferPosted { get; set; }
    }

    public class tblTransferLog
    {
        [Key]
            public int Id { get; set; }

            public string? ReferenceNo { get; set; }

            public string? TransferType { get; set; }

            public string? FromWhse { get; set; }

            public string? ToWhse { get; set; }

            public string? PartNo { get; set; }

            public string? Serial { get; set; }

            public string? SIMPartNo { get; set; }

            public string? SIMNo { get; set; }

            public string? Pin { get; set; }

            public DateTime? TransferDate { get; set; }

            public DateTime? ActualDateTime { get; set; }

            public int? Quantity { get; set; }
        }

       
    public class tbllastpoitem
    {
        [Key]
        public int ID { get; set; }

        public int? LastNumber { get; set; }

        public string CODE { get; set; }

        public string NUMBER { get; set; }

        public int? RECNO { get; set; }

        public int? POQty { get; set; }

        public DateTime? PODate { get; set; }
        public int? Created_by { get; set; }
        public DateTime? Created_date { get; set; }
        public int? Modified_by { get; set; }
        public DateTime? Modified_date { get; set; }
    }

    public class IMEIStatus
    {
        [Key]
        public int ID { get; set; }

        public string Whse { get; set; }

        public string PartNo { get; set; }

        public string IMEI { get; set; }

        public string Status { get; set; }

        public string LastInvoice { get; set; }

        public string LastInvoiceDate { get; set; }
    }

    public class tblInvoiceList
    {
        [Key]
        public int ID { get; set; }
        public string InvoiceNo { get; set; }
    }

    public class tblSalesActivations
    {
        [Key]
        public int ID { get; set; }
        public int? Seq { get; set; }
        public int? RDID { get; set; }

        public string? Invoice { get; set; }
        public string? Invoice10 { get; set; }
        public string? OrderNo { get; set; }

        public string? Customer { get; set; }
        public string? CustName { get; set; }
        public string? CustTerritory { get; set; }

        public string? MSD { get; set; }

        public DateTime? InvoiceDate { get; set; }
        public DateTime? OrderDate { get; set; }

        public string? RecordType { get; set; }
        public string? RecordTypeExtended { get; set; }

        public string? VoicePlan { get; set; }
        public string? VoicePlanDescription { get; set; }

        public int? CommissionVoice { get; set; }

        public string? DataPlan { get; set; }
        public string? DataPlanDescription { get; set; }

        public int? CommissionData { get; set; }

        public string? CAPHardware { get; set; }
        public int? CapCost { get; set; }

        public string? W00Code { get; set; }
        public string? BVType { get; set; }

        public int? BVInvoiceLine { get; set; }
        public string? BVRecNo { get; set; }

        public string? Whse { get; set; }
        public string? PartNumber { get; set; }

        public string? Description { get; set; }
        public string? ProductCode { get; set; }

        public string? CellPhoneNo { get; set; }
        public string? IMEIESN { get; set; }

        public int? Qty { get; set; }

        public int? ItemCost { get; set; }
        public int? ItemSellPrice { get; set; }

        public string? CommissionSubsidy { get; set; }
        public int? CommissionSubsidyCost { get; set; }

        public string? CommissionSPIF { get; set; }
        public int? CommissionSPIFCost { get; set; }

        public string? TopUpSDF { get; set; }
        public int? TopUpSDFCost { get; set; }

        public string? TopUpSDFAcc { get; set; }
        public int? TopUpSDFAccCost { get; set; }

        public string? TopUpSDFLic { get; set; }
        public int? TopUpSDFLicCost { get; set; }

        public int? REBATE { get; set; }

        public string? FreeAccessory { get; set; }

        public int? AccessoryCost { get; set; }
        public int? AccessoryPrice { get; set; }

        public string? UserInitials { get; set; }
        public string? Salesperson { get; set; }

        public string? CustomerPONo { get; set; }
        public string? SIMCardNo { get; set; }

        public string? WebOrderID { get; set; }
        public string? UserName { get; set; }

        public string? OriginalInvoice { get; set; }

        public string? AdjustmentType { get; set; }
        public string? Fee { get; set; }

        public bool? Supress { get; set; }

        public string? PinNo { get; set; }

        public string? CostBudgetCode { get; set; }
        public string? Department { get; set; }

        public string? Comments { get; set; }

        public string? FeeType { get; set; }

        public int? GSTRate { get; set; }
        public int? PSTRate { get; set; }

        public string? GSTFlag { get; set; }
        public string? PSTFlag { get; set; }

        public string? PayMeth { get; set; }

        public string? CustomerPostal { get; set; }
        public string? CustomerPostalFirstDigit { get; set; }

        public string? Channel { get; set; }

        public int? ImportLineID { get; set; }

        public int? InvoiceNet { get; set; }
        public int? InvoiceShipping { get; set; }
        public int? InvoiceTaxes { get; set; }
        public int? InvoiceTotal { get; set; }

        public string? RodID { get; set; }

        public string? PortedCTN { get; set; }

        public int? Terms { get; set; }
        public string? TermsText { get; set; }

        public int? CAPCostHUP { get; set; }

        public string? ShipToPostal { get; set; }

        public string? FreeAccessoryPart { get; set; }

        public int? AccessorySRP { get; set; }

        public string? SCOA { get; set; }

        public string? M2MOrderID { get; set; }

        public string? ControlCentre { get; set; }

        public string? TransactionNo { get; set; }

        public string? AccountCode { get; set; }

        public string? AuthorizedDepartment { get; set; }

        public int? CommissionCable { get; set; }

        public string? CablePlan { get; set; }

        public string? CablePlanDescription { get; set; }

        public string? RMANumber { get; set; }

        public string? PCCPID { get; set; }

        public decimal? PCCPAmount { get; set; }

        public int? Tax1Code { get; set; }
        public int? Tax2Code { get; set; }

        public string? BVReceipt { get; set; }

        public int? BVReceiptNo { get; set; }

        public string? OriginalSKUBVPartNumber { get; set; }

        public string? OriginalWebOrderID { get; set; }

        public string? OriginalHardware { get; set; }

        public string? OriginalIMEI { get; set; }

        public string? CHTRWebID { get; set; }

        public string? CHTRChaseID { get; set; }

        public int? UpFrontEdgePrice { get; set; }

        public int? InvoiceNetBeforeRVUE { get; set; }

        public string? ClaimCarrier { get; set; }

        public string? ClaimNumber { get; set; }

        public int? DeviceOfferTypeID { get; set; }

        public string? POLine { get; set; }

        public string? ShipToPostalFirstDigit { get; set; }

        public string? ShipToProvince { get; set; }

        public string? R4BOrderID { get; set; }

        public string? V21DealerCode { get; set; }

        public decimal? CustPayAmount { get; set; }

        public decimal? CustPayAmountOriginal { get; set; }

        public string? AccessoryType { get; set; }

        public string? AccountNumber { get; set; }

        public string? AgentName { get; set; }

        public string? AgentEmail { get; set; }

        public string? AgentContactNumber { get; set; }

        public decimal? RogersHWMarginShare { get; set; }

        public int? Term { get; set; }

        public bool? Isbulk { get; set; }

        public int? SpireCount { get; set; }

        public DateTime? CreatedDate { get; set; }
    }

    public class tblOnhandIMEIs
    {

        [Key]
        public int Id { get; set; }
        public string? INV_NO { get; set; }
        public string? WAREHOUSE { get; set; }
        public string? PART_NO { get; set; }
        public string? NUMBER { get; set; }
        public int? Created_by { get; set; }
        public DateTime? Created_date { get; set; }
        public int? Modified_by { get; set; }
        public DateTime? Modified_date { get; set; }
    }

        public class tblOpeningBalanceACC
    {
        [Key]
        public int ID { get; set; }
        public string? WHSE { get; set; } = string.Empty;

        public string? PartNo { get; set; } = string.Empty;
        public int? ONHAND { get; set; }
    }
    public class usermaster
    {
        [Key]
        public short Id { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        public string Password { get; set; } = "";

        [Required]
        public short UserRoleId { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }

        [Phone(ErrorMessage = "Invalid phone number"), MaxLength(20)]
        public string? ContactNumber { get; set; } = "";

        [MaxLength(200)]
        public string? Address { get; set; } = "";

        [MaxLength(100)]
        public string? State { get; set; } = "";

        public string? JwtToken { get; set; }

        [MaxLength(20)]
        public string? ZipCode { get; set; } = "";

        [MaxLength(100)]
        public string? Country { get; set; } = "";

        [MaxLength(100)]
        public string? City { get; set; } = "";

        [NotMapped]
        public string? RoleName { get; set; }
    }
    public class tblIMEICountDuplicates
    {
        [Key]
        public int ID { get; set; }

        public string? Warehouse { get; set; }

        public string? Part { get; set; }

        public string? IMEI { get; set; }

        public int? CountID { get; set; }

        public int? MinOfID { get; set; }

        public bool? Deleted { get; set; }

        public string? CountFile { get; set; }

        public int? RowNumber { get; set; }

        public int? ColumnNumber { get; set; }
    }

    public class tblACCBackOrders
    {
        [Key]
        public int ID { get; set; }

        public string Whse { get; set; }

        public string ProdCode { get; set; }

        public string PartNo { get; set; }

        public string Description { get; set; }

        public int? QtyTotal { get; set; }

        public int? RowNumber { get; set; }

        public string CountFile { get; set; }
    }


public class tblACCCounts
    {
        [Key]
        public int ID { get; set; }

        public string? Whse { get; set; }

        public string? ProdCode { get; set; }

        public string? PartNo { get; set; }

        public string? Description { get; set; }

        public int? QtyTotal { get; set; }

        public int? RowNumber { get; set; }

        public string? CountFile { get; set; }
    }

    public class userRole
    {
        public short Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class tblMan
    {
        public short Id { get; set; }
        public string Name { get; set; }
        public bool? IsActive { get; set; }
        public string InventoryType { get; set; }

    }


   

public class tbIACCBckOrders
    {
        [Key]
        public int ID { get; set; }

        public string WhseCode { get; set; } = string.Empty;

        public string ProdCode { get; set; } = string.Empty;

       
        public string? Description { get; set; }

       
        public decimal QtyTotal { get; set; }

      
        public string? CountFile { get; set; }

        public int? CountFileRow { get; set; }
    }
    public class TblPackingSlip
    {
        [Key]
        public string PONumber { get; set; }  // nvarchar(50)
        public int RecNo { get; set; }        // int
        public string Whse { get; set; }      // nvarchar(200)
        public string PartNo { get; set; }    // nvarchar(200)
        public string GUID { get; set; }      // nvarchar(200)
        public string IMEI { get; set; }      // nvarchar(200)
        public int XLSRow { get; set; }       // int
        public int? Created_by { get; set; }
        public DateTime? Created_date { get; set; }
        public int? Modified_by { get; set; }
        public DateTime? Modified_date { get; set; }
    }


    public class RolePermissions
    {
        public int Id { get; set; }
        public short RoleId { get; set; }
        public int MenuId { get; set; }
        public DateTime CreatedDate { get; set; }

     
    }

    public class usermastermenus
    {
        public short Id { get; set; }                   // smallint, PK
        public string MenuName { get; set; } = "";      // nvarchar(100), Not Null
        public string? MenuUrl { get; set; }            // nvarchar(200), Nullable
        public short? ParentId { get; set; }            // smallint, Nullable
        public bool IsActive { get; set; }              // bit, Not Null
        public string? Icon { get; set; }               // nvarchar(50), Nullable
        public DateTime? CreatedDate { get; set; }      // datetime2(7), Nullable
        public string? Controller { get; set; }     
        public short? IndexId { get; set; }
    }

    public class WWSalesDetailTEMP
    {
        [Key]
        public string NUMBER { get; set; }
        public decimal? RECNO { get; set; }

        public string IN_DATE { get; set; } = string.Empty;
        public string WHSE { get; set; } = string.Empty;
        public string CODE { get; set; } = string.Empty;
        public decimal BVCMTDQTY { get; set; }
        public decimal BVUNITPRICE { get; set; }
        public string ProdCode { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string Territory { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? Created_by { get; set; }
    }
    public class tblCounts
    {
        [Key]
        public int ID { get; set; }
        public string Whse { get; set; }
        public string PartNumber { get; set; }
        public string IMEI { get; set; }
        public string CountFile { get; set; }
        public short? RowNumber { get; set; } // smallint
        public short? ColumnNumber { get; set; } // smallint
        public bool? Duplicate { get; set; } // bit
    }
    public class WWAccessories
    {
        [Key]
        [StringLength(6)]
        [Required]
        public string WHSE { get; set; }

        //[Key]
        [StringLength(34)]
        [Required]
        public string CODE { get; set; }

        [StringLength(255)]
        public string Description { get; set; }

        [StringLength(10)]
        public string PROD { get; set; }

        [Column(TypeName = "decimal(16,5)")]
        public decimal? ONHAND { get; set; }

        [Column(TypeName = "decimal(16,5)")]
        public decimal? INV_COMMITTED { get; set; }

        [Column(TypeName = "decimal(16,5)")]
        public decimal? BACK_ORDER { get; set; }

        [Column(TypeName = "decimal(16,5)")]
        public decimal? PURCH_ORDER { get; set; }

        public double? CurrentCost { get; set; }

        public double? AvgCost { get; set; }

        public string InvGroup { get; set; }

        public bool? QtyAdjusted { get; set; }

        public int? AdjustedBy { get; set; }
    }
    public class tblSettingsApi
    {
        [Key]
        public int tblSettingsID { get; set; }
        public string ScanListFolder { get; set; } 
        public string ScanListFile { get; set; }
        public string PackingSlipFolder { get; set; }
        public string PackingSlipFile { get; set; } 
        public int LastACCReceipt { get; set; }
        public bool SendEmail { get; set; }
        public string EmailRecipients { get; set; } 
        public string ReversalFolder { get; set; } 
        public string ReversalFile { get; set; } 
        public string SendFromEmail { get; set; } 
        public string SendFromName { get; set; }
    }

    [Table("tblErrors")]
    public class tblErrors
    {
        [Key]
        public int ID { get; set; }
        public string? VBCode { get; set; }
        public string? VBDescription { get; set; }
        public string? PONumber { get; set; }
        public int? RecNo { get; set; }
        public string? ErrorWhile { get; set; }
        public int? RowCount { get; set; }
        public bool? Resolved { get; set; }
        public int? Created_by { get; set; }
        public DateTime? Created_date { get; set; }
        public int? Modified_by { get; set; }
        public DateTime? Modified_date { get; set; }
    }

    [Table("tblIMEILengthExceptions")]
    public class tblIMEILengthExceptions
    {
        [Key]
        public string ExceptionPart { get; set; } = string.Empty;
        public int? IMEILength { get; set; }
        public bool? AllowAlpha { get; set; }
    }

    [Table("tblAPILog")]
    public class tblAPILog
    {
        [Key]
        public int ID { get; set; }
        public int? ServerID { get; set; }
        public int? CompanyID { get; set; }
        public string? CallType { get; set; }
        public string? Endpoint { get; set; }
        public int? KeyValue { get; set; }
        public string? SendString { get; set; }
        public string? Parameters { get; set; }
        public string? ResponseString { get; set; }
        public string? FullURLPassed { get; set; }
        public string? FullURLUsed { get; set; }
        public int? HTTPStatus { get; set; }
        public string? HTTPStatusText { get; set; }
        public string? HeaderResponse { get; set; }
        public string? HeaderResponseKey { get; set; }
        public string? HeaderResponseLocation { get; set; }
        public long? ResponseTime { get; set; }
        public DateTime? LogDateTime { get; set; } = DateTime.Now;
    }

    [Table("tblSettings")]
    public class tblSettings
    {
        [Key]
        public int ID { get; set; }
        public bool? LoggingEnabled { get; set; }
        public bool? LogResponseData { get; set; }
        public int? LogResponseMaxSize { get; set; }
        public bool? PopUpEnabled { get; set; }
    }


    public class WWSerialNumber
    {
        [Key]
        public string WAREHOUSE { get; set; } = string.Empty;
        public string PART_NO { get; set; } = string.Empty;
        public string NUMBER { get; set; } = string.Empty;
        public decimal HIST_TYPE { get; set; }
        public string HIST_NO { get; set; } = string.Empty;
        public string HIST_GUID { get; set; } = string.Empty;

        public decimal? ISALLOCATED { get; set; }
        public decimal? TEMPSTAT { get; set; }

        public string? SO_NO { get; set; }
        public string? SO_GUID { get; set; }
        public string? SO_DATE { get; set; }
        public string? SO_USER { get; set; }

        public string? INV_NO { get; set; }
        public string? INV_GUID { get; set; }
        public string? INV_DATE { get; set; }
        public string? INV_USER { get; set; }

        public string? PO_NO { get; set; }
        public string? PO_GUID { get; set; }
        public string? PO_DATE { get; set; }
        public string? PO_USER { get; set; }

        public string? RECEIVE_NO { get; set; }
        public string? RECEIVE_GUID { get; set; }
        public string? RECEIVE_DATE { get; set; }
        public string? RECEIVE_USER { get; set; }
        public decimal? RECEIVE_TYPE { get; set; }

        public string? RETURN_NO { get; set; }
        public string? RETURN_GUID { get; set; }
        public string? RETURN_DATE { get; set; }
        public string? RETURN_USER { get; set; }
        public decimal? RETURN_TYPE { get; set; }
        public string? RETURN_NOTE { get; set; }

        public string? EditType { get; set; }
        public string? Reason { get; set; }
    }

    [Table("tblSKU")]
    public class tblSKU
    {
        [Key]
        public string SKU { get; set; }
        public string Type { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    public class dbo_t_orderimport
    {
        [Key]
        public int Id { get; set; }
        public int ImportId { get; set; }

        [NotMapped]
        public int? order_import_id { get; set; }

        // Company & Contact Info
        public string? company_name { get; set; }
        public string? shipping_company_name { get; set; }
        public string? shipping_contact_name { get; set; }
        public string? bus_tel { get; set; }
        public string? rogers_cell_number { get; set; }
        public string? user_name { get; set; }

        // Order Details
        public DateTime? OrderDate { get; set; }
        public string? voice_date_label { get; set; }
        public string? whse { get; set; }
        public string orderID { get; set; }
        public string? bulk_orderid { get; set; }
        public string? org_web_orderID { get; set; }
        public string? AccountNumber { get; set; }

        public string? imported { get; set; }
        public string? invoice_no { get; set; }
        public bool? ChargedOnCreditCard { get; set; }
        public bool? CreditCardPosted { get; set; }
        public string? CreditCardAppliedTo { get; set; }
        public int? cctypeID { get; set; }
        public string? CreditCardTransaction { get; set; }

        // Product & Hardware Details
        public string? bvpartno { get; set; }
        public string? imei { get; set; }
        public int? qty { get; set; }
        public decimal? phone_cost { get; set; }

        // Address Info
        public string? shipping_address { get; set; }
        public string? address { get; set; }
        public string? city { get; set; }
        public string? shipping_city { get; set; }
        public string? shippingprovincename { get; set; }
        public string? hardwareprovincename { get; set; }
        public string? shipping_postal { get; set; }
        public string? postal { get; set; }

        // Flags & Configuration
        public string? fff_commision { get; set; }
        public string? commission_part_no { get; set; }
        public string? hardware_billed_by_rogers { get; set; }
        public int? data_version { get; set; }
        public int? nds_chanelID { get; set; }
        public string? nds_channel_name { get; set; }
        public string? bv_territory_code { get; set; }
        public int? hardware_payment_methodID { get; set; }
        public string? hardware_country_code { get; set; }
        public string? shipping_country_code { get; set; }
        public bool? ReadyToImport { get; set; }
        public bool? BackOrder { get; set; }
        public int? DeviceOfferTypeID { get; set; }
        public decimal? UpfrontEdgePrice { get; set; }
        public string? V21DealerCode { get; set; }
        public decimal? gst_percent { get; set; }
        public decimal? pst_percent { get; set; }
        public string? authorized_cost_centre { get; set; }
        public string? cost_centre_display_name { get; set; }

        // Audit Tracking Columns
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }





    public class tblAdvantageSettings
    {
        [Key]
        public int ID { get; set; }
        public long NextOrderNo { get; set; }
        public long NextTempOrderNo { get; set; }
    }
    public class WWInventory
    {
        [Key]
        public string WHSE { get; set; } = string.Empty;
        public string CODE { get; set; } = string.Empty;

        public string? INV_DESCRIPTION { get; set; }
        public string? PROD { get; set; }
        public decimal? WHOLESALE { get; set; }
        public decimal? WEIGHTED { get; set; }
        public decimal? ONHAND { get; set; }
        public decimal? SERIALIZED_FLAG { get; set; }
        public bool? QtyAdjusted { get; set; }
        public int? AdjustedBy { get; set; }
        public string? MISC_1 { get; set; }
        public DateTime? LastSaleDate { get; set; }
    }
    public class tblScanList
    {
        public int ID { get; set; }               // Primary key
        public int PONumber { get; set; }         // PO Number
        public int RecNo { get; set; }            // Receipt Number
        public string Whse { get; set; }          // Warehouse (varchar(10))
        public string PartNo { get; set; }        // Part Number (varchar(50))
        public string GUID { get; set; }          // GUID (varchar(50))
        public string? Vendor { get; set; }       // Vendor (varchar(100)) - Nullable
        public string? Location { get; set; }     // Location (varchar(50)) - Nullable
        public string IMEI { get; set; }          // IMEI (varchar(20))
        public int XLSRow { get; set; }           // Excel row number
        public DateTime CreatedOn { get; set; }
        public int? Created_by { get; set; }
        public DateTime? Created_date { get; set; }
        public int? Modified_by { get; set; }
        public DateTime? Modified_date { get; set; }
    }

    public class SalesActivationsDetail
    {
        [Key]
        public string Invoice { get; set; }

        public decimal? RECNO { get; set; }

        public int? RecNoDetail { get; set; }

        public string WHSE { get; set; }

        public string CODE { get; set; }

        public string Description { get; set; }

        public string ProdCode { get; set; }

        public decimal? Qty { get; set; }

        public decimal? Price { get; set; }

        public decimal? Cost { get; set; }

        public string Tax1Flag { get; set; }

        public string Tax2Flag { get; set; }

        public decimal? TopUp { get; set; }

        public decimal? CustPayAmount { get; set; }

        public decimal? TopUpEdit { get; set; }

        public decimal? WebSRP { get; set; }

        public decimal? WebCost { get; set; }

        public decimal? FeeAcc { get; set; }

        public string OriginalWebOrderID { get; set; }

        public string OriginalHardware { get; set; }

        public string OriginalIMEI { get; set; }

        public string OriginalSKUBVPartNumber { get; set; }

        public decimal? CalcCost { get; set; }

        public string ReceiptNo { get; set; }

        public string BVReceiptNo { get; set; }

        public int? BVReceiptNoInt { get; set; }

        public int? BVReceiptQty { get; set; }

        public decimal? BVReceiptCost { get; set; }

        public DateTime? BVReceiptDate { get; set; }

        public decimal? UpFrontEdgePrice { get; set; }

        public string POLine { get; set; }

        public decimal? AccPayback { get; set; }

        public decimal? CustPayAmountOriginal { get; set; }

        public string AccessoryType { get; set; }

        public decimal? RogersACCMarginShare { get; set; }
    }
    public class SalesActivations
    {
        [Key]
        public int Id { get; set; }

        public string? Invoice { get; set; }
        public string? Invoice10 { get; set; }
        public string? OrderNo { get; set; }
        public string? Customer { get; set; }
        public string? CustName { get; set; }
        public string? CustTerritory { get; set; }
        public string? MSD { get; set; }

        public DateTime? InvoiceDate { get; set; }
        public DateTime? OrderDate { get; set; }

        public string? RecordType { get; set; }
        public string? RecordTypeExtended { get; set; }

        public string? VoicePlan { get; set; }
        public string? VoicePlanDescription { get; set; }
        public double? CommissionVoice { get; set; }

        public string? DataPlan { get; set; }
        public string? DataPlanDescription { get; set; }
        public double? CommissionData { get; set; }

        public string? CAPHardware { get; set; }
        public double? CapCost { get; set; }

        public string? W00Code { get; set; }
        public string? BVType { get; set; }
        public int? BVInvoiceLine { get; set; }
        public string? BVRecNo { get; set; }

        public string? Whse { get; set; }
        public string? PartNumber { get; set; }
        public string? Description { get; set; }
        public string? ProductCode { get; set; }

        public string? CellPhoneNo { get; set; }
        public string? IMEIESN { get; set; }

        public double? Qty { get; set; }
        public double? ItemCost { get; set; }
        public double? ItemSellPrice { get; set; }

        public string? CommissionSubsidy { get; set; }
        public double? CommissionSubsidyCost { get; set; }

        public string? CommissionSPIF { get; set; }
        public double? CommissionSPIFCost { get; set; }

        public string? TopUpSDF { get; set; }
        public double? TopUpSDFCost { get; set; }

        public string? TopUpSDFAcc { get; set; }
        public double? TopUpSDFAccCost { get; set; }

        public string? TopUpSDFLic { get; set; }
        public double? TopUpSDFLicCost { get; set; }

        public double? REBATE { get; set; }

        public string? FreeAccessory { get; set; }
        public double? AccessoryCost { get; set; }
        public double? AccessoryPrice { get; set; }

        public string? UserInitials { get; set; }
        public string? Salesperson { get; set; }
        public string? CustomerPONo { get; set; }
        public string? SIMCardNo { get; set; }

        public string? WebOrderID { get; set; }
        public string? UserName { get; set; }

        public string? OriginalInvoice { get; set; }
        public string? AdjustmentType { get; set; }
        public double? Fee { get; set; }
        public bool? Supress { get; set; }

        public string? PinNo { get; set; }
        public string? CostBudgetCode { get; set; }
        public string? Department { get; set; }

        public string? Comments { get; set; }
        public string? FeeType { get; set; }

        public decimal? GSTRate { get; set; }
        public decimal? PSTRate { get; set; }

        public string? GSTFlag { get; set; }
        public string? PSTFlag { get; set; }

        public string? PayMeth { get; set; }

        public string? CustomerPostal { get; set; }
        public string? CustomerPostalFirstDigit { get; set; }

        public string? Channel { get; set; }

        public int? ImportLineID { get; set; }

        public double? InvoiceNet { get; set; }
        public double? InvoiceShipping { get; set; }
        public double? InvoiceTaxes { get; set; }
        public double? InvoiceTotal { get; set; }

        public string? RodID { get; set; }
        public string? PortedCTN { get; set; }

        public int? Terms { get; set; }
        public string? TermsText { get; set; }

        public double? CAPCostHUP { get; set; }

        public string? ShipToPostal { get; set; }

        public string? FreeAccessoryPart { get; set; }
        public int? AccessorySRP { get; set; }

        public string? SCOA { get; set; }
        public string? M2MOrderID { get; set; }

        public string? ControlCentre { get; set; }
        public string? TransactionNo { get; set; }

        public string? AccountCode { get; set; }
        public string? AuthorizedDepartment { get; set; }

        public double? CommissionCable { get; set; }

        public string? CablePlan { get; set; }
        public string? CablePlanDescription { get; set; }

        public string? RMANumber { get; set; }

        public string? PCCPID { get; set; }
        public decimal? PCCPAmount { get; set; }

        public int? Tax1Code { get; set; }
        public int? Tax2Code { get; set; }

        public string? BVReceipt { get; set; }
        public int? BVReceiptNo { get; set; }

        public string? OriginalSKUBVPartNumber { get; set; }
        public string? OriginalWebOrderID { get; set; }

        public string? OriginalHardware { get; set; }
        public string? OriginalIMEI { get; set; }

        public string? CHTRWebID { get; set; }
        public string? CHTRChaseID { get; set; }

        public decimal? UpFrontEdgePrice { get; set; }
        public decimal? InvoiceNetBeforeRVUE { get; set; }

        public string? ClaimCarrier { get; set; }
        public string? ClaimNumber { get; set; }

        public int? DeviceOfferTypeID { get; set; }

        public string? POLine { get; set; }

        public string? ShipToPostalFirstDigit { get; set; }
        public string? ShipToProvince { get; set; }

        public string? R4BOrderID { get; set; }
        public string? V21DealerCode { get; set; }

        public decimal? CustPayAmount { get; set; }
        public decimal? CustPayAmountOriginal { get; set; }

        public string? AccessoryType { get; set; }

        public string? AccountNumber { get; set; }

        public string? AgentName { get; set; }
        public string? AgentEmail { get; set; }
        public string? AgentContactNumber { get; set; }

        public decimal? RogersHWMarginShare { get; set; }

        public int? Term { get; set; }

        public bool? Bulk { get; set; }

        public short? SpireCount { get; set; }

        public DateTime? CreatedDate { get; set; }
    }


public class hardwarereceived
    {
        [Key]
        public int Id { get; set; }

        public string? Vendor { get; set; }

        public string? BVReceiptNo { get; set; }

        public DateTime? BVReceiptDate { get; set; }

        public string? CMO { get; set; }

        public string? PO { get; set; }

        public string? Part { get; set; }

        public double Qty { get; set; }

        public double ReceiptUnitCost { get; set; }

        public string? IMEI { get; set; }

        public string? ItemType { get; set; }
    }

    public class HPCExtract
    {
        public int Id { get; set; }

        public string SKU { get; set; } = string.Empty;

        public decimal DealerCost { get; set; }

        public DateTime DropDate { get; set; }

        public DateTime? DelistedDate { get; set; } // Nullable because DelistedDate can be null
    }

    public class HPCExtractSummary
    {
        public int Id { get; set; }  // Assuming you have a primary key

        public string Part { get; set; } = string.Empty;

        public decimal Cost { get; set; }

        public DateTime MaxOfF3 { get; set; }  // SQL 'date' maps to DateTime

        public string Whse { get; set; } = string.Empty;

        public DateTime? DelistDate { get; set; }  // Nullable if can be null
    }

    public class t_hardware
    {
        [Key]
        public int HardwareID { get; set; }
        public int ManufacturerID { get; set; }
        public string Model { get; set; }
        public string SmallDetails { get; set; }
        public string MoreDetails { get; set; }
        public string SmallDetailsFR { get; set; }
        public string MoreDetailsFR { get; set; }
        public string BVPartNumber { get; set; }
        public int DataPlanTypeID { get; set; }
        public bool Template { get; set; }
        public bool NoHUP { get; set; }
        public int StockQty { get; set; }
        public bool OutOfStock { get; set; }
        public bool BogoHogo { get; set; }
        public bool Quarantine { get; set; }
        public bool Discontinued { get; set; }
        public decimal DealerCost { get; set; }
        public decimal MarkUpCost { get; set; }
        public decimal ConsumerMonthlyVoiceSRP { get; set; }
        public decimal ConsumerMonthlyDataSRP { get; set; }
        public decimal ConsumerMonthlyVoiceDataSRP { get; set; }
        public decimal Consumer1VoiceSRP { get; set; }
        public decimal Consumer1DataSRP { get; set; }
        public decimal Consumer1VoiceDataSRP { get; set; }
        public decimal Consumer2VoiceSRP { get; set; }
        public decimal Consumer2DataSRP { get; set; }
        public decimal Consumer2VoiceDataSRP { get; set; }
        public decimal Consumer3VoiceSRP { get; set; }
        public decimal Consumer3DataSRP { get; set; }
        public decimal Consumer3VoiceDataSRP { get; set; }
        public decimal Consumer2VoiceHUPSRP { get; set; }
        public decimal Consumer2DataHUPSRP { get; set; }
        public decimal Consumer2VoiceDataHUPSRP { get; set; }
        public decimal Consumer3VoiceHUPSRP { get; set; }
        public decimal Consumer3DataHUPSRP { get; set; }
        public decimal Consumer3VoiceDataHUPSRP { get; set; }
        public decimal VoiceSpiffMonthly { get; set; }
        public decimal DataSpiffMonthly { get; set; }
        public decimal VoiceSpiff1Yr { get; set; }
        public decimal DataSpiff1Yr { get; set; }
        public decimal VoiceSpiff2Yr { get; set; }
        public decimal DataSpiff2Yr { get; set; }
        public decimal VoiceSpiff3Yr { get; set; }
        public decimal DataSpiff3Yr { get; set; }
        public decimal PromoSpiffMonthly { get; set; }
        public decimal PromoSpiff1Yr { get; set; }
        public decimal PromoSpiff2Yr { get; set; }
        public decimal PromoSpiff3Yr { get; set; }
        public decimal HWSubsidyMonthly { get; set; }
        public decimal HWSubsidy1Yr { get; set; }
        public decimal HWSubsidy2Yr { get; set; }
        public decimal HWSubsidy3Yr { get; set; }
        public decimal DataMailInRebateMonthly { get; set; }
        public decimal DataMailInRebate1Yr { get; set; }
        public decimal DataMailInRebate2Yr { get; set; }
        public decimal DataMailInRebate3Yr { get; set; }
        public decimal ConsumerInstantRebateSpiffMonthly { get; set; }
        public decimal ConsumerInstantRebateSpiff1Yr { get; set; }
        public decimal ConsumerInstantRebateSpiff2Yr { get; set; }
        public decimal ConsumerInstantRebateSpiff3Yr { get; set; }
        public decimal ConsumerMailInRebateMonthly { get; set; }
        public decimal ConsumerMailInRebate1Yr { get; set; }
        public decimal ConsumerMailInRebate2Yr { get; set; }
        public decimal ConsumerMailInRebate3Yr { get; set; }
        public string OutOfStockETA { get; set; }
        public decimal ConsumerMonthlyBundledSRP { get; set; }
        public decimal Consumer1BundledSRP { get; set; }
        public decimal Consumer2BundledSRP { get; set; }
        public decimal Consumer3BundledSRP { get; set; }
        public decimal Consumer2BundledHUPSRP { get; set; }
        public decimal Consumer3BundledHUPSRP { get; set; }
        public decimal SpiffBundled { get; set; }
        public decimal HWSubsidyBundled { get; set; }
        public decimal ConsumerRebate { get; set; }
        public bool NDSOutOfStock { get; set; }
        public bool NDSQOutOfStock { get; set; }
        public string ModelFR { get; set; }
        public bool BPRToolkitPromo { get; set; }
        public bool NIS2YrHUP { get; set; }
        public bool NIS3YrHUP { get; set; }
        public decimal NIS2YrHUPSRP { get; set; }
        public decimal NIS3YrHUPSRP { get; set; }
        public decimal NISHUPDiscount { get; set; }
        public bool Delisted { get; set; }
        public bool Admin { get; set; }
        public int ModelID { get; set; }
        public int OldHWType { get; set; }
        public int OldHWID { get; set; }
        public bool FlgFixedSRP { get; set; }
        public bool IsM2MDevice { get; set; }
        public int SIMTypeID { get; set; }
        public bool Removed { get; set; }
        public DateTime? DelistedDate { get; set; }
        public bool NoTierDiscount { get; set; }
        public bool HWOnlyNoActivation { get; set; }
        public bool SerialNoRequired { get; set; }
        public bool DataOnlyDevice { get; set; }
        public string SpecialInstruction { get; set; }
        public string SpecialInstructionFR { get; set; }
        public decimal HardwareOnlySRP { get; set; }
        public int HardwareCategoryID { get; set; }
        public string HardwareCategory { get; set; }
        public int OrigSIMTypeID { get; set; }
        public bool SIMWithHUP { get; set; }
        public int DisplayOrder { get; set; }
        public int Weight { get; set; }
        public string RefurbishedDataPlanType { get; set; }
        public DateTime? OutOfStockETADate { get; set; }
        public int HardwareTypeID { get; set; }
        public int HardwareOSID { get; set; }
        public string CorpType { get; set; }
        public bool ExternalFulfilment { get; set; }
        public bool BuiltInSIM { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int HardwareGroupID { get; set; }
        public int HardwareSequence { get; set; }
        public bool RuggedizedDevice { get; set; }
        public string HardwareImageEN { get; set; }
        public string HardwareImageFR { get; set; }
        public int IDVDeviceSort { get; set; }
        public bool NewGCOutOfStock { get; set; }
        public bool IsRefurbDevice { get; set; }
        public bool AllowedVoiceOnly { get; set; }
        public bool IsHardwareBRT { get; set; }
        public string SIMTypeIDs { get; set; }
        public int QuantityInBV { get; set; }
        public DateTime? LastUpdateQuantityInBV { get; set; }
        public int RefurbishedTypeID { get; set; }
        public string RefurbDeviceSuffix { get; set; }
        public bool IsKMEEnabled { get; set; }
        public bool IsGZTEnabled { get; set; }
        public int HardwareConditionTypeId { get; set; }
        public bool IsAppleDEPEnabled { get; set; }
        public bool IsFeatureDevice { get; set; }
        public bool HideOutOfStockFeatureDevice { get; set; }
        public int FeatureDeviceSort { get; set; }
        public bool Is5GReady { get; set; }
        public bool ShowInDLMDashboard { get; set; }
        public decimal BVCurrentCost { get; set; }
        public bool IsEndOfLife { get; set; }
        public DateTime? QuantityInBVUpdateDateTime { get; set; }
        public int DPAgreementUrlID { get; set; }
        public int StockMinmiumThreshold { get; set; }
        public bool SerialNumberAvailable { get; set; }
        public bool ShowSTMPopupMessage { get; set; }
    }

    public class tblSpireInvoice
    {
        [Key]
        public int Seq1 { get; set; }
        public string invoice_no { get; set; } = string.Empty;
        public string cust_no { get; set; } = string.Empty;
        public DateTime? invoice_date { get; set; }
        public string territory_code { get; set; } = string.Empty;
        public string terms_description { get; set; } = string.Empty;
        public string whse { get; set; } = string.Empty;
        public string part_no { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public int? committed_qty { get; set; }
        public int? unit_price { get; set; }
        public int? current_cost { get; set; }
        public int? subtotal { get; set; }
        public int? freight { get; set; }
        public int? total_discount { get; set; }
        public int? total { get; set; }
        public int? sales_tax_total1 { get; set; }
        public int? sales_tax_total2 { get; set; }

        public string CUSTOM_AddressesWB_link_table { get; set; } = string.Empty;
        public string CUSTOM_AddressesWB_1_link_table { get; set; } = string.Empty;
        public string CUSTOM_AddressesWB_addr_type { get; set; } = string.Empty;
        public string CUSTOM_AddressesWB_1_addr_type { get; set; } = string.Empty;

        public string CUSTOM_AddressesWB_name { get; set; } = string.Empty;
        public string CUSTOM_AddressesWB_address1 { get; set; } = string.Empty;
        public string CUSTOM_AddressesWB_address2 { get; set; } = string.Empty;
        public string CUSTOM_AddressesWB_city { get; set; } = string.Empty;
        public string CUSTOM_AddressesWB_prov_state { get; set; } = string.Empty;
        public string CUSTOM_AddressesWB_postal_zip { get; set; } = string.Empty;

        public string CUSTOM_AddressesWB_1_name { get; set; } = string.Empty;
        public string CUSTOM_AddressesWB_1_address1 { get; set; } = string.Empty;
        public string CUSTOM_AddressesWB_1_address2 { get; set; } = string.Empty;
        public string CUSTOM_AddressesWB_1_city { get; set; } = string.Empty;
        public string CUSTOM_AddressesWB_1_prov_state { get; set; } = string.Empty;
        public string CUSTOM_AddressesWB_1_postal_zip { get; set; } = string.Empty;

        public string number { get; set; } = string.Empty;
        public string strGUID { get; set; } = string.Empty;
        public int? serialized_qty { get; set; }
    }

    [Table("RogersAR")]
    public class RogersAR
    {
        [Key]
        public string Transaction { get; set; }
        public string CustomerNo { get; set; }
        public DateTime? Date { get; set; }
        public string InvoiceNo { get; set; }
        public decimal? DebitAmt { get; set; }
        public decimal? Balance { get; set; }
        public string CustomerName { get; set; }
        public string Territory { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    [Table("RogersARData")]
    public class RogersARData
    {
        [Key]
        public string TransactionNo { get; set; }
        public string Comments { get; set; }
        public string Remarks { get; set; }
        public DateTime? SentOn { get; set; }
        public string Comments2 { get; set; }
        public string Comments3 { get; set; }
        public string PaymentCode { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
    [Table("tblAdvantageVoiceImport")]
    public class AdvantageVoiceImport
    {
        [Key]
        public int ID { get; set; }
        public string? CompanyName { get; set; }
        public string? ShippingContact { get; set; }
        public string? ContactNumber { get; set; }
        public DateTime? OrderDate { get; set; }
        public string? OrderType { get; set; }
        public string? SpireOrder { get; set; }
        public string? GOrderNumber { get; set; }
        public string? TemporaryNumber { get; set; }
        public string? MacAddress { get; set; }
        public string? UserName { get; set; }
        public string? BvPartNo { get; set; }
        public string? ShippingAddress { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? PostalCode { get; set; }
        public string? V21Ban { get; set; }
        public string? ContactEmail { get; set; }
        public string? RogersSpecialistEmail { get; set; }
        public string? HardwareType { get; set; }
        public string? PurolatorNumber { get; set; }
        public string? ReturnPurolatorNumber { get; set; }
        public string? DciInvoice { get; set; }
        public string? Status { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string? Note { get; set; }
        public bool Validated { get; set; }
        public string? Reason { get; set; }
        public bool Imported { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int UserId { get; set; }
    }



    [Table("tblBulkChangeList")]
    public class tblBulkChangeList
    {
        [Key]
        public int ID { get; set; }
        public string InvoiceNo { get; set; }

        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
    [Table("tblTaxCodeHistory")]
    public class TaxCodeHistory
    {
        [Key]
        public int Id { get; set; }
        public string ProvCode { get; set; }
        public string ProvinceName { get; set; }
        public decimal Tax1Rate { get; set; }
        public decimal Tax2Rate { get; set; }
        public string TaxType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Comments { get; set; }
        public bool CompoundTax2OnTax1 { get; set; }
    }

    [Table("tblTaxDataOutput")]
    public class TblTaxDataOutput
    {
        [Key]
        public int ID { get; set; }
        public int? Trans { get; set; }
        public DateTime? InvDate { get; set; }
        public string? Invoice { get; set; }
        public string? WebOrderID { get; set; }
        public string? Source { get; set; }
        public string? CustNo { get; set; }
        public string? CustName { get; set; }
        public string? Territory { get; set; }
        public string? ShipToProvince { get; set; }
        public string? PostalDigit { get; set; }
        public string? OneIMEI { get; set; }
        public int? Tax1Code { get; set; }
        public string? Tax1Name { get; set; }
        public string? Tax1GL { get; set; }
        public int? Tax2Code { get; set; }
        public string? Tax2Name { get; set; }
        public string? Tax2GL { get; set; }
        public decimal? InvoiceNet { get; set; }
        public decimal? Tax1Total { get; set; }
        public decimal? Tax2Total { get; set; }
        public decimal? ShippingAmt { get; set; }
        public decimal? InvoiceTotalBeforeUERVValue { get; set; }
        public decimal? UERVValue { get; set; }
        public decimal? InvoiceTotal { get; set; }
        public decimal? TotalOfExtendedSell { get; set; }
        public decimal? CalcTax1 { get; set; }
        public decimal? CalcTax2 { get; set; }
    }

    [Table("tblGLTransToTaxAccounts")]
    public class TblGLTransToTaxAccounts
    {
        [Key]
        public int ID { get; set; }
        public string Tran_Date { get; set; }
        public string Post_Date { get; set; }
        public string Acct_No { get; set; }
        public int? Trans_No { get; set; }
        public string Where_From { get; set; }
        public string GL_User { get; set; }
        public string BVGLMEMOWHO { get; set; }
        public string BVRESERVED11 { get; set; }
        public string BVGLMEMOKEY { get; set; }
        public string BVRESERVED13 { get; set; }
        public string BVGLMEMOTRAN { get; set; }
        public string BVRESERVED15 { get; set; }
        public string MF_Who { get; set; }
        public string MF_Key { get; set; }
        public string MF_Tran { get; set; }
        public decimal? Debit_Amt { get; set; }
        public decimal? Credit_Amt { get; set; }
    }

    [Table("tblTaxAccounts")]
    public class tblTaxAccounts
    {
        [Key]
        public int ID { get; set; }
        public string GL_ACCOUNT { get; set; }
    }

    [Table("WWGLTrans")]
    public class WWGLTrans
    {
        [Key]
        public int ID { get; set; }
        public int? Trans_No { get; set; }
        public string Tran_Date { get; set; }
        public string GL_Memo { get; set; }
        public string Acct_No { get; set; }
        public string Where_From { get; set; }
        public string GL_User { get; set; }
        public decimal? Debit_Amt { get; set; }
        public decimal? Credit_Amt { get; set; }
        public string Source { get; set; }
    }

    [Table("tbl21410Summary")]
    public class Tbl21410Summary
    {
        [Key]
        public int TransNo { get; set; }
        public System.DateTime? TransDate { get; set; }
        public string Vendor { get; set; }
        public string User { get; set; }
        public decimal? ITCAmount { get; set; }
        public decimal? ExpenseAmount { get; set; }
        public string ExpenseAccounts { get; set; }
        public string Source { get; set; }
        public string InvoiceRef { get; set; }
        public string Memo { get; set; }
        public string ExpenseAccountsDesc { get; set; }
    }
    [Table("tblCustomerSalesOutput")]
    public class TblCustomerSalesOutput
    {
        [Key]
        public int Id { get; set; }
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
        public int UserId { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
    [Table("tblCustomerColumns")]
    public class TblCustomerColumns
    {
        [Key]
        public int Id { get; set; }
        public string CustomerGroup { get; set; }
        public string FieldName { get; set; }
        public string Label { get; set; }
        public bool Include { get; set; }
        public int Sequence { get; set; }
        public string? SummaryType { get; set; }
        public string? FormatString { get; set; }
        public int? Level { get; set; }
        //public int? CreatedBy { get; set; }
        //public DateTime? CreatedDate { get; set; }
        //public int? ModifiedBy { get; set; }
        //public DateTime? ModifiedDate { get; set; }
    }

    [Table("tblCustomerGroups")]
    public class TblCustomerGroups
    {
        [Key]
        public int Id { get; set; }
        public string CustGroup { get; set; }
        public string BVCustNo { get; set; }
        public string GroupName { get; set; }
        public string BVName { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    [Table("tblEventTypes")]
    public class TblEventTypes
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int EventType { get; set; }
        public string EventDescription { get; set; } = string.Empty;
        public bool HasTrans { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    [Table("tblRootCauses")]
    public class TblRootCauses
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Code { get; set; }
        public string Description { get; set; } = string.Empty;
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    [Table("tblTerritoryGroups")]
    public class TblTerritoryGroups
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ID { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? GroupCriteria { get; set; }
        public int? SortOrder { get; set; }
        public string? Phone1 { get; set; }
        public string? Phone2 { get; set; }
        public bool RogersReporting { get; set; }
        public string? RogersReportingName { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    [Table("tblAllowedAccounts")]
    public class TblAllowedAccounts
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ID { get; set; }
        public string Account { get; set; } = string.Empty;
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    [Table("tblEvents")]
    public class TblEvents
    {
        [Key]
        public int ID { get; set; }
        public int EventType { get; set; }
        public string? CustNo { get; set; }
        public string? CustType { get; set; }
        public string? EventText { get; set; }
        public double? EventAmount { get; set; }
        public string? CommentKey { get; set; }
        public DateTime? AddDate { get; set; }
        public string? AddUser { get; set; }
        public DateTime? ModDate { get; set; }
        public string? ModUser { get; set; }
        //public string? CreatedBy { get; set; }
        //public DateTime? CreatedDate { get; set; }
        //public int? ModifiedBy { get; set; }
        //public DateTime? ModifiedDate { get; set; }
    }

    [Table("tblEventTrans")]
    public class TblEventTrans
    {
        [Key]
        public int ID { get; set; }
        public int EventID { get; set; }
        public string TransNo { get; set; } = string.Empty;
        //public int? CreatedBy { get; set; }
        //public DateTime? CreatedDate { get; set; }
        //public int? ModifiedBy { get; set; }
        //public DateTime? ModifiedDate { get; set; }
    }

    [Table("tblARDetailExtra")]
    public class TblARDetailExtra
    {
        [Key]
        public int ID { get; set; }
        public string? TransNo { get; set; } = string.Empty;
        public string? BAN { get; set; }
        public DateTime? FirstNoticeDate { get; set; }
        public decimal? FirstNoticeBalance { get; set; }
        public DateTime? SecondNoticeDate { get; set; }
        public decimal? SecondNoticeBalance { get; set; }
        public byte? RootCauseID { get; set; }
        public byte? NextID { get; set; }
        public bool OPCResolved { get; set; }
        public string? OPCDescription { get; set; }
        public string? BulkID { get; set; }
        public bool BulkIDChecked { get; set; }
        public bool IgnoreGroup { get; set; }
        public string? BillToCust { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    [Table("tblBulkCustomers")]
    public class TblBulkCustomers
    {
        [Key]
        public int ID { get; set; }
        public string CustNo { get; set; } = string.Empty;
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    [Table("tblCustomerGroupsRR")]
    public class TblCustomerGroupsRR
    {
        [Key]
        public int Id { get; set; }
        public string CustGroup { get; set; } = string.Empty;
        public string? GroupName { get; set; }
        public string BVCustNo { get; set; } = string.Empty;
        public string? BVName { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    [Table("tblCustomersOpen")]
    public class TblCustomersOpen
    {
        [Key]
        public int Id { get; set; }
        public string CUST { get; set; } = string.Empty;
        public string? CustName { get; set; }
        public string? CustGroup { get; set; }
        public bool GroupAndSingle { get; set; }
        public string? SALES_TERR { get; set; }
        public string? PostalCode { get; set; }
        public string? BVADDRTELNO1 { get; set; }
        public string? BVADDREMAIL { get; set; }
        public string? BVCOCONTACT1NAME { get; set; }
        public string? BVCOCONTACT1TEL1 { get; set; }
        public string? BVCOCONTACT1EMAIL { get; set; }
        public string? BVCOCONTACT2NAME { get; set; }
        public string? BVCOCONTACT2TEL1 { get; set; }
        public string? BVCOCONTACT2EMAIL { get; set; }
        public string? BVCOCONTACT3NAME { get; set; }
        public string? BVCOCONTACT3TEL1 { get; set; }
        public string? BVCOCONTACT3EMAIL { get; set; }
        public string? Language { get; set; }
        public int? ChannelID { get; set; }
        public int? AddressID { get; set; }
        public int UserId { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    [Table("ARDetailView")]
    public class TblARDetailView
    {
        [Key]
        public int Id { get; set; }
        public string? CustGroup { get; set; }
        public string CUST { get; set; } = string.Empty;
        public string? FOLIO { get; set; }
        public string? TopItem { get; set; }
        public string? Type { get; set; }
        public string TRANS_NO { get; set; } = string.Empty;
        public string? REF_NO { get; set; }
        public DateTime? TranDate { get; set; }
        public decimal D_AMOUNT { get; set; }
        public decimal C_AMOUNT { get; set; }
        public decimal BALANCE { get; set; }
        public int? DaysOld { get; set; }
        public bool Checked { get; set; }
        public int? ARID { get; set; }
        public int UserId { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    [Table("tblARDetailViewFull")]
    public class TblARDetailViewFull
    {
        [Key]
        public int Id { get; set; }
        public string? CustGroup { get; set; }
        public string CUST { get; set; } = string.Empty;
        public string? FOLIO { get; set; }
        public string? TopItem { get; set; }
        public string? Type { get; set; }
        public string TRANS_NO { get; set; } = string.Empty;
        public string? REF_NO { get; set; }
        public DateTime? TranDate { get; set; }
        public decimal D_AMOUNT { get; set; }
        public decimal C_AMOUNT { get; set; }
        public decimal BALANCE { get; set; }
        public int? DaysOld { get; set; }
        public bool Checked { get; set; }
        public int? ARID { get; set; }
        public int UserId { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    [Table("tblActivationsLookup")]
    public class TblActivationsLookup
    {
        [Key]
        public int Id { get; set; }
        public string Invoice { get; set; } = string.Empty;
        public DateTime? InvoiceDate { get; set; }
        public int? MaxOfID { get; set; }
        public string? Customer { get; set; }
        public string? ActivationsTerritory { get; set; }
        public string? MSD { get; set; }
        public string? WebOrderID { get; set; }
        public string? CustomerPostal { get; set; }
        public string? ShipToPostal { get; set; }
        public string? CostBudgetCode { get; set; }
        public string? CustomerPONo { get; set; }
        public string? UserName { get; set; }
        public string? CellPhoneNo { get; set; }
        public decimal? CountGovChannel { get; set; }
        public decimal? CountGovFee { get; set; }
        public int UserId { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    [Table("tblUsers")]
    public class TblUsers
    {
        [Key]
        public int ID { get; set; }
        public string DomainUser { get; set; } = string.Empty;
        public string? Initials { get; set; }
        public int? DefaultChannel { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
