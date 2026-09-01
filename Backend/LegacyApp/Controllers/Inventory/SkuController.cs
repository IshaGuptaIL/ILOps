using DAL.Inventory.AdvantageVoice;
using DAL.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LegacyApp.Controllers.Inventory
{
    /// <summary>
    /// Manages master SKU catalog definitions, descriptions, and active statuses for Advantage Voice.
    /// Provides full CRUD operations for SKU catalog management.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SkuController : ControllerBase
    {
        private readonly ISku _skuDA;

        public SkuController(ISku skuDA)
        {
            _skuDA = skuDA;
        }

        /// <summary>
        /// Retrieves all configured SKU definitions in the Advantage Voice catalog.
        /// Populates SKU selection dropdowns across the application.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<tblSKU>>> GetSkus()
        {
            var skus = await _skuDA.GetAllSkusAsync();
            return Ok(skus);
        }

        /// <summary>
        /// Creates a new SKU catalog entry with assigned item codes and descriptions.
        /// Adds a new product classification to the system.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<bool>> AddSku(tblSKU sku)
        {
            var success = await _skuDA.AddSkuAsync(sku);
            return Ok(success);
        }

        /// <summary>
        /// Updates an existing SKU's description, status, or attributes.
        /// Saves modifications to the master SKU definition.
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<bool>> UpdateSku(tblSKU sku)
        {
            var success = await _skuDA.UpdateSkuAsync(sku);
            return Ok(success);
        }

        /// <summary>
        /// Deletes a SKU definition by SKU name.
        /// Removes obsolete product codes from the catalog.
        /// </summary>
        [HttpDelete("{skuName}")]
        public async Task<ActionResult<bool>> DeleteSku(string skuName)
        {
            var success = await _skuDA.DeleteSkuAsync(skuName);
            return Ok(success);
        }
    }
}
