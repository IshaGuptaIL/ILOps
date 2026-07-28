using DAL.Inventory.AdvantageVoice;
using DAL.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LegacyApp.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class SkuController : ControllerBase
    {
        private readonly ISku _skuDA;

        public SkuController(ISku skuDA)
        {
            _skuDA = skuDA;
        }

        [HttpGet]
        public async Task<ActionResult<List<tblSKU>>> GetSkus()
        {
            var skus = await _skuDA.GetAllSkusAsync();
            return Ok(skus);
        }

        [HttpPost]
        public async Task<ActionResult<bool>> AddSku(tblSKU sku)
        {
            var success = await _skuDA.AddSkuAsync(sku);
            return Ok(success);
        }

        [HttpPut]
        public async Task<ActionResult<bool>> UpdateSku(tblSKU sku)
        {
            var success = await _skuDA.UpdateSkuAsync(sku);
            return Ok(success);
        }

        [HttpDelete("{skuName}")]
        public async Task<ActionResult<bool>> DeleteSku(string skuName)
        {
            var success = await _skuDA.DeleteSkuAsync(skuName);
            return Ok(success);
        }
    }
}
