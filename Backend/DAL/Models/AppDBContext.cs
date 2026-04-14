using DAL.Inventory.SpareLight;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Models
{
    public class AppDBContext :DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext>options):base(options) { }
        

        public DbSet<usermaster> usermaster { get; set; }
        public DbSet<userRole> userRole { get; set; }
        public DbSet<tblMan> tblMan { get; set; }

        public DbSet<usermastermenus> usermastermenus { get; set; }

        public DbSet<RolePermissions> RolePermissions { get; set; }
        public DbSet<hardwarereceived> hardwarereceived { get; set; }

        public DbSet<HPCExtract> HPCExtract { get; set; }

        public DbSet<HPCExtractSummary> HPCExtractSummary { get; set; }

        public DbSet<t_hardware> t_hardware { get; set; }
        public DbSet<tblScanList> tblScanList { get; set; }

        public DbSet<TblPackingSlip> TblPackingSlip { get; set; }

        public DbSet<tblSettingsApi> tblSettingsApi { get; set; }
        public DbSet<WWAccessories> WWAccessories { get; set; }

        public DbSet<WWInventory> WWInventory { get; set; }

        public DbSet<WWSerialNumber> WWSerialNumber { get; set; }
        public DbSet<tblCounts> tblCounts { get; set; }
        public DbSet<WWSalesDetailTEMP> WWSalesDetailTEMP { get; set; }
        public DbSet<tbIACCBckOrders> tbIACCBckOrders { get; set; }
        public DbSet<tblACCCounts> tblACCCounts { get; set; }
        public DbSet<tblIMEICountDuplicates> tblIMEICountDuplicates { get; set; }
        public DbSet<IMEIStatus> IMEIStatus { get; set; }
        public DbSet<tblACCBackOrders> tblACCBackOrders { get; set; }
        public DbSet<tblOpeningBalanceACC> tblOpeningBalanceACC { get; set; }
        public DbSet<tblInvoiceList> tblInvoiceList { get; set; }
        public DbSet<tblSalesActivations> tblSalesActivations { get; set; }
        public DbSet<SalesActivations> SalesActivations { get; set; }

        public DbSet<SalesActivationsDetail> SalesActivationsDetail { get; set; }

        public DbSet<tblSpireInvoice> tblSpireInvoice { get; set; }

        public DbSet<tblOnhandIMEIs> tblOnhandIMEIs { get; set; }

        public DbSet<tbllastpoitem> tbllastpoitem { get; set; }




        public DbSet<tblTransferList> tblTransferList { get; set; }
        public DbSet<tblTransferListACC> tblTransferListACC { get; set; }
        public DbSet<tblTransferLog> tblTransferLog { get; set; }


        public DbSet<RogersAR> RogersAR { get; set; }
        public DbSet<RogersARData> RogersARData { get; set; }



















    }
}
