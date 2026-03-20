using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.RunRate
{
    public class RunRateBO
    {
    }
    public class RunRateItem
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public decimal OnHand { get; set; }
    }
}
