using DAL.Common.Login;
using DAL.Inventory.AddInventory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.Inventory
{
    /// <summary>
    /// Handles new inventory item creation and validation across Spire ERP warehouses.
    /// Validates part numbers, bilingual descriptions, product codes, and initializes inventory records.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AddInventoryController : ControllerBase
    {
        private readonly IAddInventory _inventory;

        public AddInventoryController(IAddInventory inventory)
        {
            _inventory = inventory;
        }

        /// <summary>
        /// Checks whether a specified part number already exists in a given warehouse.
        /// Prevents duplicate part code creation across the inventory master.
        /// </summary>
        [HttpGet("CheckPartNo")]
        public async Task<ApiResposne> CheckPartNo(string partNo, string whse)
        {
            return await _inventory.CheckPartNo(partNo, whse);
        }

        // ==============================
        // Add inventory item
        // ==============================
        /// <summary>
        /// Validates item attributes and creates a new inventory master record in Spire ERP.
        /// Enforces description character limits, cost constraints, and product type rules.
        /// </summary>
        [HttpPost("InventoryAdd")]
        public async Task<ApiResposne> InventoryAdd([FromBody] AddInventoryBO model)
        {
            var errors = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(model.Whse)) errors["Whse"] = "Warehouse required";
            if (string.IsNullOrWhiteSpace(model.PartNo)) errors["PartNo"] = "Part Number required";
            if (string.IsNullOrWhiteSpace(model.Description)) errors["Description"] = "Description required";
            if (string.IsNullOrWhiteSpace(model.FrDescription)) errors["FrDescription"] = "French description required";
            if (model.Description?.Length > 80) errors["Description"] = $"Max 80 chars ({model.Description.Length})";
            if (model.FrDescription?.Length > 80) errors["FrDescription"] = $"Max 80 chars ({model.FrDescription.Length})";
            if (model.CostPrice < 0) errors["CostPrice"] = "Cost Price cannot be negative";

            if (!string.IsNullOrEmpty(model.PartNo)) {
        model.PartNo = model.PartNo.ToUpper().Trim();
    } else {
        errors["PartNo"] = "Part Number required";
    }

            ValidateTypeSpecific(model, errors);

            if (errors.Count > 0)
            {
                return new ApiResposne
                {
                    Success = false,
                    StatusCode = 400,
                    Result = errors,
                    Message = "Validation failed"
                };
            }

            try
            {
                // Save via Spire / Postgres helper
                var spireResult = await _inventory.AddInventoryItemAsync(model);

                return spireResult;
            }
            catch (Exception ex)
            {
                return new ApiResposne
                {
                    Success = false,
                    StatusCode = 500,
                    Message = ex.Message
                };
            }
        }

        // ==============================
        // Type-specific validation
        // ==============================
        private void ValidateTypeSpecific(AddInventoryBO model, Dictionary<string, string> errors)
        {
            if (model.Type == "Hardware")
            {
                if (model.ProductCode != "HCC") errors["ProductCode"] = "Must be HCC for Hardware";
                if (model.SalesDept != 4) errors["SalesDept"] = "Must be 4 for Hardware";
                if (string.IsNullOrEmpty(model.AccessoryGroup)) errors["AccessoryGroup"] = "Manufacturer required for Hardware";
            }
            else if (model.Type == "Accessory")
            {
                if (model.ProductCode != "ACC") errors["ProductCode"] = "Must be ACC for Accessory";
                if (model.SalesDept != 5) errors["SalesDept"] = "Must be 5 for Accessory";
                if (string.IsNullOrEmpty(model.AccessoryGroup)) errors["AccessoryGroup"] = "Group required for Accessory";
            }
            else if (model.Type == "License")
            {
                if (model.ProductCode != "ACC") errors["ProductCode"] = "Must be ACC for License";
                if (model.SalesDept != 5) errors["SalesDept"] = "Must be 5 for License";
            }
        }




        [HttpGet("GetWarehousesAsync")]
        public async Task<List<WarehouseBO>> GetWarehousesAsync(int? userRoleId)
        {
            return await _inventory.GetWarehousesAsync(userRoleId);
        }

        [HttpGet("GetManufacturersAsync")]
        public async Task<List<ManufacturerBO>> GetManufacturersAsync()
        {
            return await _inventory.GetManufacturersAsync();
        }

    }
}