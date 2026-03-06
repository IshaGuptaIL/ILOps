using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.IMEI.RecieveIMEI
{
    public class RecieveIMEIBO
    {
        public int PONumber { get; set; }
        public int RecNo { get; set; }
        public string Whse { get; set; }
        public string PartNo { get; set; }
        public string GUID { get; set; }
        public string Vendor { get; set; }
        public string Location { get; set; }
        public string IMEI { get; set; }
        public int XLSRow { get; set; }
    }

    public class IMEIItemDto
    {
        public string IMEI { get; set; } = string.Empty;
        public bool Invalid { get; set; } = false;
        public bool Dupe { get; set; } = false;
    }

    public class IMEIGridsDto
    {
        public List<IMEIItemDto> ScanList { get; set; } = new();
        public List<IMEIItemDto> PackingSlip { get; set; } = new();
        public List<IMEIItemDto> Matches { get; set; } = new();
        public List<IMEIItemDto> ScanNoPack { get; set; } = new();
        public List<IMEIItemDto> PackNoScan { get; set; } = new();
        public List<IMEIItemDto> Onhand { get; set; } = new();
    }
    public class PostReceiptsRequest
    {
        public long PoId { get; set; }
        public long PoItemId { get; set; }
        public string Cmo { get; set; }
        public bool IsReversal { get; set; }
    }


}
