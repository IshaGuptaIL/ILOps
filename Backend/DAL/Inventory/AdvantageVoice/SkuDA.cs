using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Inventory.AdvantageVoice
{
    public class SkuDA : ISku
    {
        private readonly AppDBContext _context;

        public SkuDA(AppDBContext context)
        {
            _context = context;
        }

        public async Task<List<tblSKU>> GetAllSkusAsync()
        {
            return await _context.tblSKU.ToListAsync();
        }

        public async Task<tblSKU> GetSkuByIdAsync(int id)
        {
            return await _context.tblSKU.FindAsync(id);
        }

        public async Task<bool> AddSkuAsync(tblSKU sku)
        {
            sku.CreatedDate = DateTime.Now;
            await _context.tblSKU.AddAsync(sku);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateSkuAsync(tblSKU sku)
        {
            var existing = await _context.tblSKU.FindAsync(sku.SKU);
            if (existing == null) return false;

            existing.SKU = sku.SKU;
            existing.Type = sku.Type;
            existing.ModifiedBy = sku.ModifiedBy;
            existing.ModifiedDate = DateTime.Now;

            _context.tblSKU.Update(existing);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteSkuAsync(string skuName)
        {
            var sku = await _context.tblSKU
                .FirstOrDefaultAsync(x => x.SKU == skuName);

            if (sku == null) return false;

            _context.tblSKU.Remove(sku);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
