using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using static DAL.Inventory.IMEI.HardwareIMEI.HardwareService;

namespace DAL.Inventory.IMEI.HardwareIMEI
{
    public class ReceiveImeiBO
    {
    }

    public class ReceiveImeiRequest
    {
        public long PurchaseOrderId { get; set; }
        public long PurchaseOrderLineId { get; set; }
        public string[] Imeis { get; set; } = Array.Empty<string>();
        public string[] PackingSlipImeis { get; set; } = Array.Empty<string>();
        public string[] ScanListImeis { get; set; } = Array.Empty<string>();
        public bool PostReceipt { get; set; }
        public bool IsReversal { get; set; }
        public string? CmoNumber { get; set; }
    }

    public class CheckErrorsRequest
    {
        public long PurchaseOrderId { get; set; }
        public long PurchaseOrderLineId { get; set; }
        public string[] PackingSlipImeis { get; set; } = Array.Empty<string>();
        public string[] ScanListImeis { get; set; } = Array.Empty<string>();
        public bool IsReversal { get; set; }
        // From Combo3 selection — needed for qty validation
        public decimal OrderQty { get; set; }
        public decimal ReceivedQty { get; set; }
        public string? Whse { get; set; }
    }

    public class CheckErrorsResponse
    {
        public bool HasErrors => Errors.Count > 0;
        public List<string> Errors { get; set; } = new();
        public int PackingSlipCount { get; set; }
        public int ScanListCount { get; set; }
        public int InvalidScanCount { get; set; }
        public int InvalidPackCount { get; set; }
        public int ScanDupeCount { get; set; }
        public int PackDupeCount { get; set; }

        // Multi-grid result collections (maps to frmReceive sub-forms)
        public List<string> Matches { get; set; } = new();
        public List<string> ScanNoPack { get; set; } = new();
        public List<string> PackNoScan { get; set; } = new();
        public List<string> AlreadyInInventory { get; set; } = new();
        public List<string> InvalidScanImeis { get; set; } = new();
        public List<string> InvalidPackImeis { get; set; } = new();
    }

    public class PurchaseOrderListItem
    {
        public string Id { get; set; }                // PO line item ID — Col(14) = POITEMID
        public long PurchaseOrderId { get; set; }   // PO header ID — Col(13) = POID
        public string? PoNumber { get; set; }       // po_number — Col(0)
        public string? Vendor { get; set; }         // vendor_no — Col(1)
        public int Sequence { get; set; }           // sequence/recno — Col(2)
        public string? Whse { get; set; }           // whse — Col(3)
        public string? PartNo { get; set; }         // part_no — Col(4)
        public string? Description { get; set; }   // description — Col(5)
        public string? Guid { get; set; }           // guid (line item GUID) — Col(6)
        public decimal OrderQty { get; set; }       // order_qty — Col(7)
        public decimal ReceivedQty { get; set; }    // received_qty — Col(8)
        public decimal UnitCost { get; set; }       // unit_price — Col(9)
        public string? Status { get; set; }         // PO header status — Col(10)
        public string? Location { get; set; }       // whse_location — Col(12)
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    public class SerialNumber
    {
        [JsonPropertyName("serialNumber")]
        public string? Number { get; set; }

        [JsonPropertyName("whse")]
        public string? Whse { get; set; }

        [JsonPropertyName("partNo")]
        public string? PartNo { get; set; }

        [JsonPropertyName("committedQty")]
        public int CommittedQty { get; set; }
    }

    // Maps to Spire API purchasing/orders/{id} items[] array element

    public class UpdatePoLineDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("receiveQty")]
        public int ReceiveQty { get; set; }

        [JsonPropertyName("serials")]
        public List<SerialNumber> Serials { get; set; } = new();
    }


    public class PurchaseOrderLine
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("orderQty")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal OrderQty { get; set; }

        [JsonPropertyName("receiveQty")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal ReceiveQty { get; set; }

        [JsonPropertyName("unitPrice")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("partNo")]
        public string PartNo { get; set; } = "";

        [JsonPropertyName("whse")]
        public string Whse { get; set; } = "";

        [JsonPropertyName("serials")]
        public List<SerialNumber> Serials { get; set; } = new();
    }
    // Maps to Spire API purchasing/orders/{id} response root
    public class PurchaseOrder
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        // ✅ FIX: correct field name
        [JsonPropertyName("number")]
        public string? OrderNo { get; set; }

        // ✅ FIX: nested object
        [JsonPropertyName("vendor")]
        public Vendor? Vendor { get; set; }

        // ✅ helper (safe access)
        [JsonIgnore]
        public string VendorNo => Vendor?.VendorNo ?? "";

        [JsonPropertyName("items")]
        public List<PurchaseOrderLine> Items { get; set; } = new();
    }

    public class Vendor
    {
        [JsonPropertyName("vendorNo")]
        public string? VendorNo { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
    // Maps to Spire API inventory/serials response item
    public class SpireSerial
    {
        [JsonPropertyName("number")]
        public string? Number { get; set; }

        [JsonPropertyName("whse")]
        public string? Whse { get; set; }

        [JsonPropertyName("partNo")]
        public string? PartNo { get; set; }

        
        [JsonPropertyName("onhandQty")]
        public decimal OnhandQty { get; set; }

        [JsonPropertyName("tempQty")]
        public decimal TempQty { get; set; }

        [JsonPropertyName("committedQty")]
        public decimal CommittedQty { get; set; }

        public bool IsAllocated =>
            OnhandQty == 0 || TempQty != 0 || CommittedQty != 0;
    }


}
