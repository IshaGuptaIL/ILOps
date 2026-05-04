using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.AdvantageVoice
{
    public class AdvantageVoiceBO
    {
    }

    public class AdvantageImportVM
    {
        public int ID { get; set; }
        public string CompanyName { get; set; }
        public string ShippingContact { get; set; }
        public string ContactNumber { get; set; }
        public DateTime? OrderDate { get; set; }
        public string OrderType { get; set; }
        public string SpireOrder { get; set; }
        public string GOrderNumber { get; set; }
        public string TemporaryNumber { get; set; }
        public string MacAddress { get; set; }
        public string UserName { get; set; }
        public string BvPartNo { get; set; }
        public string ShippingAddress { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string PostalCode { get; set; }
        public string V21Ban { get; set; }
        public string ContactEmail { get; set; }
        public string RogersSpecialistEmail { get; set; }
        public string HardwareType { get; set; }
        public string PurolatorNumber { get; set; }
        public string ReturnPurolatorNumber { get; set; }
        public string DciInvoice { get; set; }
        public string Status { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string Note { get; set; }
        public bool Validated { get; set; }
        public string Reason { get; set; }
        public bool Imported { get; set; }
        public int UserId { get; set; }
    }

    public class AdvantageTemplateVM
    {
        public string COMPANY_NAME { get; set; }
        public string SHIPPING_CONTACT { get; set; }
        public string CONTACT_NUMBER { get; set; }
        public string Order_date { get; set; }
        public string Order_Type_Hardware_Exchange_Accessory { get; set; }
        public string SpireOrder { get; set; }
        public string G_Order_Number { get; set; }
        public string Temporary_Number { get; set; }
        public string MAC_ADDRESS { get; set; }
        public string First_Name_and_Last_Name { get; set; }
        public string Hardware_SKU { get; set; }
        public string Delivery_Unit_and_Street_Address { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string Postal_Code { get; set; }
        public string V21_BAN { get; set; }
    }
}
