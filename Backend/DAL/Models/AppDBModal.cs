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



}
