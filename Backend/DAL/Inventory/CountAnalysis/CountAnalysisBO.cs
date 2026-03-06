using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.CountAnalysis
{
    public class CountAnalysisBO
    {
    }
    public class UpdateQtyRequest
    {
        public int Id { get; set; }
        public double NewQty { get; set; }
    }
    public class ACCCountsEditBO
    {
        public int ID { get; set; }
        public string Whse { get; set; }
        public string InvGroup { get; set; }
        public string ProdCode { get; set; }
        public string PartNo { get; set; }
        public string Description { get; set; }
        public double QtyTotal { get; set; }
        public int RowNumber { get; set; }
        public string CountFile { get; set; }
    }

    public class AssignWarehouseRequest
    {
        public string CountFile { get; set; }
        public string Warehouse { get; set; }
        public string CountType { get; set; } // 'hardware' or 'accessory'
    }
    public class InvalidSerialDto
    {
        public string PartNumber { get; set; }
        public string Imei { get; set; }
        public int ImeiLength { get; set; }
        public string Spreadsheet { get; set; } // Mapping to CountFile
        public int Row { get; set; }
        public int Column { get; set; }
        public int VerificationResult { get; set; }
        public string Exp1 { get; set; }
    }
    public class ACCEditResponse
    {
        public List<ACCCountsEditBO> Items { get; set; }
        public int TotalItems { get; set; }
    }
    public class AccessoryReportResponse
    {
        public List<object> Items { get; set; }
        public int TotalItems { get; set; }
    }
    public class ImportRequest
    {
        public string FolderPath { get; set; }
    }

    public class BackorderImportDto
    {
        public string Whse { get; set; }
        public string ProdCode { get; set; }
        public string PartNo { get; set; }
        public string Description { get; set; }
        public double QtyTotal { get; set; }
    }
}
