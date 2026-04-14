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


    public class RunRateRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int UserId { get; set; }
    }
    public class AccessoriesRunRateItem
    {
        public string Group { get; set; }
        public string PROD { get; set; }
        public string CODE { get; set; }
        public string Description { get; set; }
        public decimal Cost { get; set; }
        public decimal OnHand { get; set; }
        public decimal TotalSales { get; set; }
        public decimal AvgDailySales { get; set; }
        public decimal WeeklyRunRate { get; set; }
        public string WeeksAvailable { get; set; } // "NA" if totalSales=0
    }
    public class RunRateItemBO
    {
        public string Code { get; set; }
        public string Group { get; set; }
        public string Prod { get; set; }
        public string Description { get; set; }

        public decimal Cost { get; set; }
        public decimal OnHand { get; set; }

        public decimal TotalSales { get; set; }
        public decimal AvgDailySales { get; set; }
        public decimal WeeklyRunRate { get; set; }
        public decimal WeeksAvailable { get; set; }

        public string POLast { get; set; }
        public decimal QtyLast { get; set; }
        public DateTime? DateLast { get; set; }
        public int AgeLast { get; set; }

        public string POLast2 { get; set; }
        public decimal QtyLast2 { get; set; }
        public DateTime? DateLast2 { get; set; }
        public int AgeLast2 { get; set; }
    }

    public class HardwareViewItem
    {
        public string Manufacturer { get; set; }
        public string CODE { get; set; }
        public string PROD { get; set; }
        public string Description { get; set; }
        public decimal Cost { get; set; }
        public decimal OnHand { get; set; }
        public decimal TotalSales { get; set; }
        public decimal AvgDailySales { get; set; }
        public decimal WeeklyRunRate { get; set; }
        public string WeeksAvailable { get; set; } // string to include 'NA'
    }
    public class HardwareRunRateItem
    {
        public string Manufacturer { get; set; }
        public string PROD { get; set; }
        public string CODE { get; set; }
        public string Description { get; set; }
        public decimal Cost { get; set; }
        public decimal OnHand { get; set; }
        public decimal TotalSales { get; set; }
        public decimal AvgDailySales { get; set; }
        public decimal WeeklyRunRate { get; set; }
        public decimal WeeksAvailable { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }

}


