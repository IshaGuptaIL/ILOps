using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.RunRate
{
    public interface IRunRate
    {

        Task<List<RunRateItem>> GetWFHInventoryAsync();
    }
}
