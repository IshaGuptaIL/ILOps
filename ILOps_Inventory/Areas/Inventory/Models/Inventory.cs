using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ILOps_Inventory.Areas.Inventory.Models
{
    public class InventoryItems
    {
        [Required(ErrorMessage = "Warehouse is required")]
        public string Whse { get; set; } = "";

        [Required(ErrorMessage = "You must enter a part number.")]
        [StringLength(80)]
        public string PartNo { get; set; } = "";

        [Required(ErrorMessage = "You must enter a description.")]
        [StringLength(80, ErrorMessage = "The english description must be 80 characters or less.")]
        public string Description { get; set; } = "";

        [Required(ErrorMessage = "You must enter a French Description.")]
        [StringLength(80, ErrorMessage = "The french description must be 80 characters or less.")]
        public string FrDescription { get; set; } = "";

        [Required(ErrorMessage = "You must select a Type.")]
        public string Type { get; set; } = "";

        [Display(Name = "Manufacturer/Group")]
        public string? AccessoryGroup { get; set; }

        public string ProductCode { get; set; } = "";
        public string SalesDept { get; set; } = "";

        [Required(ErrorMessage = "Cost price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Cost price must be greater than zero.")]
        public decimal? CostPrice { get; set; }

        public List<SelectListItem>? Warehouses { get; set; }
        public List<ManufacturerBO>? Manufacturers { get; set; }
        public decimal? SellingPrice { get; set; }
    }

    public class ManufacturerBO
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string InventoryType { get; set; } = "";
    }

   

    public class CostEntry
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool IsActive { get; set; }
    }






   
}
