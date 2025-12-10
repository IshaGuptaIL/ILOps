using System.ComponentModel.DataAnnotations;

namespace ILOps_Inventory.Areas.Inventory_Add.Models
{
    public class InventoryItems
    {
        [Required(ErrorMessage = "You must select a warehouse.")]
        public string Whse { get; set; }

        [Required(ErrorMessage = "You must enter a part number.")]
        [StringLength(80)]
        public string PartNo { get; set; }

        [Required(ErrorMessage = "You must enter a description.")]
        [StringLength(80, ErrorMessage = "The english description must be 80 characters or less.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "You must enter a French Description.")]
        [StringLength(80, ErrorMessage = "The french description must be 80 characters or less.")]
        public string FrDescription { get; set; }

        [Required(ErrorMessage = "You must select a Type.")]
        public string Type { get; set; }          // Combo6

        public string AccessoryGroup { get; set; } // cmbGroup

        public string ProductCode { get; set; }    // txtProdCode

        public string SalesDept { get; set; }      // txtSalesDept

        [Range(0, double.MaxValue)]
        public decimal CostPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal SellingPrice { get; set; }


    }
}
